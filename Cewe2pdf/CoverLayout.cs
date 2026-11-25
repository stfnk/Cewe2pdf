using System;
using System.Drawing;

namespace Cewe2pdf {
    sealed class CoverLayout {
        public RectangleF Front { get; private set; }
        public RectangleF Back { get; private set; }
        public float SpineWidth { get; private set; }

        // CEWE's cover includes the board overhang on both axes. Do not use the
        // spine TEXT area's height: it is inset and is not the physical spine width.
        internal static CoverLayout FromSizes(float coverWidth, float coverHeight, float insideWidth, float insideHeight) {
            float panel = insideWidth / 2 + coverHeight - insideHeight;
            float spine = coverWidth - panel * 2;
            if (insideWidth <= 0 || insideHeight <= 0 || coverHeight <= 0 || panel <= 0 || spine < -.02f || spine > coverWidth / 4)
                return null;
            spine = Math.Max(0,spine);
            return new CoverLayout {
                Back = new RectangleF(0,0,panel,coverHeight),
                Front = new RectangleF(coverWidth-panel,0,panel,coverHeight),
                SpineWidth = spine,
            };
        }

        internal static bool Intersects(RectangleF viewport, RectangleF rect, float rotation, float expansion = 0) {
            double angle = rotation * Math.PI / 180;
            float w = (float)(Math.Abs(Math.Cos(angle))*rect.Width + Math.Abs(Math.Sin(angle))*rect.Height);
            float h = (float)(Math.Abs(Math.Sin(angle))*rect.Width + Math.Abs(Math.Cos(angle))*rect.Height);
            var bounds = new RectangleF(rect.X+rect.Width/2-w/2-expansion,rect.Y+rect.Height/2-h/2-expansion,w+2*expansion,h+2*expansion);
            return viewport.IntersectsWith(bounds);
        }
    }
}
