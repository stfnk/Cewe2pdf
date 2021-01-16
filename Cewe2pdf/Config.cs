using iTextSharp.text;
using Org.BouncyCastle.Crypto.Engines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cewe2pdf {

    class Config {

        // default config settings
        private static readonly string[] DEFAULT_PROGRAM_PATHS = new string[] {
            "C://Program Files//CEWE//CEWE Fotowelt",
            "C://Program Files (x86)//CEWE//CEWE Fotowelt",

            "C://Program Files//CEWE//Mein CEWE FOTOBUCH",
            "C://Program Files (x86)//CEWE//Mein CEWE FOTOBUCH",

            "C://Program Files//CEWE Fotowelt",
            "C://Program Files (x86)//CEWE Fotowelt",

            "C://Program Files//CEWE Photoworld",
            "C://Program Files (x86)//CEWE Photoworld",

            "C://Program Files//CEWE",
            "C://Program Files (x86)//CEWE",
        };
        private const int DEFAULT_TO_PAGE = 0;
        private const float DEFAULT_IMG_SCALE = 1.0f;

        // config options available to config.txt and commmandline parser
        public static readonly Dictionary<string, string> optionList = new Dictionary<string, string>() {
            { "program_path="+"\"C:\\Program Files\"", "Path to the cewe software installation." },
            { "to_page="+DEFAULT_TO_PAGE, "Number of pages to convert. 0 converts all pages."},
            { "img_scale="+DEFAULT_IMG_SCALE, "Pixel size of images in pdf. Use smaller values for higher resolution images." },
        };

        public static string ProgramPath { get; private set; } = "";
        public static int ToPage { get; set; } = -1;
        public static float ImgScale { get; set; } = -1.0f;

        public static void setMissingFromOptions(string[] pOptions) {
            foreach (string option in pOptions) {
                if (!option.Contains("=")) {
                    Log.Error("invalid option: '" + option + "'.");
                    continue;
                }
                string[] tokens = option.Split("=");
                string key = toPascalCase(tokens.First());
                string value = tokens.Last();
                PropertyInfo info = typeof(Config).GetProperty(key);
                if (info == null) {
                    Log.Warning("property '" + option + "' does not exist. Skipping.");
                    continue;
                }

                // property exists. Let's parse it.
                string type = info.PropertyType.Name;
                switch (type) {
                    case "String":
                        if (String.IsNullOrWhiteSpace((string)info.GetValue(null))) {
                            info.SetValue(null, value.Replace("\"",""));
                            Log.Info("Set '" + key + "' to '" + value + "'.");
                        }
                        break;
                    case "Int32":
                        if ((int)info.GetValue(null) < 0) {
                            int i32 = Convert.ToInt32(value);
                            info.SetValue(null, i32);
                            Log.Info("Set '" + key + "' to '" + value + "'.");
                        }
                        break;
                    case "Single":
                        if ((float)info.GetValue(null) < 0.0f) {
                            float single = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                            info.SetValue(null, single);
                            Log.Info("Set '" + key + "' to '" + value + "'.");
                        }
                        break;
                    default:
                        Log.Warning("Unhandled type: '" + type + "'.");
                        break;
                }
            }
        } 

        public static void setMissingToDefaults() {
            Log.Info("Loading config defaults...");

            // some extra handling for program path in case given is invalid or null try default paths
            if (!System.IO.Directory.Exists(ProgramPath + "//Resources")) {
                foreach (string path in DEFAULT_PROGRAM_PATHS) {
                    if (System.IO.Directory.Exists(path + "//Resources")) {
                        ProgramPath = path;
                        break;
                    }
                }
            }
            setMissingFromOptions(optionList.Keys.ToArray());
        }

        public static void setMissingFromFile(string pFile) {

            // check if file exists, otherwise abort
            if (!System.IO.File.Exists(pFile)) {
                Log.Warning("Config file at: '" + pFile + "' not found, falling back to defaults.");
                return;
            } else {
                Log.Info("Loading config from '" + pFile + "'...");
            }

            string line;
            System.IO.StreamReader file;

            // read config file
            try {
                file = new System.IO.StreamReader(pFile);
            } catch (System.Exception e) {
                Log.Error("Reading config file failed with error: '" + e.Message + "'");
                return;
            }

            List<string> options = new List<string>();

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

                options.Add(line);   
            }
            file.Close();

            setMissingFromOptions(options.ToArray());
        }

        public static string print() {
            string log = "Config:";
            foreach (PropertyInfo info in typeof(Config).GetProperties()) {
                log += "\n\t\t" + info.Name + "=" + info.GetValue(null).ToString();
            }
            return log;
        }

        public static string toPascalCase(string pSnakeCase) {
            // TODO not reliable. // TODO move to utils class?
            string[] words = pSnakeCase.ToLower().Split("_");
            string camelCase = "";
            foreach (string word in words) {
                if (String.IsNullOrWhiteSpace(word)) continue;
                string upperfirst = word.Substring(0, 1).ToUpper();
                camelCase += upperfirst + word.Substring(1); 
            }
            return camelCase;
        }
    }
}
