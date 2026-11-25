using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Drawing;
using System.Globalization;

namespace Cewe2pdf {
    static class ClipartRenderer {
        internal static string ReadSvg(string path) {
            string data=File.ReadAllText(path);
            if(Path.GetExtension(path).Equals(".clp",StringComparison.OrdinalIgnoreCase)) {
                // CEWE CLP wraps SVG as hex with interspersed non-hex characters and an 'a' marker.
                if(!data.StartsWith("a",StringComparison.Ordinal))throw new InvalidDataException("Unknown CEWE CLP header");
                string hex=new string(data.Skip(1).Where(c=>c>='0'&&c<='9'||c>='a'&&c<='f').ToArray());
                data=Encoding.UTF8.GetString(Convert.FromHexString(hex));
            }
            return data;
        }
        internal static byte[] Render(ClipartArea art) {
            string path;
            if(art.spineLogo) {
                path=Path.Combine(Config.ProgramPath,"Resources","icons","photofun","spine_logo.clp");
                if(!File.Exists(path))throw new FileNotFoundException("CEWE spine logo resource missing",path);
            } else path=DesignResources.Find(art.designId,"decorations",art.uniqueName);
            if(path==null)throw new FileNotFoundException("CEWE clipart "+art.designId+" not found");
            string content=ReadSvg(path);
            var doc=new XmlDocument{XmlResolver=null};
            using(var reader=XmlReader.Create(new StringReader(content),new XmlReaderSettings{DtdProcessing=DtdProcessing.Ignore,XmlResolver=null}))doc.Load(reader);
            foreach(XmlElement element in doc.SelectNodes("//*")) {
                foreach(string property in new[]{"fill","stroke","stop-color"}) {
                    string value=element.GetAttribute(property);
                    if(TryMap(value,art,out string color,out float alpha)) {
                        element.SetAttribute(property,color);
                        MultiplyOpacity(element,property=="stop-color"?"stop-opacity":property+"-opacity",alpha);
                    }
                }
                string style=element.GetAttribute("style");
                if(!string.IsNullOrEmpty(style)) {
                    var properties=new System.Collections.Generic.Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
                    foreach(string part in style.Split(';')) { int colon=part.IndexOf(':');if(colon>0)properties[part[..colon].Trim()]=part[(colon+1)..].Trim(); }
                    foreach(string prop in new[]{"fill","stroke","stop-color"}) if(properties.TryGetValue(prop,out string value)&&TryMap(value,art,out string color,out float alpha)) {
                        properties[prop]=color;
                        if(alpha<1) {
                            string op=prop=="stop-color"?"stop-opacity":prop+"-opacity";
                            string old=properties.TryGetValue(op,out string existing)?existing:element.GetAttribute(op);
                            float factor=float.TryParse(old,NumberStyles.Float,CultureInfo.InvariantCulture,out float parsed)?parsed:1;
                            properties[op]=(factor*alpha).ToString(CultureInfo.InvariantCulture);
                        }
                    }
                    element.SetAttribute("style",string.Join(";",properties.Select(p=>p.Key+":"+p.Value)));
                }
            }
            doc.DocumentElement.SetAttribute("preserveAspectRatio","none");
            int width=Math.Clamp((int)Math.Ceiling(art.rect.Width*4),1,6000),height=Math.Clamp((int)Math.Ceiling(art.rect.Height*4),1,6000);
            string tool=Environment.GetEnvironmentVariable("CEWE2PDF_SVG_TOOL")??Path.Combine(AppContext.BaseDirectory,"SvgToPng","SvgToPng.exe");
            string temp=Path.Combine(Path.GetTempPath(),"cewe-svg-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
            byte[] pixels;
            try {
                string input=Path.Combine(temp,"input.svg"),pngOutput=Path.Combine(temp,"output.png");File.WriteAllText(input,doc.OuterXml);
                var settings=new System.Diagnostics.ProcessStartInfo(tool){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true};
                foreach(string arg in new[]{input,pngOutput,width.ToString(CultureInfo.InvariantCulture),height.ToString(CultureInfo.InvariantCulture)})settings.ArgumentList.Add(arg);
                using var process=System.Diagnostics.Process.Start(settings);string error=process.StandardError.ReadToEnd();process.WaitForExit();
                if(process.ExitCode!=0)throw new InvalidDataException("SVG renderer failed: "+error);
                pixels=File.ReadAllBytes(pngOutput);
            } finally {Directory.Delete(temp,true);}
            using var pngStream=new MemoryStream(pixels);using var bitmap=new Bitmap(pngStream);
            string mirror=(art.mirror??"").ToLowerInvariant();
            if(mirror is "x" or "horizontal")bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            else if(mirror is "y" or "vertical")bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            else if(mirror is not "" and not "none")Log.Warning("Unsupported clipart mirror value: "+mirror);
            using var output=new MemoryStream();bitmap.Save(output,System.Drawing.Imaging.ImageFormat.Png);return output.ToArray();
        }
        static bool TryMap(string source,ClipartArea art,out string color,out float opacity) {
            string Canonical(string value) {
                value=value.Trim().ToLowerInvariant();
                if(value=="white")return "#ffffff";if(value=="black")return "#000000";
                if(value.Length==9&&value.StartsWith("#"))return "#"+value.Substring(3);
                if(value.Length==4&&value.StartsWith("#"))return "#"+string.Concat(value.Skip(1).Select(c=>new string(c,2)));
                return value;
            }
            foreach(var pair in art.colors) if(Canonical(source)==Canonical(pair.Key)) {
                var target=PdfWriter.argb2BaseColor(pair.Value);color=$"#{target.R:x2}{target.G:x2}{target.B:x2}";opacity=PdfWriter.ColorAlpha(target)/255f;return true;
            }
            color=source;opacity=1;return false;
        }
        static void MultiplyOpacity(XmlElement element,string name,float factor) {
            if(factor>=1)return;
            float old=1;if(float.TryParse(element.GetAttribute(name),NumberStyles.Float,CultureInfo.InvariantCulture,out float value))old=value;
            element.SetAttribute(name,(old*factor).ToString(CultureInfo.InvariantCulture));
        }
    }
}

