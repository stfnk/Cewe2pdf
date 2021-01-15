using iTextSharp.text;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Cewe2pdf {
    class DesignIdConverter {

        private static Dictionary<string, string> _idPaths;

        public static void initDesignIdDatabase() {

            System.Diagnostics.Debug.Assert(_idPaths == null);

            Log.Info("Initializing DesignIdConverter");
            _idPaths = new Dictionary<string, string>();

            string path = Config.programPath + "\\Resources\\ls-R";

            // check if path is valid
            if (!System.IO.File.Exists(path)) {
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
                    string id = line.Split("/").Last().Split(".").First();
                    //Log.Info("Register ID: " + id + " at: " + line);
                    id = id.Split("-").Last(); // some ids have names... keep only the id number...
                    _idPaths.TryAdd(id, line);
                }
            }

            // also search this folder for background images...
            const string dlpath = "C:\\ProgramData\\hps\\6822\\addons";
            if (System.IO.Directory.Exists(dlpath)) {
                string[] filenames = System.IO.Directory.GetFiles(dlpath, "*", System.IO.SearchOption.AllDirectories);
                Log.Info("Loading DesignIDs from '" + dlpath + "'.");
                foreach (string addfile in filenames) {
                    if (addfile.EndsWith(".jpg") || addfile.EndsWith(".bmp") || addfile.EndsWith(".webp")) {
                        Log.Info("\t found '" + addfile + "'");
                        string id = addfile.Split("/").Last().Split(".").First();
                        //Log.Info("Register ID: " + id + " at: " + line);
                        id = id.Split("-").Last(); // some ids have names... keep only the id number...
                        _idPaths.TryAdd(id, addfile);
                    }
                }
            } else {
                Log.Warning("Directory at: '" + dlpath + "' does not exist. No additional backgrounds loaded.");
            }

            file.Close();
            Log.Info("Loaded " + _idPaths.Count + " backgrounds.");
        }

        public static Bitmap getBitmapFromID(string pId) {
            if (_idPaths == null) {
                Log.Error("DesignIdConverter not initialized.");
                return null;
            }

            string path = "";
            _idPaths.TryGetValue(pId, out path);

            if (String.IsNullOrWhiteSpace(path)) {
                Log.Error("Design ID '" + pId + "' not found.");
                return null;
            }

            path = Config.programPath + "//Resources//" + path;

            if (!System.IO.File.Exists(path)) {
                Log.Error("DesignID file at: '" + path + "' does not exist.");
                return null;
            } else {
                Log.Info("Loading DesignID from path: " + path);
            }

            if (path.EndsWith(".webp")) {
                // load webp
                try {
                    WebPWrapper.WebP webP = new WebPWrapper.WebP();
                    return webP.Load(path);
                } catch (Exception e) {
                    Log.Error("Loading '" + path + "' failed with error: '" + e.Message + "'.");
                    return null;
                }
            } else if (path.EndsWith(".bmp") || path.EndsWith(".jpg") || path.EndsWith(".png") || path.EndsWith(".JPG")) {
                // load image
                try {
                    return (Bitmap)System.Drawing.Image.FromFile(path);
                } catch (Exception e) {
                    Log.Error("Loading '" + path + "' failed with error: '" + e.Message + "'.");
                    return null;
                }
            } else {
                Log.Error("Unsupported DesignID format: " + path);
                return null;
            }
        }
    }
}
