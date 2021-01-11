using System;

namespace Cewe2pdf {

    class Program {

        public static string mcfPath = "";
        public static string pdfPath = "";

        static void Main(string[] args) {

#if DEBUG || _DEBUG
            Log.level = Log.Level.Info;
            Log.Info("Running Debug configuration");
#else
            Log.level = Log.Level.Message;
#endif

            // initializes config with either defaults or from config file
            Config.initialize();

            if (String.IsNullOrWhiteSpace(Config.programPath)) {
                Log.Error("Cewe Installation directory not found. Please specify installation folder in config.txt next to Cewe2pdf.exe."); // TODO: add manual link explaining config file.
                return;
            }

            // overwrites config with arguments if given
            CmdArgParser.parse(args);

            // check for valid input file
            if (mcfPath == "") { Log.Error("No input.mcf file specified."); return; }
            if (!System.IO.File.Exists(mcfPath)) { Log.Error("'" + mcfPath + "' does not exist.'"); return; }

            // allow only input file as argument
            if (pdfPath == "") pdfPath = mcfPath.Replace(".mcf", ".pdf");

            // for user information only
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

            // init design id converter
            DesignIdConverter.initDesignIdDatabase();

            // initialize with given files
            mcfParser parser = new mcfParser(mcfPath);
            pdfWriter writer = new pdfWriter(pdfPath);

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
            Log.Message("Writing '" + pdfPath + "'.");
            writer.close();
            Log.Message("Conversion finished in " + timer.ElapsedMilliseconds/1000.0f + " seconds.");
            Log.writeLogFile();
        }
    }
}
