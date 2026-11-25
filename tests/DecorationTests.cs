using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using Cewe2pdf;
class DecorationTests {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Main(string[] args){
  string folder=Path.GetFullPath(args[0]);Directory.CreateDirectory(folder);
  Config.setMissingFromOptions(new[]{"resource_path="+folder});
  Check(Config.DownloadResources,"dynamic downloads must default to on");
  CmdArgParser.parse(new[]{"--offline"},out var offline);Config.setMissingFromOptions(offline.ToArray());Check(!Config.DownloadResources,"offline flag disables network");
  Check(!Config.SpineLogo,"logo must default to off");
  CmdArgParser.parse(new[]{"--spine-logo"},out var opts);Config.setMissingFromOptions(opts.ToArray());Check(Config.SpineLogo,"logo flag");Config.setMissingToDefaults();Check(Config.SpineLogo,"defaults preserve explicit logo flag");Check(!Config.DownloadResources,"defaults preserve offline flag");
  Config.setMissingFromOptions(new[]{"spine_logo=false"});
  Config.setMissingFromOptions(new[]{"spine_logo=true"},true);Check(!Config.SpineLogo,"config file must not override explicit CLI setting");
  var xml="<fotobook><page type=\"normalpage\" pagenr=\"1\"><bundlesize width=\"1000\" height=\"700\"/><area areatype=\"spinelogoarea\" width=\"50\" height=\"20\"/></page><page type=\"normalpage\" pagenr=\"2\"/></fotobook>";
  var p=new McfParser(new MemoryStream(Encoding.UTF8.GetBytes(xml))).nextPage();Check(!p.areas.OfType<ClipartArea>().Any(),"disabled logo skipped");
  Config.setMissingFromOptions(new[]{"spine_logo=true"});p=new McfParser(new MemoryStream(Encoding.UTF8.GetBytes(xml))).nextPage();Check(p.areas.OfType<ClipartArea>().Single().spineLogo,"enabled logo parsed");
  Config.setMissingFromOptions(new[]{"spine_logo=false"});
  Directory.CreateDirectory(Path.Combine(folder,"123"));
  string svg="<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\" viewBox=\"0 0 100 100\"><circle cx=\"50\" cy=\"50\" r=\"30\" fill=\"#ffffff\"/></svg>";
  File.WriteAllText(Path.Combine(folder,"123","shape.svg"),svg);
  var art=new ClipartArea{designId="123",rect=new RectangleF(0,0,100,100)};art.colors["#ffffffff"]="#8000ff00";
  using(var stream=new MemoryStream(ClipartRenderer.Render(art)))using(var png=new Bitmap(stream)) {
   var middle=png.GetPixel(200,200);Check(middle.G>240&&middle.R<10&&Math.Abs(middle.A-128)<3,"recolor and ARGB opacity");Check(png.GetPixel(0,0).A==0,"transparent edges preserved");
  }
  svg=svg.Replace("fill=\"#ffffff\"","style=\"fill:#fff;fill-opacity:0.5\"");File.WriteAllText(Path.Combine(folder,"123","shape.svg"),svg);
  using(var stream=new MemoryStream(ClipartRenderer.Render(art)))using(var png=new Bitmap(stream))Check(Math.Abs(png.GetPixel(200,200).A-64)<3,"style opacity multiplied");
  string clp="a"+string.Concat(Encoding.UTF8.GetBytes(svg).Select(b=>b.ToString("x2")+"z"));string file=Path.Combine(folder,"shape.clp");File.WriteAllText(file,clp);Check(ClipartRenderer.ReadSvg(file)==svg,"CLP decoded");
  Check(DesignResources.Find("123","decorations")!=null,"cache resource lookup");
  Console.WriteLine("PASS clipart rendering, transparent PNG, recoloring, opacity, CLP decoding, cache lookup and opt-in spine logo");
 }
}

