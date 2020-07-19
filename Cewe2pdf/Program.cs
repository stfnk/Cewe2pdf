using System;

namespace Cewe2pdf {

    class Program {

        static void Main(string[] args) {

            // settings that should get a commandline interface at some point
            string mcfPath = "../2018 06 Diverses.mcf";
            string pdfPath = "ConvertedTest18.pdf";
            int toPage = 0; // process to page (useful for testing) 0 to process all.
         
            // make sure to print commandline input errors
            Log.level = Log.Level.Error;

            //// simple commandline interface
            //if (args.Length <= 1) {
            //    Log.Message("\n\tUsage:\tcewe2pdf <source.mcf> <destination.pdf> <options>\n");
            //    Log.Message("\tOptions:\n\t\t[-p x]\tonly convert up to x pages: '-p 4' converts 4 double pages.");
            //    return;
            //} else if (args.Length >= 2) {
            //    mcfPath = args[0];
            //    pdfPath = args[1];
            //}
            //if (args.Length == 4) {
            //    if (args[2] == "-p")
            //        toPage = Convert.ToInt32(args[3]);
            //}

            // at least some error checking...
            if (!mcfPath.EndsWith(".mcf")) { Log.Error("\ninvalid argument (" + mcfPath + "); file is not a .mcf file."); return; }
            if (!pdfPath.EndsWith(".pdf")) { Log.Error("\ninvalid argument (" + pdfPath + "); file is not a .pdf file."); return; }

            // for user information only
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

            // only show user messages
            Log.level = Log.Level.Info;

            Log.Message("Loading '" + mcfPath + "'...");

            // initialize with given files
            mcfParser parser = new mcfParser(mcfPath);
            pdfWriter writer = new pdfWriter(pdfPath);

            if (toPage > 0)
                Log.Message("Converting " + toPage.ToString() + " pages.");

            Log.Message("Starting conversion. This may take several minutes.");

            // for user information
            int count = 0;
            int pageCount = toPage > 0 ? toPage : parser.pageCount();

            //  iterate through all pages
            while (true) {
                Log.Message("[" + (count / (float)pageCount * 100).ToString("F1") + "%]\tprocessing Page " + (count+1) + "/" + pageCount + "...", false);
                long lastTime = timer.ElapsedMilliseconds;
                Page next = parser.nextPage();
                if (next == null) break; // reached end of book
                writer.writePage(next);
                float pageTime = (timer.ElapsedMilliseconds - lastTime) / 1000.0f;
                count++;
                if (count == toPage) break;
                Log.Message("\tremaining: ~" + MathF.Ceiling(timer.ElapsedMilliseconds / count * (pageCount - count) / 1000.0f / 60.0f).ToString() + " minutes.");
            }

            // close files
            Log.Message("Saving '" + pdfPath + "'.");
            writer.close();
            Log.Message("Finished in " + timer.ElapsedMilliseconds/1000.0f + " seconds.");
            Log.writeLogFile();
        }
    }
}
