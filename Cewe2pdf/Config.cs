using iTextSharp.text;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Cewe2pdf {
    class Config {

        public static string programPath = "";  // (required) path to installation folder
        public static string mcfPath = "";      // (optional) path to mcf 
        public static string pdfPath = "";      // (optional) path to pdf
        public static int toPage = 0;           // (optional) number of pages to parse
        public static float imgScale = 1.0f;

        public static Dictionary<string, BaseColor> bgColors = new Dictionary<string, BaseColor>();

        public static void readConfig(string pPath) {

            string line;
            System.IO.StreamReader file;

            // read config file
            try {
                file = new System.IO.StreamReader(pPath);
            } catch (System.Exception e) {
                Log.Error("Opening config file failed with error: '" + e.Message + "'");
                // TODO write default file for convinience?
                return;
            }

            while ((line = file.ReadLine()) != null) {

                if (line.StartsWith("#") || line == "") continue; // ignore comment & blank lines

                string[] tokens = line.Split("=");

                switch (tokens.First()) {
                    case "program_path":
                        programPath = tokens.Last().Replace(";", "");
                        Log.Info("   program_path: '" + programPath + "'.");
                        break;
                    case "mcf_path":
                        mcfPath = tokens.Last().Replace(";", "");
                        Log.Info("   mcf_path: '" + mcfPath + "'.");
                        break;
                    case "pdf_path":
                        pdfPath = tokens.Last().Replace(";", "");
                        Log.Info("   pdf_path: '" + pdfPath + "'.");
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
                        Log.Warning("Unexpected token '" + tokens.First() + "' in config file.");
                        break;
                }
            }

            file.Close();

            Log.Info("Registered " + bgColors.Count + " additional background colors.");
        }
    }
}
