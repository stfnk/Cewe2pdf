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

        private static Dictionary<string, string> _idPaths;

        public static void initDesignIdDatabase() {
            if (_idPaths == null) {
                _idPaths = new Dictionary<string, string>();
                Log.Info("Initializing DesignIdConverter");
            } else {
                Log.Warning("DesignIdConverter already initialized.");
                return;
            }

            string path = Config.programPath + "\\Resources\\ls-R";

            // check if path is valid
            if (!System.IO.Directory.Exists(path)) {
              Log.Error("Directory at '" + path + "' does not exist. No DesignIDs loaded.");
              return;
            }

            // Read the file and display it line by line.
            System.IO.StreamReader file;
            try {
                file = new System.IO.StreamReader(path);
                Log.Info("Loading Design IDs from '" + path + "'.");
            } catch (Exception e) {
                Log.Error("Loading Design IDs failed with Error: '" + e.Message + "'");
                return;
            }

            string line;

            // this file contains all design id paths, store relevant ones for easy use in mcfParser
            while ((line = file.ReadLine()) != null) {
                // TODO: for now only looks for backgrounds.
                if (line.StartsWith("photofun/backgrounds")) {
                    string id = line.Split("/").Last();
                    _idPaths.TryAdd(id, line);
                }
            }

            file.Close();
            Log.Info("Loaded " + _idPaths.Count + " backgrounds.");
        }

        // TODO: this method will be removed once background actually uses image directly
        public static BaseColor getBaseColorFromID(string pId) {
            BaseColor ret;

            // check if color is already cached
            Config.bgColors.TryGetValue(pId, out ret);
            if (ret != null) {
                Log.Info("Using cached color '" + pId + "'.");
                return ret;
            }

            // load color from .webp file
            Bitmap bmp = getBitmapFromID(pId);
            ret = new BaseColor(bmp.GetPixel(0, 0)); // assume every pixel holds the color...

            // cache for fast reuse
            Config.bgColors.Add(pId, ret);

            Log.Warning("deprecated method DesignIDConverter.getBaseColorFromID() called.");

            return ret;
        }

        // TODO: support other formats except .webp? -> also needs to be loaded ofc
        public static Bitmap getBitmapFromID(string pId) {
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

            path = Config.programPath + "//Resources//" + path;

            Log.Info("Loading DesignID from path: " + path);

            if (path.EndsWith(".webp")) {
                // load webp
                path = path.Replace(".webp", "");
                WebPWrapper.WebP webP = new WebPWrapper.WebP();
                return webP.Load(path);
            } else {
                // TODO: load other formats
                Log.Error("Unsupported DesignID format: " + path);
                return null;
            }
        }
    }
}
