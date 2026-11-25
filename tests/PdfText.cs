using UglyToad.PdfPig;
static class PdfText {
    internal static string Read(string file, int page) {
        using var doc=PdfDocument.Open(file);
        return doc.GetPage(page).Text;
    }
}
