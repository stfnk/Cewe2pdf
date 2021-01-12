using iTextSharp.text;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Cewe2pdf {

    class Config {

        // defaults
        private static readonly string[] DEFAULT_PROGRAM_PATHS = new string[] {
            // TODO: make this compatible with other OS
            "C://Program Files//CEWE//CEWE Fotowelt//",
            "C://Program Files (x86)//CEWE//CEWE Fotowelt//",
            "C://Program Files//CEWE//",
            "C://Program Files (x86)//CEWE//",
        };
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
            // check all default locations
            foreach(string path in DEFAULT_PROGRAM_PATHS) {
                if (System.IO.Directory.Exists(path)) {
                    programPath = path;
                    break;
                }
            }
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
                Log.Info("Reading config from '" + CONFIG_PATH + "'...");
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
                if (line.StartsWith("#") || String.IsNullOrWhiteSpace(line)) continue;

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
                        string wantedProgramPath = tokens.Last().Replace(";", "");
                        // is this a valid path?
                        if (!System.IO.Directory.Exists(wantedProgramPath)) Log.Error("program_path (" + wantedProgramPath + ") loaded from file is invalid.");
                        else programPath = wantedProgramPath;
                        Log.Info("   program_path: '" + programPath + "'");
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
                        if (String.IsNullOrWhiteSpace(tokens.First())) continue;
                        Log.Warning("Unexpected token '" + tokens.First() + "' skipping...");
                        break;
                }
            }

            file.Close();

            if (bgColors.Count > 0)
                Log.Info("Registered " + bgColors.Count + " additional background colors.");
        }
    }
}
