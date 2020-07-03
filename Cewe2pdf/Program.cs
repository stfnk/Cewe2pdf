using System;

namespace Cewe2pdf {

    class Program {

        static private mcfParser _parser;
        static private pdfWriter _writer;

        static void Main(string[] args) {

            string mcfPath = "Test.mcf";
            string pdfPath = "Converted.pdf";

            // basic commandline interface
            if (args.Length <= 1) {
                Console.WriteLine("\n\tUsage: cewe2pdf <source.mcf> <destination.pdf>\n");
               return;
            } else if (args.Length >= 2) {
               mcfPath = args[0];
               pdfPath = args[1];
            } else {
               Console.WriteLine("\tinvalid arguments.");
               return;
            }

            // at least some error checking...
            if (mcfPath.EndsWith(".mcf")) { Console.WriteLine("\ninvalid argument (" + mcfPath + "); file is not a .mcf file."); return; }
            if (pdfPath.EndsWith(".pdf")) { Console.WriteLine("\ninvalid argument (" + pdfPath + "); file is not a .pdf file."); return; }

            _parser = new mcfParser(mcfPath);
            _writer = new pdfWriter(pdfPath);

            Page next = _parser.nextPage();

            while (next != null) {
               _writer.writePage(next);
               next = _parser.nextPage();
            }

            _writer.close();
        }
    }
}
