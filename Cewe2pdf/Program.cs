using System;

namespace Cewe2pdf {

    class Program {

        static void Main(string[] args) {

            // settings that should get a commandline interface at some point
            string mcfPath = "Test.mcf";
            string pdfPath = "Converted.pdf";
            int toPage = 0; // process to page (useful for testing) 0 to process all.

            // make sure to print commandline input errors
            Log.level = Log.Level.Error;

            // simple commandline interface
            if (args.Length <= 1) {
                Log.Message("\n\tUsage: cewe2pdf <source.mcf> <destination.pdf>\n");
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

            // only show user messages
            Log.level = Log.Level.Error;

            Log.Message("Loading '" + mcfPath + "'...");

            // initialize with given files
            mcfParser parser = new mcfParser(mcfPath);
            pdfWriter writer = new pdfWriter(pdfPath);

            Log.Message("Starting conversion. This may take several minutes.");

            // for user information
            int count = 0;
            int pageCount = toPage > 0 ? toPage : parser.pageCount();

            //  iterate through all pages
            while (true) {
                Log.Message("[" + (count / (float)pageCount * 100).ToString("F1") + "%]\tprocessing Page " + (count+1) + "/" + pageCount, false);
                long lastTime = timer.ElapsedMilliseconds;
                Page next = parser.nextPage();
                if (next == null) break; // reached end of book
                writer.writePage(next);
                float pageTime = (timer.ElapsedMilliseconds - lastTime) / 1000.0f;
                count++;
                Log.Message(" ...done, " + pageTime.ToString("F3") + " seconds.");
                if (count == toPage) break;
            }

            // close files
            Log.Message("Saving '" + pdfPath + "'.");
            writer.close();
            Log.Message("Finished in " + timer.ElapsedMilliseconds/1000.0f + " seconds.");
            Log.writeLogFile();
        }
    }
}
