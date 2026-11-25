using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Cewe2pdf {
    class DesignIdConverter {

        private static Dictionary<string, Image> _imageCache = new Dictionary<string, Image>();

        private static string getPath(string pId) {
            string path;
            path = DesignResources.Find(pId,"backgrounds");
            if (!String.IsNullOrWhiteSpace(path)) return path;

            // from installation
            path = getIdPathFromInstallationDirectly(pId);
            if (!String.IsNullOrWhiteSpace(path)) {
                return path;
            }

            // Existing installations can have different bundled design collections.
            string dm = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dm", "dm-Fotowelt", "Resources", "photofun", "backgrounds");
            path = getIdPathFromDirectory(pId, dm);
            if (!String.IsNullOrWhiteSpace(path)) return path;

            // in downloaded content
            path = getIdPathFromProgramData(pId);
            if (!String.IsNullOrWhiteSpace(path)) {
                return path;
            }

            Log.Error("DesignID '" + pId + "' not found.");
            return "";
        }

        private static string getIdPathFromDirectory(string pId, string pDirectory) {
            // scan whole folder for image files
            if (System.IO.Directory.Exists(pDirectory)) {
                string[] filenames = System.IO.Directory.GetFiles(pDirectory, "*", System.IO.SearchOption.AllDirectories);
                Log.Info("Loading DesignIDs from '" + pDirectory + "'.");
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
                Log.Warning("Directory at: '" + pDirectory + "' does not exist.");
            }
            return "";
        }

        private static string getIdPathFromProgramData(string pId) {
            // scan dynamic download folder for matching design ids
            const string dlpath = "C:\\ProgramData\\hps";
            string path = getIdPathFromDirectory(pId, dlpath);
            if (!String.IsNullOrWhiteSpace(path)) return path;
            return getIdPathFromDirectory(pId, System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CEWE", "hps"));
        }

        public static string getIdPathFromInstallationDirectly(string pId) {
            // directly scans the installtion folder for matching design ids
            string path = Config.ProgramPath + "\\Resources\\photofun\\backgrounds";
            return getIdPathFromDirectory(pId, path);
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
