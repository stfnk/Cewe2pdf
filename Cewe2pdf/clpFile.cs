using Svg; // https://github.com/svg-net/SVG
using System;
using System.Drawing;
using System.IO;
using System.Text;

namespace Cewe2pdf {
    class clpFile {

        private string _svg;

        public Image getImage() {
            if (_svg == null) return null;
            SvgDocument doc = SvgDocument.FromSvg<SvgDocument>(_svg);
            Image img = doc.Draw();
            return img;
        }

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
                // https://github.com/bash0/cewe2pdf/blob/master/CLP%20file%20format.txt

                // string contains hex data
                string data = File.ReadAllText(path, Encoding.UTF8);
                data = data.Remove(0, 1); // pop leading 'a'

                // invalid characters in hex string
                string[] invalid = new string[] { "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };

                // remove invalid characters from hex string
                foreach (string c in invalid)
                    data = data.Replace(c, String.Empty);

                // convert hex string to bytes and back to utf8 -> final .svg plain text
                byte[] svgData = hexStringToByteArray(data);
                _svg = Encoding.UTF8.GetString(svgData);

            } catch (Exception e) {
                Log.Error("Loading clipart from '" + path + "' failed with error: '" + e.Message + "'");
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
