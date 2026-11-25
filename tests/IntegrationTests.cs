using System;
using System.Linq;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Text;
using Cewe2pdf;

class IntegrationTests {
    static void Main(string[] args) {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
        if (args.Length > 0 && args[0] == "--audit") {
            var parser = new McfParser(new Mcfx(args[1]).getMcfFile());
            int areas = 0, nonempty = 0;
            var expectedAreas = new System.Collections.Generic.List<string>();
            Page page;
            while ((page = parser.nextPage()) != null) foreach (var area in page.areas.OfType<TextArea>()) {
                areas++;
                if (!string.IsNullOrWhiteSpace(area.text)) { nonempty++; expectedAreas.Add(area.text); }
            }
            Console.WriteLine($"Parsed {areas} text areas, {nonempty} nonempty");
            using var auditReader = new iTextSharp.text.pdf.PdfReader(args[2]);
            string all = string.Concat(Enumerable.Range(1, auditReader.NumberOfPages).Select(i => PdfText.Read(args[2], i)));
            string Normalize(string value) => string.Concat(value.Where(c => !char.IsWhiteSpace(c)));
            string normalized = Normalize(all);
            var missing = expectedAreas.Where(t => !normalized.Contains(Normalize(t))).ToList();
            Console.WriteLine($"Complete text areas found in PDF: {expectedAreas.Count - missing.Count}/{expectedAreas.Count}");
            foreach (var value in missing) Console.WriteLine("INCOMPLETE AREA: " + value.Replace("\n", " | "));
            if (missing.Count > 0) throw new Exception("Some original text areas are incomplete");
            CheckFonts(auditReader);
            Console.WriteLine($"PASS original PDF {auditReader.NumberOfPages} pages, {all.Length} extracted characters, cover and final greeting present");
            return;
        }
        if (args.Length > 0 && args[0] == "--inspect") {
            var db = new Mcfx(args[1]);
            var doc = new System.Xml.XmlDocument();
            doc.Load(db.getMcfFile());
            var texts = doc.SelectNodes("//area[@areatype='textarea' or @areatype='spinetextarea']/text");
            Console.WriteLine("Text areas: " + texts.Count);
            foreach (System.Xml.XmlNode node in texts.Cast<System.Xml.XmlNode>().Take(3)) Console.WriteLine(node.OuterXml);
            return;
        }
        if (args.Length > 0 && args[0] == "--real") {
            typeof(Cewe2pdf.Program).GetMethod("Main", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Invoke(null, new object[] { new[] { args[1], args[2], "-program_path=" + args[3] } });
            using var pdf = new iTextSharp.text.pdf.PdfReader(args[2]);
            Console.WriteLine("PASS de-DE real photobook PDF: " + pdf.NumberOfPages + " pages");
            return;
        }
        string folder = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(Path.Combine(folder, "Resources", "photofun", "fonts"));
        string input = Path.Combine(folder, "synthetic.mcfx");
        string output = Path.Combine(folder, "synthetic.pdf");
        if (File.Exists(input)) File.Delete(input);
        string xml = """
        <fotobook><pagenumbering margin="10" verticalMargin="10" fontsize="11.62" fontfamily="Arial" textcolor="#ff000000" /><page type="normalpage" pagenr="1">
        <bundlesize width="1000.5" height="700.25" />
        <area areatype="textarea"><position left="20.5" top="20.5" width="900" height="500" rotation="0" />
        <text><textFormat font="Arial,11.62,0" foregroundColor="#ff000000" backgroundColor="#00ffffff" />
        <![CDATA[<html><body style="font-family: Arial; font-size: 11.62pt; font-weight: 400; color: #193C41;"><p>Kulturtest 11.62</p><p>Direkttext <span style="font-weight:700">Fett</span><br/>ÄÖÜ äöü ß € <span style="font-style: italic">Kursiv</span></p><p><span style="font-family: MissingCeweFont">Fallback</span></p></body></html>]]>
        </text></area><area areatype="textarea"><position left="20.5" top="550" width="900" height="100" rotation="0"/>
        <text><textFormat font="Arial,11.62,0" Alignment="ALIGNLEFT" foregroundColor="#ff000000" backgroundColor="#00000000"/>
        <![CDATA[<html><body><p>Nur Absatz ohne Span</p></body></html>]]></text></area></page><page type="normalpage" pagenr="2" /></fotobook>
        """;
        using (var connection = new SQLiteConnection("Data Source=" + input)) {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE Files (Data BLOB, Filename TEXT, LastModified INTEGER)";
            command.ExecuteNonQuery();
            command.CommandText = "INSERT INTO Files VALUES (@data, 'data.mcf', 0)";
            command.Parameters.AddWithValue("@data", Encoding.UTF8.GetBytes(xml));
            command.ExecuteNonQuery();
        }
        if (args.Length > 1 && args[1] == "--fixture-only") return;
        typeof(Cewe2pdf.Program).GetMethod("Main", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Invoke(null, new object[] { new[] { input, output, "-program_path=" + folder } });
        using var reader = new iTextSharp.text.pdf.PdfReader(output);
        if (reader.NumberOfPages != 1) throw new Exception("Expected one PDF page");
        string text = PdfText.Read(output, 1);
        if (!text.Contains("Kulturtest 11.62")) throw new Exception("PDF text missing");
        foreach (string expected in new[] { "Direkttext Fett", "ÄÖÜ äöü ß €", "Kursiv", "Fallback", "Nur Absatz ohne Span" })
            if (!text.Contains(expected)) throw new Exception("Missing text: " + expected + " actual: " + text);
        CheckFonts(reader);
        Console.WriteLine("PASS direct p text, nested span, br, mixed styles, Unicode, missing-font fallback and embedded fonts");
        Console.WriteLine("PASS de-DE SQLite -> McfParser.nextPage -> PDF, one page and expected text");
    }
    static void CheckFonts(iTextSharp.text.pdf.PdfReader reader) {
        int count = 0;
        for (int i = 1; i < reader.XrefSize; i++) {
            var dict = reader.GetPdfObject(i) as iTextSharp.text.pdf.PdfDictionary;
            if (dict == null || !iTextSharp.text.pdf.PdfName.Fontdescriptor.Equals(dict.GetAsName(iTextSharp.text.pdf.PdfName.TYPE))) continue;
            if (dict.Get(iTextSharp.text.pdf.PdfName.Fontfile) == null && dict.Get(iTextSharp.text.pdf.PdfName.Fontfile2) == null && dict.Get(iTextSharp.text.pdf.PdfName.Fontfile3) == null)
                throw new Exception("Font is not embedded: " + dict);
            count++;
        }
        if (count == 0) throw new Exception("No embedded font descriptors");
        Console.WriteLine("PASS embedded font descriptors: " + count);
    }

}



