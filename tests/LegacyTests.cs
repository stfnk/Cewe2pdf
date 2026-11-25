using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Data.SQLite;
using Cewe2pdf;
class LegacyTests {
 static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
 static void Main(string[] args) {
  CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("de-DE");
  string folder=Path.GetFullPath(args[0]);Directory.CreateDirectory(folder);
  string imageFolder=Path.Combine(folder,"Bilder mit Umlaut ä");Directory.CreateDirectory(imageFolder);
  byte[] image=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aY9sAAAAASUVORK5CYII=");
  string file="O'Brien.png";File.WriteAllBytes(Path.Combine(imageFolder,file),image);
  string xml="<fotobook imagedir=\"Bilder mit Umlaut ä\"><page type=\"normalpage\" pagenr=\"1\"><bundlesize width=\"1000\" height=\"700\"/><area areatype=\"imagearea\" left=\"20.5\" top=\"30\" width=\"400\" height=\"500\" rotation=\"11.62\"><border enabled=\"0\" width=\"3\"/><image filename=\"O'Brien.png\" left=\"-2.5\" top=\"-3.5\" scale=\"1.25\"/></area></page><page type=\"normalpage\" pagenr=\"2\"/></fotobook>";
  string mcf=Path.Combine(folder,"ALT.MCF");File.WriteAllBytes(mcf,Encoding.UTF8.GetBytes(xml+"\0trailer"));
  var oldBook=new Mcfx(mcf);var oldPage=new McfParser(oldBook.getMcfFile()).nextPage();var oldImage=(ImageArea)oldPage.areas.Single();
  Check(Math.Abs(oldImage.rect.X-8.2f)<.001,"legacy position");Check(Math.Abs(oldImage.rotation-11.62f)<.001,"legacy rotation");
  Check(Math.Abs(oldImage.cutout.X+1.25f)<.001 && Math.Abs(oldImage.cutout.Y+1.75f)<.001 && Math.Abs(oldImage.scale-.5)<.001,"legacy pixel crop and scale");Check(!oldImage.border,"disabled border");
  Check(oldBook.getDataForFilename(file).SequenceEqual(image),"legacy relative image directory");
  byte[] orientedJpeg=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"fixtures","orientation6.jpg"));
  File.WriteAllBytes(Path.Combine(imageFolder,"orientation6.jpg"),orientedJpeg);
  using(var oriented=oldBook.getSystemImageForFilename("orientation6.jpg")) {
   Check(oriented.Width==30 && oriented.Height==20,"EXIF orientation survives stream disposal");
   Check(((System.Drawing.Bitmap)oriented).GetPixel(25,5).R>200,"EXIF rotation preserves expected corner");
   Cewe2pdf.PdfWriter.ExifRotate(oriented);
   Check(oriented.Width==30 && oriented.Height==20,"normalized image is not rotated twice");
  }

  string db=Path.Combine(folder,"NEU.MCFX");if(File.Exists(db))File.Delete(db);
  using(var conn=new SQLiteConnection("Data Source="+db)){conn.Open();using var cmd=conn.CreateCommand();cmd.CommandText="CREATE TABLE Files(Data BLOB,Filename TEXT,LastModified INTEGER)";cmd.ExecuteNonQuery();
   cmd.CommandText="INSERT INTO Files VALUES(@data,@name,0)";cmd.Parameters.AddWithValue("@data",Encoding.UTF8.GetBytes(xml));cmd.Parameters.AddWithValue("@name","data.mcf");cmd.ExecuteNonQuery();
   cmd.Parameters["@data"].Value=image;cmd.Parameters["@name"].Value=file;cmd.ExecuteNonQuery();}
  var newBook=new Mcfx(db);Check(newBook.getDataForFilename(file).SequenceEqual(image),"parameterized embedded filename with apostrophe");
  Check(new McfParser(newBook.getMcfFile()).nextPage().areas.Count==1,"MCFX retained");
  foreach(string p in new[]{mcf,db}){Check(CmdArgParser.parse(new[]{p},out var opts),"CLI parse");Check(Program.mcfxPath==p,"uppercase input extension");}
  Check(Cewe2pdf.PdfWriter.ColorAlpha(Cewe2pdf.PdfWriter.argb2BaseColor("4278190080"))==255,"decimal legacy ARGB");
  Check(Cewe2pdf.PdfWriter.ColorAlpha(Cewe2pdf.PdfWriter.argb2BaseColor("#000000"))==255,"RGB black is opaque");
  Check(Cewe2pdf.PdfWriter.ColorAlpha(Cewe2pdf.PdfWriter.argb2BaseColor("0"))==0,"decimal transparent background");
  var numberingParser=new McfParser(new MemoryStream(Encoding.UTF8.GetBytes("<fotobook><pagenumbering position=\"0\"/><page type=\"normalpage\" pagenr=\"1\"><bundlesize width=\"1000\" height=\"700\"/></page><page type=\"normalpage\" pagenr=\"2\"/></fotobook>")));
  Check(!Page.pageNoEnabled,"disabled automatic page numbering");
  numberingParser=new McfParser(new MemoryStream(Encoding.UTF8.GetBytes("<fotobook><pagenumbering position=\"1\"/></fotobook>")));
  Check(Page.pageNoEnabled,"enabled automatic page numbering");
  // Release builds must balance the canvas state even for an empty legacy photo placeholder.
  string emptyXml="<fotobook><pagenumbering fontsize=\"12\" fontfamily=\"Arial\" textcolor=\"#ff000000\"/><page type=\"normalpage\" pagenr=\"1\"><bundlesize width=\"1000\" height=\"700\"/><area areatype=\"imagearea\" left=\"20\" top=\"20\" width=\"100\" height=\"100\"><image/></area></page><page type=\"normalpage\" pagenr=\"2\"/></fotobook>";
  var parser=new McfParser(new MemoryStream(Encoding.UTF8.GetBytes(emptyXml)));
  Config.setMissingFromOptions(new[]{"program_path="+folder});Config.setMissingToDefaults();
  var writer=new Cewe2pdf.PdfWriter(Path.Combine(folder,"empty-placeholder.pdf"),oldBook);writer.writePage(parser.nextPage());writer.close();
  using(var reader=new iTextSharp.text.pdf.PdfReader(Path.Combine(folder,"empty-placeholder.pdf"))) Check(reader.NumberOfPages==1,"empty placeholder PDF closes successfully");
  string numberedXml="<fotobook><pagenumbering position=\"0\" fontsize=\"12\" fontfamily=\"Arial\" textcolor=\"#ff000000\"/><page type=\"normalpage\" pagenr=\"7\"><bundlesize width=\"1000\" height=\"700\"/><area areatype=\"textarea\" left=\"20\" top=\"20\" width=\"200\" height=\"200\"><text><textFormat font=\"Arial,12,0\" foregroundColor=\"#ff000000\" backgroundColor=\"0\"/><![CDATA[<html><body><p>7</p></body></html>]]></text></area></page><page type=\"normalpage\" pagenr=\"8\"/></fotobook>";
  parser=new McfParser(new MemoryStream(Encoding.UTF8.GetBytes(numberedXml)));
  writer=new Cewe2pdf.PdfWriter(Path.Combine(folder,"single-designed-page-number.pdf"),oldBook);writer.writePage(parser.nextPage());writer.close();
  using(var reader=new iTextSharp.text.pdf.PdfReader(Path.Combine(folder,"single-designed-page-number.pdf"))) {
   string content=PdfText.Read(Path.Combine(folder,"single-designed-page-number.pdf"),1);
   Check(string.Concat(content.Where(c=>!char.IsWhiteSpace(c)))=="7","designed page number appears once, no injected 7 or 8");
  }
  try {oldBook.getDataForFilename("missing.jpg");throw new Exception("missing image silently accepted");} catch(FileNotFoundException) {}
  Console.WriteLine("PASS legacy coordinates/crop/rotation, disabled border, MCF images, MCFX blobs, apostrophes, uppercase CLI, binary trailer and missing images under de-DE");
 }
}


