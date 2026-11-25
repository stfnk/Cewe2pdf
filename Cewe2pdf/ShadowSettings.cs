using System;
using System.Xml;
using System.Numerics;
namespace Cewe2pdf {
    // CEWE exposes no public rendering specification. Keep the empirical unit mapping here.
    sealed class ShadowSettings {
        public bool Enabled;
        public float Angle = 135, Distance = 20, Spread, Blur = 2, Opacity = 128f / 255;
        internal static ShadowSettings Parse(XmlNode area) {
            var node = area.SelectSingleNode("decoration/shadow") ?? area.SelectSingleNode("shadow");
            string Value(string name, string legacy, string fallback) =>
                node?.Attributes?[name]?.Value ?? area.Attributes?[legacy]?.Value ?? fallback;
            if (Value("enabled", "shadowEnabled", "0") is not ("1" or "true")) return new ShadowSettings();
            float Number(string name, string legacy, string fallback) => (float)McfParser.ParseNumber(Value(name, legacy, fallback));
            return new ShadowSettings {
                Enabled = Value("enabled", "shadowEnabled", "0") is "1" or "true",
                Angle = Number("angle", "shadowAngle", "135"),
                Distance = Math.Clamp(Number("distance", "shadowDistance", "50"),0,10000) * .4f,
                Spread = Math.Clamp(Number("shadowWidthInMM", "shadowWidthInMM", "0"),0,1000) * 4f,
                Blur = Math.Clamp(Number("blur", "shadowBlurNew", "5"),0,1000) * .4f,
                Opacity = Math.Clamp(Number("intensity", "shadowIntensity", "128"), 0, 255) / 255f,
            };
        }
        internal Vector2 Offset(float rotation) {
            double angle = Angle * Math.PI / 180, r = rotation * Math.PI / 180;
            double x = -Math.Cos(angle) * Distance, y = Math.Sin(angle) * Distance;
            // CEWE angles are page-relative. Compensate the enclosing object's rotation.
            return new Vector2((float)(Math.Cos(r)*x + Math.Sin(r)*y), (float)(-Math.Sin(r)*x + Math.Cos(r)*y));
        }
    }

}
