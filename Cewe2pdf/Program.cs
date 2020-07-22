using System;

namespace Cewe2pdf {

    class Program {

        static void Main(string[] args) {

            Log.Message("Reading 'config.txt'...");
            Config.readConfig("config.txt");

            // simple commandline interface
            if (args.Length == 1 && args[0] == "--help") {
                Log.Message("\n\tUsage:\tcewe2pdf <source.mcf> <destination.pdf> <options>\n");
                Log.Message("\tOptions:\n\t\t[-p x]\tonly convert up to x pages: '-p 4' converts 4 double pages.");
                return;
            } else if (args.Length >= 2) {
                Config.mcfPath = args[0];
                Config.pdfPath = args[1];
            }
            if (args.Length == 4) {
                if (args[2] == "-p")
                    Config.toPage = Convert.ToInt32(args[3]);
            }

            // at least some error checking...
            if (!Config.mcfPath.EndsWith(".mcf")) { Log.Error("invalid argument (" + Config.mcfPath + "); file is not a .mcf file."); return; }
            if (Config.pdfPath == "") Config.pdfPath = Config.mcfPath.Replace(".mcf", ".pdf");
            if (!Config.pdfPath.EndsWith(".pdf")) { Log.Error("invalid argument (" + Config.pdfPath + "); file is not a .pdf file."); return; }

            // for user information only
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

            // only show user messages
            //Log.level = Log.Level.Message;

            Log.Message("Loading '" + Config.mcfPath + "'...");

            // init design id converter
            DesignIdConverter.initDesignIdDatabase();

            // initialize with given files
            mcfParser parser = new mcfParser(Config.mcfPath);
            pdfWriter writer = new pdfWriter(Config.pdfPath);

            if (Config.toPage > 0)
                Log.Message("Converting " + Config.toPage.ToString() + " pages.");

            Log.Message("Starting conversion. This may take several minutes.");

            // for user information
            int count = 0;
            int pageCount = Config.toPage > 0 ? Config.toPage : parser.pageCount();

            //  iterate through all pages
            while (true) {
                Log.Message("[" + (count / (float)pageCount * 100).ToString("F1") + "%]\tprocessing Page " + (count+1) + "/" + pageCount + "...", false);
                long lastTime = timer.ElapsedMilliseconds;
                Page next = parser.nextPage();
                if (next == null) break; // reached end of book
                writer.writePage(next);
                float pageTime = (timer.ElapsedMilliseconds - lastTime) / 1000.0f;
                count++;
                if (count == Config.toPage) break;
                Log.Message("\tremaining: ~" + MathF.Ceiling(timer.ElapsedMilliseconds / count * (pageCount - count) / 1000.0f / 60.0f).ToString() + " minutes.");
            }

            // close files
            Log.Message("Saving '" + Config.pdfPath + "'.");
            writer.close();
            Log.Message("Finished in " + timer.ElapsedMilliseconds/1000.0f + " seconds.");
            Log.writeLogFile();
        }
    }
}
