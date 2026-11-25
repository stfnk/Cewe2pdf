using System;
using System.Collections.Generic;

namespace Cewe2pdf {

    class Program {
        public static readonly string version = "v0.4.4-book1";
        public static string mcfxPath = "";
        public static string pdfPath = "";

        private const string CONFIG_PATH = "config.txt";

        static void Main(string[] args) {
            try { Run(args); }
            catch (Exception e) { Log.Error(e.Message); Environment.ExitCode = 1; }
        }

        static void Run(string[] args) {

#if DEBUG || _DEBUG
            Log.level = Log.Level.Info;
            Log.Message("Cewe2pdf " + version + " [Debug]");
#else
            Log.level = Log.Level.Error;
            Log.Message("Cewe2pdf " + version + " [Release]");
#endif

            Log.Message("Copyright (c) 2020 Stefan Kreller; changes (c) 2026 Robert Brendel. MIT application; LGPL-2.0-only PDF library; no warranty. Source and notices: ANLEITUNG.md / LICENSE / THIRD-PARTY-NOTICES.md");
            Log.Message("iTextSharp.LGPLv2.Core 3.8.8: Vahid Nasiri and upstream contributors; LGPL-2.0-only, see licenses/itextsharp-lgplv2-core-LICENSE.md.");
            List<string> cmdoptions;

            if (!CmdArgParser.parse(args, out cmdoptions)) return;

            // check for valid input file
            if (String.IsNullOrWhiteSpace(mcfxPath)) { Log.Error("No input.mcf or input.mcfx file specified."); Environment.ExitCode = 1; return; }
            if (!System.IO.File.Exists(mcfxPath)) { Log.Error("'" + mcfxPath + "' does not exist.'"); Environment.ExitCode = 1; return; }

            // allow only input file as argument
            if (String.IsNullOrWhiteSpace(pdfPath)) pdfPath = System.IO.Path.ChangeExtension(mcfxPath, null) + "-converted.pdf";
            if (System.IO.Path.GetFullPath(mcfxPath).Equals(System.IO.Path.GetFullPath(pdfPath), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Output must not overwrite the source project.");

            // set config settings
            Config.setMissingFromOptions(cmdoptions.ToArray());
            Config.setMissingFromFile(CONFIG_PATH);
            Config.setMissingToDefaults();

            Log.Info("Using " + Config.print());

            if (String.IsNullOrWhiteSpace(Config.ProgramPath) || !System.IO.Directory.Exists(Config.ProgramPath + "//Resources")) {
                Log.Error("Cewe Installation directory not found. Please specify installation folder in config.txt next to Cewe2pdf.exe. Check (https://github.com/stfnk/Cewe2pdf#troubleshooting) for more information.");
                Environment.ExitCode = 1;
                return;
            }

            // measure runtime to calculate remaining time
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

            // initialize with given files
            using Mcfx mcfx = new Mcfx(mcfxPath);
            McfParser parser = new McfParser(mcfx.getMcfFile());
            PdfWriter writer = new PdfWriter(pdfPath, mcfx);

            if (Config.ToPage > 0)
                Log.Message("Converting " + Config.ToPage.ToString() + " pages.");

            Log.Message("Starting conversion. This may take several minutes.");

            // keep track to calculate progress to report to user
            int count = 0;
            int pageCount = Config.ToPage > 0 ? Config.ToPage : parser.pageCount();

            Page cover = null;
            // iterate through all pages
            while (true) {
                Page next = parser.nextPage();
                if (next == null) break; // reached end of book
                Log.Message("[" + (Math.Min(count / (float)pageCount * 100, 100.0f)).ToString("F1") + "%]\tprocessing Page " + Math.Min(count + 1, pageCount) + "/" + pageCount + "...", false);
                long lastTime = timer.ElapsedMilliseconds;
                if (next.type == Page.Type.Fullcover && parser.CoverLayout != null) {
                    if(cover!=null) throw new InvalidOperationException("More than one assembled cover found.");
                    cover = next;
                    writer.writePage(next,parser.CoverLayout.Front);
                } else writer.writePage(next);
                float pageTime = (timer.ElapsedMilliseconds - lastTime) / 1000.0f;
                count++;
                if (count == Config.ToPage) break;
                Log.Message("\tremaining: ~" + MathF.Ceiling(timer.ElapsedMilliseconds / count * (pageCount - count) / 1000.0f / 60.0f).ToString() + " minutes.");
            }

            if(cover!=null) {
                writer.writePage(cover,parser.CoverLayout.Back);
                writer.writePage(cover);
                Log.Info("Appended back cover and complete wraparound cover (including spine).");
            }
            // close files
            Log.Message("Writing '" + pdfPath + "'.");
            writer.close();
            Log.Message("Conversion finished after " + timer.ElapsedMilliseconds / 1000.0f + " seconds.");
            Log.writeLogFile();
        }
    }
}
