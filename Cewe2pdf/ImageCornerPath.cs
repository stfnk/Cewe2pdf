using System;
using System.Drawing.Drawing2D;
using iTextSharp.text.pdf;
namespace Cewe2pdf {
    sealed partial class ImageCorners {
        // One path definition serves PDF clipping, shaped borders and alpha-shadow masks.
        internal GraphicsPath Path(float width, float height, float offset = 0) {
            var path = new GraphicsPath();
            float w = width + offset * 2, h = height + offset * 2, x = -offset, y = -offset;
            if (w <= 0 || h <= 0) return path;
            float Radius(float value) => Math.Min(Math.Min(w, h) / 2, value > 0 ? Math.Max(0, value + offset) : 0);
            float tl = Radius(TopLeft), tr = Radius(TopRight), br = Radius(BottomRight), bl = Radius(BottomLeft);
            const float k = .55228475f;
            path.StartFigure();
            path.AddLine(x + tl, y, x + w - tr, y);
            if (tr > 0) path.AddBezier(x+w-tr,y, x+w-tr+k*tr,y, x+w,y+tr-k*tr, x+w,y+tr);
            path.AddLine(x+w,y+tr, x+w,y+h-br);
            if (br > 0) path.AddBezier(x+w,y+h-br, x+w,y+h-br+k*br, x+w-br+k*br,y+h, x+w-br,y+h);
            path.AddLine(x+w-br,y+h, x+bl,y+h);
            if (bl > 0) path.AddBezier(x+bl,y+h, x+bl-k*bl,y+h, x,y+h-bl+k*bl, x,y+h-bl);
            path.AddLine(x,y+h-bl, x,y+tl);
            if (tl > 0) path.AddBezier(x,y+tl, x,y+tl-k*tl, x+tl-k*tl,y, x+tl,y);
            path.CloseFigure();
            return path;
        }

        internal void AppendPdfPath(PdfContentByte canvas, float x, float y, float width, float height, float offset = 0) {
            using var path = Path(width, height, offset);
            var points = path.PathPoints; var types = path.PathTypes;
            float X(int i) => x + points[i].X;
            float Y(int i) => y + height - points[i].Y;
            for (int i = 0; i < points.Length; i++) {
                switch (types[i] & 7) {
                    case 0: canvas.MoveTo(X(i), Y(i)); break;
                    case 1: canvas.LineTo(X(i), Y(i)); break;
                    case 3:
                        canvas.CurveTo(X(i), Y(i), X(i+1), Y(i+1), X(i+2), Y(i+2)); i += 2; break;
                }
                if ((types[i] & 128) != 0) canvas.ClosePath();
            }
        }
    }
}
