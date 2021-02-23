using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Cewe2pdf {
    class DesignIdConverter {

        private static Dictionary<string, Image> _imageCache = new Dictionary<string, Image>();
        private static Dictionary<string, string> _resourceList;

        public static void initResourceList() {
            if (_resourceList == null) {
                _resourceList = new Dictionary<string, string>();

                string path = Config.ProgramPath + "\\Resources\\ls-R";

                // check if path is valid
                if (!System.IO.File.Exists(path)) {
                    Log.Error("File at '" + path + "' does not exist.");
                }

                // Read the file and display it line by line.
                System.IO.StreamReader file;
                try {
                    file = new System.IO.StreamReader(path);
                    string line;

                    // this file contains all design id paths, store relevant ones for easy use in mcfParser
                    while ((line = file.ReadLine()) != null) {
                        // TODO: for now only looks for backgrounds.
                        if (line.StartsWith("photofun/backgrounds")) {
                            string id = line.Split("/").Last().Split(".").First();
                            //Log.Info("Register ID: " + id + " at: " + line);
                            id = id.Split("-").Last(); // some ids have names... keep only the id number...
                            _resourceList.TryAdd(id, line);
                        }
                    }
                } catch (Exception e) {
                    Log.Error("Loading Design IDs failed with Error: '" + e.Message + "'");
                }

                Log.Info("Added " + _resourceList.Count + " Design IDs to resource cache.");
            }
        }

        private static string getPath(string pId) {
            string path;

            // in installation
            path = getIdPathFromInstallation(pId);
            if (!String.IsNullOrWhiteSpace(path)) {
                return path;
            }

            // in downloaded content
            path = getIdPathFromProgramData(pId);
            if (!String.IsNullOrWhiteSpace(path)) {
                return path;
            }

            Log.Error("DesignID '" + pId + "' not found.");
            return "";
        }

        private static string getIdPathFromInstallation(string pId) {
            if (_resourceList == null) initResourceList();

            // look up path
            string ret = "";
            _resourceList.TryGetValue(pId, out ret);

            if (String.IsNullOrWhiteSpace(ret))
                return "";
            else
                return Config.ProgramPath + "//Resources//" + ret;
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
                        if (id == pId)
                            return addfile;
                    }
                }
            } else {
                Log.Warning("Directory at: '" + dlpath + "' does not exist.");
            }

            return "";
        }

        public static Image getImageFromID(string pId) {
            if (_imageCache.ContainsKey(pId)) {
                Log.Info("Using cached image for Design ID '" + pId + "'");
                return _imageCache[pId];
            }

            string path = getPath(pId);

            if (String.IsNullOrWhiteSpace(path)) {
                Log.Error("Design ID '" + pId + "' not found.");
                return null;
            }

            if (!System.IO.File.Exists(path)) {
                Log.Error("DesignID file at: '" + path + "' does not exist.");
                return null;
            } else {
                Log.Info("Loading DesignID: " + path.Split("/").Last());
            }

            if (path.EndsWith(".webp")) {
                // load webp
                try {
                    WebPWrapper.WebP webP = new WebPWrapper.WebP();
                    Image bm = webP.Load(path);
                    addToImageCache(pId, bm);
                    return bm;
                } catch (Exception e) {
                    Log.Error("Loading '" + path + "' failed with error: '" + e.Message + "'.");
                    return null;
                }
            } else {
                // load image
                try {
                    Image bm = Image.FromFile(path);
                    addToImageCache(pId, bm);
                    return bm;
                } catch (Exception e) {
                    Log.Error("Loading '" + path + "' failed with error: '" + e.Message + "'.");
                    return null;
                }
            }
        }

        private static void addToImageCache(string pId, Image pImg) {
            // TODO: add a (memory) limit to cache size?
            _imageCache.Add(pId, pImg);
        }
    }
}
