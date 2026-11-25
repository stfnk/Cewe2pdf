using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Text;
using Cewe2pdf;
class ShapeTests {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static Page Parse(string areas)=>new McfParser(new MemoryStream(Encoding.UTF8.GetBytes("<fotobook><pagenumbering position='0'/><page type='normalpage' pagenr='1'><bundlesize width='1600' height='1200'/>"+areas+"</page><page type='normalpage' pagenr='2'/></fotobook>"))).nextPage();
 static void Main(string[] args){
  string folder=Path.GetFullPath(args[0]);Directory.CreateDirectory(folder);
  string corners="<corners enabled='yes'><corner length='60' shape='convex' where='top-left'/><corner length='60' shape='convex' where='top-right'/><corner length='60' shape='convex' where='bottom-left'/><corner length='60' shape='convex' where='bottom-right'/></corners>";
  string legacy="<area areatype='imagearea' left='100' top='100' width='500' height='400' borderenabled='1' sizeborder='10' colorborder='#ffbb3300'><image filename='photo.png' scale='2.5'/>"+corners+"</area>";
  string modern="<area areatype='imagearea'><position left='850' top='100' width='500' height='400' rotation='12'/><image filename='photo.png'><cutout left='0' top='0' scale='2.5'/></image><decoration><border enabled='1' width='10' color='#ff229944' position='outsideWithGap' gap='10'/><corners enabled='1'><corner shape='convex' length='60' where='top-left'/></corners></decoration></area>";
  string text="<area areatype='textarea' left='100' top='650' width='1400' height='180'><text><outline enabled='1' width='4' color='#ff993300'/><textFormat font='Arial,36,0' Alignment='ALIGNLEFT' foregroundColor='#ffcceeff' backgroundColor='#00000000' hasOutline='1' outlineWidth='2' outlineColor='#ff000000'/><![CDATA[<html><body style='font-size:36pt;color:#cceeff;font-weight:700'><p>Kontur ÄÖÜ</p></body></html>]]></text></area>";
  foreach(var culture in new[]{"de-DE","en-US","fr-FR","tr-TR"}) {
   CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture);
   var p=Parse(legacy+modern+text);var a=(ImageArea)p.areas[0];var b=(ImageArea)p.areas[1];var t=(TextArea)p.areas[2];
   Check(a.border&&Math.Abs(a.borderWidth-4)<.001&&a.borderColor=="#ffbb3300","legacy frame width and color");
   Check(Math.Abs(a.corners.TopLeft-24)<.001&&a.corners.BottomRight==24,"CEWE corner units");
   Check(Math.Abs(b.BorderCenterOffset-6)<.001,"outside gap and half-width");
   Check(b.corners.TopLeft==24&&b.corners.TopRight==0,"per-corner geometry");
   Check(t.outlineWidth==1.6f&&t.outlineColor=="#ff993300","outline width and color");
   Check(!((ImageArea)Parse(legacy.Replace("borderenabled='1'","borderenabled='0'")).areas[0]).border,"legacy frame disabled");
   Check(!((ImageArea)Parse(legacy.Replace("</area>","<border enabled='0' width='10' color='#ff000000'/></area>")).areas[0]).border,"explicit disabled element overrides old attribute");
   Check(!((ImageArea)Parse(legacy.Replace("enabled='yes'","enabled='no'")).areas[0]).corners.Rounded,"corners disabled");
   Check(((TextArea)Parse(text.Replace("<outline enabled='1'","<outline enabled='0'")).areas[0]).outlineWidth==0,"disabled outline overrides textFormat");
   using(var path=a.corners.Path(200,160)){Check(!path.IsVisible(1,1)&&path.IsVisible(100,80),"corner cut versus center");}
   var huge=new ImageCorners{TopLeft=9999,TopRight=9999,BottomLeft=9999,BottomRight=9999};
   using(var path=huge.Path(100,60)){Check(path.IsVisible(50,30)&&!path.IsVisible(0,0),"radius clamped to area");}
   using var pixels=new Bitmap(200,160);using(var g=Graphics.FromImage(pixels))g.Clear(Color.CornflowerBlue);
   a.border=false;using var mask=ShadowRenderer.ImageMask(pixels,a);
   Check(mask.GetPixel(0,0).A==0&&mask.GetPixel(mask.Width/2,mask.Height/2).A==255,"rounded shadow silhouette");
  }
  using(var photo=new Bitmap(200,160)) {
   using(var g=Graphics.FromImage(photo)) {
    using var brush=new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0,0,200,160),Color.DeepSkyBlue,Color.MidnightBlue,35);
    g.FillRectangle(brush,0,0,200,160);g.FillEllipse(Brushes.Gold,70,40,60,60);
   }
   photo.Save(Path.Combine(folder,"photo.png"),ImageFormat.Png);
  }
  string plain=text.Replace("top='650'","top='900'").Replace("<outline enabled='1'","<outline enabled='0'").Replace("Kontur ÄÖÜ","Ohne Kontur");
  string xml="<fotobook imagedir='.'><pagenumbering position='0'/><page type='normalpage' pagenr='1'><bundlesize width='1600' height='1200'/>"+legacy+modern+text+plain+"</page><page type='normalpage' pagenr='2'/></fotobook>";
  string input=Path.Combine(folder,"shapes.mcf"),output=Path.Combine(folder,"shapes.pdf");File.WriteAllText(input,xml,Encoding.UTF8);
  typeof(Cewe2pdf.Program).GetMethod("Main",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{new[]{input,output,"-program_path="+folder,"--offline"}});
  Check(Environment.ExitCode==0,"converter exit");
  string extracted=PdfText.Read(output,1);Check(extracted.Contains("Kontur ÄÖÜ")&&extracted.Contains("Ohne Kontur"),"outlined Unicode text remains searchable");
  using var reader=new iTextSharp.text.pdf.PdfReader(output);string content=Encoding.ASCII.GetString(reader.GetPageContent(1));
  Check(content.Contains("2 Tr"),"PDF fill-and-stroke text operator");
  Check(content.Contains(" c")&&content.Contains("W"),"vector curve and image clipping");
  Console.WriteLine("PASS four cultures, per-corner radius and clipping, legacy/disabled frame precedence, shaped frame placement, searchable outlined Unicode, disabled outline and rounded alpha silhouette");
 }
}
