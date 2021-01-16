using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Cewe2pdf {
    class DesignIdConverter {

        private static Dictionary<string, string> _idCache =  new Dictionary<string, string>();

        private static string getPath(string pId) {
            // check if id is already loaded in cache
            if (_idCache.ContainsKey(pId)) {
                Log.Info("Loaded DesignID '" + pId + "' from cache.");
                return _idCache[pId];
            }

            // no yet in cache. Search it.
            string path;
            
            // in installation
            path = getIdPathFromInstallation(pId);
            if (!String.IsNullOrWhiteSpace(path)) {
                _idCache.Add(pId, path);
                Log.Info("Added DesignID '" + pId + "' at '" + path + "' to cache.");
                return path;
            }

            // in downloaded content
            path = getIdPathFromProgramData(pId);
            if (!String.IsNullOrWhiteSpace(path)) {
                _idCache.Add(pId, path);
                Log.Info("Added DesignID '" + pId + "' at '" + path + "' to cache.");
                return path;
            }

            Log.Error("DesignID '" + pId + "' not found.");
            return "";
        }

        private static string getIdPathFromInstallation(string pId) {
            string path = Config.ProgramPath + "\\Resources\\ls-R";

            // check if path is valid
            if (!System.IO.File.Exists(path)) {
                Log.Error("File at '" + path + "' does not exist.");
                return "";
            }

            // Read the file and display it line by line.
            System.IO.StreamReader file;
            try {
                file = new System.IO.StreamReader(path);
            } catch (Exception e) {
                Log.Error("Loading Design IDs failed with Error: '" + e.Message + "'");
                return "";
            }

            string line;

            // this file contains all design id paths, store relevant ones for easy use in mcfParser
            while ((line = file.ReadLine()) != null) {
                // TODO: for now only looks for backgrounds.
                if (line.StartsWith("photofun/backgrounds")) {
                    string id = line.Split("/").Last().Split(".").First();
                    //Log.Info("Register ID: " + id + " at: " + line);
                    id = id.Split("-").Last(); // some ids have names... keep only the id number...
                    if (id == pId)
                        return Config.ProgramPath + "//Resources//" + line;
                }
            }

            return "";
        }

        private static string getIdPathFromProgramData(string pId) {
            // scan whole folder for image files
            const string dlpath = "C:\\ProgramData\\hps";
            if (System.IO.Directory.Exists(dlpath)) {
                string[] filenames = System.IO.Directory.GetFiles(dlpath, "*", System.IO.SearchOption.AllDirectories);
                Log.Info("Loading DesignIDs from '" + dlpath + "'.");
                foreach (string addfile in filenames) {
                    if (addfile.EndsWith(".jpg") || addfile.EndsWith(".bmp") || addfile.EndsWith(".webp")) {
                        string id = addfile.Split("\\").Last().Split(".").First();
                        //Log.Info("Register ID: " + id + " at: " + line);
                        id = id.Split("-").Last(); // some ids have names... keep only the id number...
                        //Log.Info("\t found id: '" + id + "' at: '" + addfile + "'");
                        return addfile;
                    }
                }
            } else {
                Log.Warning("Directory at: '" + dlpath + "' does not exist.");
            }

            return "";
        }

        public static Bitmap getBitmapFromID(string pId) {
            string path = getPath(pId);

            if (String.IsNullOrWhiteSpace(path)) {
                Log.Error("Design ID '" + pId + "' not found.");
                return null;
            }

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
