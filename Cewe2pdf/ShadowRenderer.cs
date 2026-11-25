using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Xml;
using System.Globalization;

namespace Cewe2pdf {
    sealed class ShadowImage : IDisposable {
        public Bitmap Pixels;
        public float Padding;
        public void Dispose() => Pixels.Dispose();
    }

    static class ShadowRenderer {
        internal static ShadowImage Create(Bitmap silhouette, float width, float height, ShadowSettings settings, float opacity = 1) {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            float pad = settings.Spread + settings.Blur * 3 + 1;
            float scale = Math.Min(2, 1536 / Math.Max(width + pad*2, height + pad*2));
            int w = Math.Max(1,(int)Math.Ceiling((width + pad*2)*scale)), h = Math.Max(1,(int)Math.Ceiling((height + pad*2)*scale));
            using var mask = new Bitmap(w,h,PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(mask)) {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                // Expansion changes silhouette bounds, not the thickness of each opaque stroke.
                g.DrawImage(silhouette, new RectangleF((pad-settings.Spread)*scale,(pad-settings.Spread)*scale,
                    (width+2*settings.Spread)*scale,(height+2*settings.Spread)*scale));
            }
            var rect = new Rectangle(0,0,w,h);
            var data = mask.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            var rgba = new byte[w*h*4];
            try { Marshal.Copy(data.Scan0,rgba,0,rgba.Length); } finally { mask.UnlockBits(data); }
            var alpha = new float[w*h];
            for(int i=0;i<alpha.Length;i++) alpha[i] = rgba[i*4+3];
            int radius = (int)Math.Ceiling(settings.Blur*scale);
            if (radius>0) for(int pass=0;pass<3;pass++) {
                alpha = BoxBlur(alpha,w,h,radius,true);
                alpha = BoxBlur(alpha,w,h,radius,false);
            }
            float factor = settings.Opacity * Math.Clamp(opacity,0,1);
            Array.Clear(rgba);
            for(int i=0;i<alpha.Length;i++) rgba[i*4+3]=(byte)Math.Clamp((int)Math.Round(alpha[i]*factor),0,255);
            var result = new Bitmap(w,h,PixelFormat.Format32bppArgb);
            data=result.LockBits(rect,ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try { Marshal.Copy(rgba,0,data.Scan0,rgba.Length); } finally { result.UnlockBits(data); }
            return new ShadowImage { Pixels=result,Padding=pad };
        }

        // Sliding-window passes keep work linear in pixel count even for large blur settings.
        static float[] BoxBlur(float[] source,int w,int h,int radius,bool horizontal) {
            var result=new float[source.Length];
            int length=horizontal?w:h, lines=horizontal?h:w;
            radius=Math.Min(radius,length);
            for(int line=0;line<lines;line++) {
                int Index(int pos)=>horizontal?line*w+pos:pos*w+line;
                double sum=0;
                for(int p=0;p<=radius && p<length;p++)sum+=source[Index(p)];
                for(int p=0;p<length;p++) {
                    result[Index(p)]=(float)(sum/(radius*2+1));
                    if(p-radius>=0)sum-=source[Index(p-radius)];
                    if(p+radius+1<length)sum+=source[Index(p+radius+1)];
                }
            }
            return result;
        }

        internal static Bitmap ImageMask(System.Drawing.Image image, ImageArea area) {
            float scale=Math.Min(2,1536/Math.Max(area.rect.Width,area.rect.Height));
            var mask=new Bitmap(Math.Max(1,(int)Math.Ceiling(area.rect.Width*scale)),Math.Max(1,(int)Math.Ceiling(area.rect.Height*scale)));
            using var g=Graphics.FromImage(mask);
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            g.SmoothingMode=SmoothingMode.AntiAlias;
            g.ScaleTransform(scale,scale);
            var state=g.Save();
            using(var path=area.corners.Path(area.rect.Width,area.rect.Height))g.SetClip(path);
            g.DrawImage(image,new RectangleF(area.cutout.X,area.cutout.Y,image.Width*area.scale,image.Height*area.scale));
            g.Restore(state);
            if(area.border && area.borderWidth>0) {
                using var pen=new Pen(Color.FromArgb(PdfWriter.ColorAlpha(PdfWriter.argb2BaseColor(area.borderColor)),Color.Black),area.borderWidth);
                using var path=area.corners.Path(area.rect.Width,area.rect.Height,area.BorderCenterOffset);
                g.DrawPath(pen,path);
            }
            return mask;
        }

        // The shadow is raster-only; the original foreground text remains searchable and embedded.
        // GDI and PDF font metrics can differ slightly; this approximation is explicitly documented.
        internal static Bitmap TextMask(TextArea area) {
            float scale=Math.Min(3,1536/Math.Max(area.rect.Width,area.rect.Height));
            var mask=new Bitmap(Math.Max(1,(int)Math.Ceiling(area.rect.Width*scale)),Math.Max(1,(int)Math.Ceiling(area.rect.Height*scale)));
            using var g=Graphics.FromImage(mask);
            g.ScaleTransform(scale,scale);
            g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            float y=0;
            var parts=new System.Collections.Generic.List<(TextElement Element,string Word,float X,float Y,int Line)>();
            var widths=new System.Collections.Generic.List<float>();
            int line=0;
            using var format=(StringFormat)StringFormat.GenericTypographic.Clone();
            format.Trimming=StringTrimming.None;
            format.FormatFlags=StringFormatFlags.MeasureTrailingSpaces;
            float x=0,lineHeight=0;
            foreach(var element in area.textElements) {
                var style=(element.bold?FontStyle.Bold:0)|(element.italic?FontStyle.Italic:0)|(element.underlined?FontStyle.Underline:0);
                using var font=new System.Drawing.Font(element.family,Math.Max(1,element.size),style,GraphicsUnit.Pixel);
                foreach(var word in System.Text.RegularExpressions.Regex.Split(element.text,"(?<=\\s)")) {
                    float advance=g.MeasureString(word,font,int.MaxValue,format).Width;
                    if(x>0 && x+advance>area.rect.Width){widths.Add(x);line++;x=0;y+=lineHeight;lineHeight=0;}
                    parts.Add((element,word,x,y,line));
                    x+=advance;lineHeight=Math.Max(lineHeight,element.size*1.3f);
                }
                if(element.newline || element.text.EndsWith("\n")){widths.Add(x);line++;x=0;y+=lineHeight;lineHeight=0;}
            }
            widths.Add(x);
            float contentHeight=y+lineHeight;
            float vertical=area.valign=="ALIGNVCENTER"?(area.rect.Height-contentHeight)/2:area.valign=="ALIGNVBOTTOM"?area.rect.Height-contentHeight:0;
            foreach(var part in parts) {
                var e=part.Element;
                var style=(e.bold?FontStyle.Bold:0)|(e.italic?FontStyle.Italic:0)|(e.underlined?FontStyle.Underline:0);
                using var font=new System.Drawing.Font(e.family,Math.Max(1,e.size),style,GraphicsUnit.Pixel);
                float horizontal=area.align=="ALIGNHCENTER"?(area.rect.Width-widths[part.Line])/2:area.align=="ALIGNRIGHT"?area.rect.Width-widths[part.Line]:0;
                using var brush = new SolidBrush(Color.FromArgb(PdfWriter.ColorAlpha(PdfWriter.argb2BaseColor(e.color)),Color.Black));
                var point=new PointF(part.X+horizontal,part.Y+Math.Max(0,vertical));
                if(area.outlineWidth>0 && PdfWriter.ColorAlpha(PdfWriter.argb2BaseColor(area.outlineColor))>0) {
                    using var glyphs=new GraphicsPath();
                    glyphs.AddString(part.Word,font.FontFamily,(int)style,font.Size,point,format);
                    using var pen=new Pen(Color.FromArgb(PdfWriter.ColorAlpha(PdfWriter.argb2BaseColor(area.outlineColor)),Color.Black),area.outlineWidth) { LineJoin=LineJoin.Round };
                    g.FillPath(brush,glyphs);g.DrawPath(pen,glyphs);
                } else g.DrawString(part.Word,font,brush,point,format);
            }
            return mask;
        }
    }
}
