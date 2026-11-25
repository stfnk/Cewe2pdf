using System;
using System.Xml;

namespace Cewe2pdf {
    sealed partial class ImageCorners {
        public float TopLeft, TopRight, BottomLeft, BottomRight;
        public bool Rounded => TopLeft > 0 || TopRight > 0 || BottomLeft > 0 || BottomRight > 0;

        internal static ImageCorners Parse(XmlNode area) {
            var result = new ImageCorners();
            var node = area.SelectSingleNode("decoration/corners") ?? area.SelectSingleNode("corners");
            if (node == null || node.Attributes?["enabled"]?.Value is "0" or "no" or "false") return result;
            foreach (XmlNode corner in node.SelectNodes("corner")) {
                string shape = corner.Attributes?["shape"]?.Value ?? "default";
                if (shape == "default") continue;
                if (shape != "convex") { Log.Warning("Unsupported image corner: " + shape); continue; }
                float length = Math.Max(0, (float)McfParser.ParseNumber(corner.Attributes?["length"]?.Value ?? "0") * .4f);
                switch (corner.Attributes?["where"]?.Value) {
                    case "top-left": result.TopLeft = length; break;
                    case "top-right": result.TopRight = length; break;
                    case "bottom-left": result.BottomLeft = length; break;
                    case "bottom-right": result.BottomRight = length; break;
                }
            }
            return result;
        }

    }
}
