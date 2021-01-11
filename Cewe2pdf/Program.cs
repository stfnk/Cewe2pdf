using System;

namespace Cewe2pdf {

    class Program {

        static string mcfPath = "";
        static string pdfPath = "";

        static void Main(string[] args) {

            // initializes config with either defaults or from config file
            Config.initialize();

            // handle arguments, overwriting config
            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "-h" || args[i] == "--help") {
                  Console.WriteLine(
                    "Cewe2pdf usage:"
                    "\n\t    argument | description\n"
                    "\n\t      --help | lists all arguments."
                    "\n\t          -h | same as --help."
                    "\n\t    'in.mcf' | input file to convert (required)"
                    "\n\t   'out.pdf' | output file to generate (optional)"
                    "\n\t  -to-page 0 | convert only up to this page nr, 0 converts all."
                    "\n\t-quality 1.0 | pixel size of images. Use lower value for higher resolution images in .pdf." // TODO: true?
                  );
                }
                else if (args[i].EndsWith(".mcf")) mcfPath = args[i];
                else if (args[i].EndsWith(".pdf")) pdfPath = args[i];
                else if (args[i] == "-to-page") {
                  i++;
                  if (i < args.Length) config.toPage = MathF.Max(0, ToInt32(args[i]));
                }
                else if (args[i] == "-quality") {
                  i++;
                  if (i < args.Length) config.toPage = MathF.Clamp(ToFloat(args[i]), 0.0f, 1.0f); // TODO: true?
                }
                else {
                  Log.Warning("invalid argument: '" + args[i] + "'.");
                }
            }

            // check for valid input file
            if (mcfPath == "") { Log.Error("no input.mcf file specified."); return; }
            if (!File.Exists(mcfPath)) { Log.Error("'" + mcfPath + "' does not exist.'"); return; }

            // allow only input file as argument
            if (pdfPath == "") pdfPath = mcfPath.Replace(".mcf", ".pdf");

            // for user information only
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

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
