using iTextSharp.text;
using Org.BouncyCastle.Asn1;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;

namespace Cewe2pdf {
    class DesignIdConverter {

        const string CW_PATH = "C:\\Users\\Stefan\\Desktop\\Medien\\Programme\\Cewe";

        private static Dictionary<string, string> _idPaths;

        public static void initDesignIdDatabase() {
            if (_idPaths != null) {
                Log.Error("DesignIdConverter already initialized.");
                return;
            }

            string path = CW_PATH + "\\Resources\\ls-R";

            _idPaths = new Dictionary<string, string>();

            // parse 
            string line;

            // Read the file and display it line by line.  
            System.IO.StreamReader file = new System.IO.StreamReader(path);

            Log.Info("Loading Design IDs from '" + path + "'.");

            while ((line = file.ReadLine()) != null) {
                // TODO for now only looks for backgrounds.
                if (line.StartsWith("photofun/backgrounds") && line.EndsWith(".webp")) {
                    string id = line.Split("/").Last().Replace(".webp", "");
                    _idPaths.TryAdd(id, line);
                }
            }

            file.Close();

            Log.Info("Loaded " + _idPaths.Count + " backgrounds.");

            getFromID("6878");
        }

        public static BaseColor getBaseColorFromID(string pId) {
            BaseColor ret;

            // check if color is already cached
            Config.bgColors.TryGetValue(pId, out ret);
            if (ret != null) {
                Log.Info("Using cached color '" + pId + "'.");
                return ret;
            }

            // load color from .webp file
            Bitmap bmp = getFromID(pId);
            ret = new BaseColor(bmp.GetPixel(0, 0)); // assume every pixel holds the color...

            // cache for fast reuse
            Config.bgColors.Add(pId, ret);

            return ret;
        }

        private static Bitmap getFromID(string pId) {
            if (_idPaths == null) {
                Log.Error("DesignIdConverter not initialized.");
                return null;
            }

            string path = "";
            _idPaths.TryGetValue(pId, out path);

            if (path == "") {
                Log.Error("Design ID '" + pId + "' not found.");
                return null;
            }

            path = CW_PATH + "//Resources//" + path;

            Log.Info("Loading webp from path: " + path);

            WebPWrapper.WebP webP = new WebPWrapper.WebP();

            return webP.Load(path);
        }
    }
}
