using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using Cewe2pdf;
class BookTests {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static string Text(string label,float left,float top=40,float width=350,float height=100,string kind="textarea",float rotation=0)=>FormattableString.Invariant($"<area areatype='{kind}'><position left='{left}' top='{top}' width='{width}' height='{height}' rotation='{rotation}'/><text><textFormat font='Arial,12,0' Alignment='ALIGNLEFT' foregroundColor='#ff000000' backgroundColor='#00000000'/><![CDATA[<html><body style='font-family:Arial;font-size:12pt'><p>{label}</p></body></html>]]></text></area>");
 static string Page(string type,string areas,string width="1100",string height="500")=>$"<page type='{type}' pagenr='1'><bundlesize width='{width}' height='{height}'/>{areas}</page>";
 static void Run(string input,string output,string folder){typeof(Cewe2pdf.Program).GetMethod("Main",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{new[]{input,output,"-program_path="+folder,"--offline"}});Check(Environment.ExitCode==0,"conversion exit");}
 static void Main(string[] args){
  string folder=Path.GetFullPath(args[0]);Directory.CreateDirectory(folder);Directory.CreateDirectory(Path.Combine(folder,"Resources"));
  foreach(var culture in new[]{"de-DE","en-US","fr-FR","tr-TR"}) {
   CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture);
   var g=CoverLayout.FromSizes(440,200,400,190);Check(Math.Abs(g.Front.Width-210)<.001&&g.Front.X==230&&g.Back.X==0&&g.SpineWidth==20,"real spine excluded from panels");
   Check(CoverLayout.FromSizes(420,200,400,190).SpineWidth==0,"softcover without spine");
   Check(CoverLayout.FromSizes(100,200,400,190)==null,"reject impossible geometry");
   Check(!CoverLayout.Intersects(g.Front,new RectangleF(160,95,120,10),270),"rotated spine excluded from front");
   Check(CoverLayout.Intersects(g.Front,new RectangleF(250,20,100,20),0),"front text retained");
  }
  foreach(var spec in new[]{("back.png",210,Color.LightBlue),("front.png",210,Color.LightGreen),("spine.png",20,Color.LightCoral)}) {
   using var image=new Bitmap(spec.Item2,200);using(var graphics=Graphics.FromImage(image))graphics.Clear(spec.Item3);image.Save(Path.Combine(folder,spec.Item1),ImageFormat.Png);
  }
  string back=Page("fullcover","<area areatype='imagearea' left='0' top='0' width='525' height='500'><image filename='back.png' scale='2.5'/></area>"+"<area areatype='imagearea' left='525' top='0' width='50' height='500'><image filename='spine.png' scale='2.5'/></area>"+Text("BACK",40)+Text("SPINE",400,237.5f,300,25,"spinetextarea",270).Replace("Arial,12,0","Arial,4,0").Replace("font-size:12pt","font-size:4pt"));
  string spine=Page("spine","");
  string front=Page("fullcover","<area areatype='imagebackgroundarea'><position left='0' top='0' width='525' height='500'/><imagebackground filename='front.png' backgroundPosition='RIGHT_OR_BOTTOM'><cutout left='0' top='0' scale='2.5'/></imagebackground></area>"+Text("FRONT",650));
  string inner=Page("normalpage",Text("INSIDE-L",30),"1000","475")+Page("normalpage",Text("INSIDE-R",550),"1000","475");
  string xml="<fotobook imagedir='.'><pagenumbering position='0'/>"+back+spine+front+inner+"</fotobook>";
  string input=Path.Combine(folder,"book.mcf"),output=Path.Combine(folder,"book.pdf");File.WriteAllText(input,xml,Encoding.UTF8);Run(input,output,folder);
  using(var reader=new iTextSharp.text.pdf.PdfReader(output)) {
   Check(reader.NumberOfPages==4,"front, inside spread, back, complete cover");
   float[] widths={210,400,210,440};float[] heights={200,190,200,200};
   for(int i=1;i<=4;i++){var size=reader.GetPageSize(i);Check(Math.Abs(size.Width-widths[i-1])<.01&&Math.Abs(size.Height-heights[i-1])<.01,"page geometry "+i);}
  }
  string t=PdfText.Read(output,1);Check(t.Contains("FRONT")&&!t.Contains("BACK")&&!t.Contains("SPINE"),"front has only front text, no hidden back or spine text");
  t=PdfText.Read(output,2);Check(t.Contains("INSIDE-L")&&t.Contains("INSIDE-R"),"inside spread preserved");
  t=PdfText.Read(output,3);Check(t.Contains("BACK")&&!t.Contains("FRONT")&&!t.Contains("SPINE"),"back panel only");
  t=PdfText.Read(output,4);Check(t.Contains("FRONT")&&t.Contains("BACK")&&t.Contains("SPINE"),"complete final wraparound including spine");
  var parser=new McfParser(new MemoryStream(Encoding.UTF8.GetBytes(xml)));var cover=parser.nextPage();var bg=cover.areas.OfType<ImageBackgroundArea>().Single();float originalX=bg.rect.X;
  using(var book=new Mcfx(input)){var writer=new Cewe2pdf.PdfWriter(Path.Combine(folder,"repeat.pdf"),book);writer.writePage(cover,parser.CoverLayout.Front);writer.writePage(cover,parser.CoverLayout.Back);writer.writePage(cover);writer.close();}
  Check(bg.rect.X==originalX,"repeated cover rendering does not move source background");
  File.WriteAllText(Path.Combine(folder,"no-cover.mcf"),"<fotobook><pagenumbering position='0'/>"+inner+"</fotobook>",Encoding.UTF8);Run(Path.Combine(folder,"no-cover.mcf"),Path.Combine(folder,"no-cover.pdf"),folder);using(var reader=new iTextSharp.text.pdf.PdfReader(Path.Combine(folder,"no-cover.pdf")))Check(reader.NumberOfPages==1,"no invented covers");
  string only="<fotobook imagedir='.'><pagenumbering position='0'/>"+back+spine+front+"</fotobook>";File.WriteAllText(Path.Combine(folder,"cover-only.mcf"),only,Encoding.UTF8);Run(Path.Combine(folder,"cover-only.mcf"),Path.Combine(folder,"cover-only.pdf"),folder);using(var reader=new iTextSharp.text.pdf.PdfReader(Path.Combine(folder,"cover-only.pdf")))Check(reader.NumberOfPages==1,"unknown panel dimensions preserve full cover without crashing");
  Console.WriteLine("PASS four cultures, front/inside/back/wrap order, true spine width, mixed page sizes, text filtering, rotation, stable repeated backgrounds, no-cover and cover-only fallback");
 }
}
