using System.Data.SQLite;
using System.Text;
using System;
using System.IO;
using System.Xml;

namespace Cewe2pdf {

    class Mcfx : IDisposable {
        /* Quick Note:
         * .mcfx is 'just' a sqlite3 database file, containing 3 columns:
         *  | Data | Filename | LastModified |
         *  this class wraps SQL commands to access specific data blobs from the database
        */

        public Mcfx(string filePath) {
            _filePath = Path.GetFullPath(filePath);
            _legacy = Path.GetExtension(filePath).Equals(".mcf", StringComparison.OrdinalIgnoreCase);
            // System.Data.SQLite cannot reliably open UNC shares with readonly flags.
            // Read a private snapshot; never create journals or lock the NAS original.
            if (!_legacy && _filePath.StartsWith(@"\\")) {
                _temporaryDatabase = Path.Combine(Path.GetTempPath(), "cewe2pdf-" + Guid.NewGuid().ToString("N") + ".mcfx");
                File.Copy(_filePath, _temporaryDatabase, false);
                _filePath = _temporaryDatabase;
            }
            if (_legacy) {
                var xml = new XmlDocument { XmlResolver = null };
                xml.Load(new MemoryStream(TrimMcf(File.ReadAllBytes(_filePath))));
                string dir = xml.DocumentElement.GetAttribute("imagedir");
                _imageDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_filePath),
                    string.IsNullOrWhiteSpace(dir) ? Path.GetFileNameWithoutExtension(_filePath) + "_mcf-Dateien" : dir));
            }
        }

        public byte[] getDataForFilename(string filename) {
            if (_legacy) {
                if (filename == "data.mcf") return File.ReadAllBytes(_filePath);
                string name = filename.Replace("safecontainer:/", "");
                foreach (string path in new[] { Path.Combine(_imageDirectory, name), Path.Combine(Path.GetDirectoryName(_filePath), name) })
                    if (File.Exists(path)) return File.ReadAllBytes(path);
                throw new FileNotFoundException("Legacy project image not found in '" + _imageDirectory + "': " + name);
            }
            // Provide the path to your existing database
            string connectionString = new SQLiteConnectionStringBuilder { DataSource = _filePath, Version = 3, ReadOnly = true, FailIfMissing = true }.ConnectionString;

            // Open the connection
            using (SQLiteConnection conn = new SQLiteConnection(connectionString)) {
                conn.Open();

                // Query the database (example: SELECT all rows from the Users table)
                string selectQuery = "SELECT Data FROM Files WHERE Filename = @filename;";
                using (SQLiteCommand cmd = new SQLiteCommand(selectQuery, conn)) {
                    cmd.Parameters.AddWithValue("@filename", filename);
                    using (SQLiteDataReader reader = cmd.ExecuteReader()) {

                        if (reader.Read()) {
                            // Read the BLOB data from the database
                            byte[] blobData = reader["Data"] as byte[];

                            if (blobData != null) {
                                return blobData;
                            }
                        }
                    }
                }
            }

            Log.Error("No BLOB data found for filename " + filename);
            return null;
        }

        public System.Drawing.Image getSystemImageForFilename(string filename) {
            byte[] blobData = getDataForFilename(filename);
            if (blobData == null) return null;

            // for debugging, write extracted binary data to jpg file - should be valid to open in any image viewer
            //using (var fileStream = new System.IO.FileStream("test_img.jpg", System.IO.FileMode.Create)) {
            //    fileStream.Write(blobData, 0, blobData.Length);
            //}

            // construct an in-memory system image from binary data
            using (var ms = new System.IO.MemoryStream(blobData)) {
                using (var image = System.Drawing.Image.FromStream(ms)) {
                    // GDI+ loses lazy EXIF properties when the backing stream is closed,
                    // even on Image.Clone(). Orient while the stream is alive, then copy pixels.
                    PdfWriter.ExifRotate(image);
                    return new System.Drawing.Bitmap(image);
                }
            }
        }

        public System.IO.MemoryStream getMcfFile() {
            byte[] blobData = getDataForFilename("data.mcf");
            if (blobData == null) return null;

            // TODO: first convert to string and cut off anything after </fotobook>
            // then convert to memory stream for returning
            // mcf has some binary data after </fotobook>, so we need to trim that off - not really sure what the data is for now
            return new System.IO.MemoryStream(TrimMcf(blobData));
        }

        private static byte[] TrimMcf(byte[] data) {
            // Keep original XML bytes/encoding (including legacy declarations) and remove binary trailer only.
            byte[] end = Encoding.ASCII.GetBytes("</fotobook>");
            for (int i = 0; i <= data.Length - end.Length; i++) {
                bool match = true;
                for (int j = 0; j < end.Length; j++) if (data[i+j] != end[j]) { match = false; break; }
                if (match) return data[..(i + end.Length)];
            }
            throw new InvalidDataException("CEWE project has no closing fotobook element.");
        }

        private string _filePath; // the database file path
        private bool _legacy;
        private string _imageDirectory;
        private string _temporaryDatabase;
        public void Dispose() {
            if (_temporaryDatabase != null) { File.Delete(_temporaryDatabase); _temporaryDatabase = null; }
        }
    }
}
