using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Numerics;
using System.Xml;
using Cewe2pdf;
class ShadowTests {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static ShadowSettings Parse(string xml){var doc=new XmlDocument();doc.LoadXml(xml);return ShadowSettings.Parse(doc.DocumentElement);}
 static void Main(string[] args){
  string folder=Path.GetFullPath(args[0]);Directory.CreateDirectory(folder);
  foreach(var culture in new[]{"de-DE","en-US","fr-FR","tr-TR"}) {
   CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture);
   var old=Parse("<area shadowEnabled='1' shadowDistance='11.62' shadowAngle='135' shadowIntensity='64' shadowWidthInMM='1.25' shadowBlurNew='2.5'/>");
   var modern=Parse("<area><decoration><shadow enabled='1' distance='11.62' angle='135' intensity='64' shadowWidthInMM='1.25' blur='2.5'/></decoration></area>");
   Check(old.Enabled&&modern.Enabled&&Math.Abs(old.Distance-4.648)<.001,"invariant legacy distance");
   Check(old.Distance==modern.Distance&&old.Spread==modern.Spread&&old.Blur==modern.Blur&&old.Opacity==modern.Opacity,"old and new structures agree");
   Check(!Parse("<area shadowEnabled='1'><shadow enabled='0'/></area>").Enabled,"disabled child overrides legacy flag");
   Check(!Parse("<area/>").Enabled,"no invented shadows");
  }
  var settings=new ShadowSettings{Enabled=true,Angle=90,Distance=12,Blur=0,Spread=0,Opacity=.5f};
  Check(Vector2.Distance(settings.Offset(0),new Vector2(0,12))<.001,"page direction");
  Check(Vector2.Distance(settings.Offset(90),new Vector2(12,0))<.001,"compensate rotated area");
  using var shape=new Bitmap(100,100);using(var g=Graphics.FromImage(shape)){g.FillEllipse(Brushes.CornflowerBlue,10,10,80,80);g.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;g.FillEllipse(Brushes.Transparent,35,35,30,30);}
  using(var crisp=ShadowRenderer.Create(shape,100,100,settings)){
   Check(crisp.Pixels.GetPixel(crisp.Pixels.Width/2,crisp.Pixels.Height/2).A==0,"alpha hole preserved");
   Check(crisp.Pixels.GetPixel(50,50).A>=126&&crisp.Pixels.GetPixel(50,50).A<=129,"intensity multiplied");
   Check(crisp.Pixels.GetPixel(0,0).A==0,"no dark rectangle outside silhouette");
   crisp.Pixels.Save(Path.Combine(folder,"crisp.png"));
  }
  using(var dim=ShadowRenderer.Create(shape,100,100,settings,.5f))Check(dim.Pixels.GetPixel(50,50).A<=65,"object alpha multiplies shadow alpha");
  settings.Blur=3;
  using(var soft=ShadowRenderer.Create(shape,100,100,settings)){soft.Pixels.Save(Path.Combine(folder,"soft.png"));Check(soft.Pixels.GetPixel(soft.Pixels.Width/2,soft.Pixels.Height/2).A<100,"blur does not fill silhouette hole");}
  shape.Save(Path.Combine(folder,"shape.png"));
  string mcf=Path.Combine(folder,"shadows.mcf");File.WriteAllText(mcf,"<fotobook imagedir='.'></fotobook>");using var book=new Mcfx(mcf);
  Config.setMissingFromOptions(new[]{"program_path="+folder,"download_resources=false"});Config.setMissingToDefaults();
  Page.pageNoEnabled=false;
  var page=new Page{bundleSize=new Vector2(640,400)};
  page.areas.Add(new ImageArea{filename="shape.png",rect=new RectangleF(70,70,100,100),scale=1,shadow=settings});
  page.areas.Add(new ImageArea{filename="shape.png",rect=new RectangleF(270,70,100,100),scale=1,rotation=35,shadow=settings});
  var text=new TextArea{rect=new RectangleF(65,240,500,95),font="Arial",fontsize=32,text="Schatten ÄÖÜ",align="ALIGNLEFT",valign="ALIGNVTOP",backgroundcolor="#00000000",shadow=settings,
   textElements=new(){new TextElement{text="Schatten ÄÖÜ",family="Arial",size=32,bold=true,newline=true,color="#ff226688"}}};
  using(var textMask=ShadowRenderer.TextMask(text)) {int opaque=0;for(int y=0;y<textMask.Height;y++)for(int x=0;x<textMask.Width;x++)if(textMask.GetPixel(x,y).A>0)opaque++;Check(opaque>200&&opaque<textMask.Width*textMask.Height/2,"text shadow follows glyphs");}
  text.textElements[0].color="#00226688";
  using(var invisible=ShadowRenderer.TextMask(text)){int alpha=0;for(int y=0;y<invisible.Height;y++)for(int x=0;x<invisible.Width;x++)alpha+=invisible.GetPixel(x,y).A;Check(alpha==0,"transparent text casts no shadow");}
  text.textElements[0].color="#ff226688";
  page.areas.Add(text);
  Directory.CreateDirectory(Path.Combine(folder,"123"));
  File.WriteAllText(Path.Combine(folder,"123","shape.svg"),"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'><path d='M50 5 L95 95 L5 95 Z' fill='#339977'/></svg>");
  Config.setMissingFromOptions(new[]{"resource_path="+folder});
  page.areas.Add(new ClipartArea{designId="123",rect=new RectangleF(470,70,100,100),opacity=.7f,shadow=settings});
  var writer=new Cewe2pdf.PdfWriter(Path.Combine(folder,"shadows.pdf"),book);writer.writePage(page);writer.close();
  using var reader=new iTextSharp.text.pdf.PdfReader(Path.Combine(folder,"shadows.pdf"));Check(reader.NumberOfPages==1,"PDF generated");
  File.WriteAllText(Path.Combine(folder,"cli-shadows.mcf"), """
  <fotobook imagedir="."><pagenumbering position="0"/><page type="normalpage" pagenr="1"><bundlesize width="1600" height="1000"/>
  <area areatype="imagearea" left="175" top="175" width="250" height="250" shadowEnabled="1" shadowDistance="30" shadowAngle="90" shadowIntensity="128" shadowWidthInMM="0" shadowBlurNew="7.5"><image filename="shape.png" scale="2.5"/></area>
  <area areatype="imagearea"><position left="675" top="175" width="250" height="250" rotation="35"/><image filename="shape.png"><cutout left="0" top="0" scale="2.5"/></image><decoration><shadow enabled="1" distance="30" angle="90" intensity="128" shadowWidthInMM="0" blur="7.5"/></decoration></area>
  <area areatype="textarea" left="162.5" top="600" width="1250" height="237.5"><shadow enabled="1" distance="30" angle="90" intensity="128" shadowWidthInMM="0" blur="7.5"/><text><textFormat font="Arial,25,0" Alignment="ALIGNLEFT" foregroundColor="#ff226688" backgroundColor="#00000000"/><![CDATA[<html><body style="font-family: Arial; font-size:25pt; font-weight:700; color:#226688"><p>Schatten ÄÖÜ</p></body></html>]]></text></area>
  <area areatype="clipartarea" alpha="0.7" left="1175" top="175" width="250" height="250"><clipart designElementId="123"/><shadow enabled="1" distance="30" angle="90" intensity="128" shadowWidthInMM="0" blur="7.5"/></area>
  </page><page type="normalpage" pagenr="2"/></fotobook>
  """);
  Console.WriteLine("PASS four cultures, legacy/new shadow parsing, disable, rotation, alpha silhouettes, intensity, blur, image and searchable text shadow PDF");
 }
}
