using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cewe2pdf {

    class Program {
        public static readonly string version = "v0.3.0-alpha4";
        public static string mcfPath = "";
        public static string pdfPath = "";

        private const string CONFIG_PATH = "config.txt";

        static void Main(string[] args) {

#if DEBUG || _DEBUG
            Log.level = Log.Level.Info;
            Log.Message("Cewe2pdf " + version + " [Debug]");
#else
            Log.level = Log.Level.Error;
            Log.Message("Cewe2pdf " + version + " [Release]");
#endif

            List<string> cmdoptions;
            
            if (!CmdArgParser.parse(args, out cmdoptions)) return;

            // check for valid input file
            if (String.IsNullOrWhiteSpace(mcfPath)) { Log.Error("No input.mcf file specified."); return; }
            if (!System.IO.File.Exists(mcfPath)) { Log.Error("'" + mcfPath + "' does not exist.'"); return; }

            // allow only input file as argument
            if (String.IsNullOrWhiteSpace(pdfPath)) pdfPath = mcfPath.Replace(".mcf", "-converted.pdf");
            
            // set config settings
            Config.setMissingFromOptions(cmdoptions.ToArray());
            Config.setMissingFromFile(CONFIG_PATH);
            Config.setMissingToDefaults();

            Log.Info("Using " + Config.print());

            if (String.IsNullOrWhiteSpace(Config.ProgramPath) || !System.IO.Directory.Exists(Config.ProgramPath + "//Resources")) {
                Log.Error("Cewe Installation directory not found. Please specify installation folder in config.txt next to Cewe2pdf.exe. Check (https://github.com/stfnk/Cewe2pdf#troubleshooting) for more information.");
                return;
            }

            // for user information only
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

            // init design id converter
            DesignIdConverter.initDesignIdDatabase();

            // initialize with given files
            mcfParser parser = new mcfParser(mcfPath);
            pdfWriter writer = new pdfWriter(pdfPath);

            if (Config.ToPage > 0)
                Log.Message("Converting " + Config.ToPage.ToString() + " pages.");

            Log.Message("Starting conversion. This may take several minutes.");

            // for user information
            int count = 0;
            int pageCount = Config.ToPage > 0 ? Config.ToPage : parser.pageCount();

            //  iterate through all pages
            while (true) {
                Log.Message("[" + (count / (float)pageCount * 100).ToString("F1") + "%]\tprocessing Page " + (count+1) + "/" + pageCount + "...", false);
                long lastTime = timer.ElapsedMilliseconds;
                Page next = parser.nextPage();
                if (next == null) break; // reached end of book
                writer.writePage(next);
                float pageTime = (timer.ElapsedMilliseconds - lastTime) / 1000.0f;
                count++;
                if (count == Config.ToPage) break;
                Log.Message("\tremaining: ~" + MathF.Ceiling(timer.ElapsedMilliseconds / count * (pageCount - count) / 1000.0f / 60.0f).ToString() + " minutes.");
            }

            // close files
            Log.Message("Writing '" + pdfPath + "'.");
            writer.close();
            Log.Message("Conversion finished after " + timer.ElapsedMilliseconds/1000.0f + " seconds.");
            Log.writeLogFile();
        }
    }
}
