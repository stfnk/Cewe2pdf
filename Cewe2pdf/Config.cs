using iTextSharp.text;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Cewe2pdf {

    class Config {

        // defaults
        private const string DEFAULT_PROGRAM_PATH = "C://Program Files//CEWE//"; // TODO: make this compatible with other OS
        private const int DEFAULT_TO_PAGE = 0;
        private const float DEFAULT_IMG_SCALE = 1.0f;

        // simple custom file format syntax:
        //    identifier=value;
        //    # indicates comment lines
        private const string CONFIG_PATH = "config.txt";

        // actual values used by program,
        // either loaded from cmd args, config file or default constants
        public static string programPath; // (required) path to installation folder
        public static int toPage;
        public static float imgScale;

        // store user-defined (via config file) background id colors
        public static Dictionary<string, BaseColor> bgColors = new Dictionary<string, BaseColor>();

        // sets config to default constants
        private static void setToDefaults() {
            programPath = DEFAULT_PROGRAM_PATH;
            toPage = DEFAULT_TO_PAGE;
            imgScale = DEFAULT_IMG_SCALE;
        }

        // read the config file if existant
        public static void initialize() {

            // assure valid values in all fields
            setToDefaults();

            // check if file exists, otherwise abort reading & keep defaults
            if (!System.IO.File.Exists(CONFIG_PATH)) {
                Log.Warning("'" + CONFIG_PATH + "' file not found, using defaults.");
                // TODO: write default file for convinience?
                return;
            } else {
                Log.Message("Reading config from '" + CONFIG_PATH + "'...");
            }

            string line;
            System.IO.StreamReader file;

            // read config file
            try {
                file = new System.IO.StreamReader(CONFIG_PATH);
            } catch (System.Exception e) {
                Log.Error("Reading config file failed with error: '" + e.Message + "'");
                return;
            }

            while ((line = file.ReadLine()) != null) {
                // ignore comment & blank lines
                if (line.StartsWith("#") || line == "") continue;

                // split identifier and value
                string[] tokens = line.Split("=");

                // each non-comment line *must* contain 2 tokens, identifier and value
                if (tokens.Length != 2) {
                    Log.Error("Parsing '" + line + "' failed. '" +
                      (tokens.Length < 2 ?
                      "Too few tokens in line." :
                      "Too many tokens in line.") +
                      " Skipping this line.");
                    continue;
                }

                switch (tokens.First()) {
                    case "program_path":
                        programPath = tokens.Last().Replace(";", "");
                        Log.Info("   program_path: '" + programPath + "'.");
                        break;
                    case "to_page":
                        toPage = Convert.ToInt32(tokens.Last().Replace(";", ""));
                        Log.Info("   to_page: " + toPage);
                        break;
                    case "img_scale":
                        imgScale = (float)Convert.ToDouble(tokens.Last().Replace(";", "").Replace(".", ","));
                        Log.Info("   img_scale: " + imgScale);
                        break;

                    case "bg_color_id":
                        string[] col = tokens.Last().Split(":");
                        string argb = col.Last().Replace(";","");
                        if (argb.Length != 9) {
                            Log.Error("bg_color_id format wrong. Expected html style argb (#aarrggbb), got '" + argb + "'.");
                            continue;
                        }
                        bgColors.TryAdd(col.First(), pdfWriter.argb2BaseColor(argb));
                        break;

                    default:
                        if (tokens.First() == "") continue;
                        Log.Warning("Unexpected token '" + tokens.First() + "' skipping...");
                        break;
                }
            }

            file.Close();

            // check if program path is valid
            if (!System.IO.Directory.Exists(programPath)) Log.Error("Directory at '" + programPath + "' does not exist."); // TODO: stop program? What is actually expected to happen?

            if (bgColors.Count > 0)
                Log.Info("Registered " + bgColors.Count + " additional background colors.");
        }
    }
}
