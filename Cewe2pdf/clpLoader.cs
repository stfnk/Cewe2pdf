using System;
using System.IO;
using System.Text;

namespace Cewe2pdf {
    class clpLoader {

        private string _svg;

        public void fromFile(string path) {
            if (!File.Exists(path)) {
                Log.Error(".clp file at '" + path + "' does not exist.");
                return;
            }
            if (!path.EndsWith(".clp")) {
                Log.Error("'" + path + "' is no .clp file.");
                return;
            }
            try {
                byte[] data = File.ReadAllBytes(path);

                string str = Encoding.UTF8.GetString(data);
                str = str.Remove(0, 1); // pop leading 'a'

                string[] invalid = new string[] { "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };
                foreach (var c in invalid) {
                    str = str.Replace(c, String.Empty);
                }

                byte[] svgData = hexStringToByteArray(str);

                string svg = Encoding.UTF8.GetString(svgData);

                Log.Info("Writing data.");
                Console.WriteLine(svg);
                //File.WriteAllText("data.txt", rm);


            } catch (Exception e) {
                Log.Error("Loading '" + path + "' failed with error: '" + e.Message + "'");
            }
        }

        private static byte[] hexStringToByteArray(String hex) {
            // https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/311179#311179
            int NumberChars = hex.Length / 2;
            byte[] bytes = new byte[NumberChars];
            using (var sr = new StringReader(hex)) {
                for (int i = 0; i < NumberChars; i++)
                    bytes[i] =
                      Convert.ToByte(new string(new char[2] { (char)sr.Read(), (char)sr.Read() }), 16);
            }
            return bytes;
        }
    }
}
