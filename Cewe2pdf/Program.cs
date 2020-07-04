using System;

namespace Cewe2pdf {

    class Program {

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

            // for user information only
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

            Console.WriteLine("Loading '" + mcfPath + "'...");

            // initialize with given files
            mcfParser parser = new mcfParser(mcfPath);
            pdfWriter writer = new pdfWriter(pdfPath);

            Console.WriteLine("Starting conversion. This can take several minutes.");

            // for user information again
            int count = 0;

            //  iterate through all pages
            while (count < parser.pageCount()) {
                Page next = parser.nextPage();
                writer.writePage(next);
                count++;
                Console.WriteLine("\t...processing Page " + count + "/" + parser.pageCount() + "; " + (timer.ElapsedMilliseconds/1000.0f/count) * (parser.totalPages - count) + " seconds remaining.");
            }

            // close files
            writer.close();
            Console.WriteLine("Finished in " + timer.ElapsedMilliseconds/1000.0f + "seconds.");
        }
    }
}
