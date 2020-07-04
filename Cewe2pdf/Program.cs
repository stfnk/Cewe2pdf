using System;

namespace Cewe2pdf {

    class Program {

        static void Main(string[] args) {

            string mcfPath = "Test.mcf";
            string pdfPath = "Converted.pdf";

            // simple commandline interface
            if (args.Length <= 1) {
                Log.Info("\n\tUsage: cewe2pdf <source.mcf> <destination.pdf>\n");
               return;
            } else if (args.Length >= 2) {
               mcfPath = args[0];
               pdfPath = args[1];
            } else {
               Log.Error("\tinvalid arguments.");
               return;
            }

            // at least some error checking...
            if (!mcfPath.EndsWith(".mcf")) { Log.Error("\ninvalid argument (" + mcfPath + "); file is not a .mcf file."); return; }
            if (!pdfPath.EndsWith(".pdf")) { Log.Error("\ninvalid argument (" + pdfPath + "); file is not a .pdf file."); return; }

            // for user information only
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

            Log.Info("Loading '" + mcfPath + "'...");

            // initialize with given files
            mcfParser parser = new mcfParser(mcfPath);
            pdfWriter writer = new pdfWriter(pdfPath);

            Log.Info("Starting conversion. This can take several minutes.");

            // for user information
            int count = 0;
            float pageAverageTime = 0.0f;
            
            //  iterate through all pages
            while (true) {
                long lastTime = timer.ElapsedMilliseconds;
                Page next = parser.nextPage();
                if (next == null) break;
                writer.writePage(next);
                float pageTime = (timer.ElapsedMilliseconds - lastTime) / 1000.0f;
                pageAverageTime = count == 0 ? pageTime : (pageAverageTime + pageTime) / 2.0f;
                count++;
                Log.Info("\tprocessing Page " + count + "/" + parser.pageCount() + "; " + (pageAverageTime * (parser.pageCount()-count)) + " seconds remaining.");
            }

            // close files
            writer.close();
            Log.Info("Finished in " + timer.ElapsedMilliseconds/1000.0f + " seconds.");
            Log.writeLog();
        }
    }
}
