using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml;

namespace Cewe2pdf {
    // CEWE original assets stay in a separate user cache, never in the source repository.
    static class DesignResources {
        public static string CachePath => string.IsNullOrWhiteSpace(Config.ResourcePath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Cewe2pdf","resources")
            : Path.GetFullPath(Config.ResourcePath);
        static readonly HashSet<string> attempted = new(StringComparer.Ordinal);
        public static IEnumerable<string> Roots(string kind) {
            string pf=Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            foreach(string program in new[]{Config.ProgramPath,Path.Combine(pf,"CEWE","CEWE Fotowelt"),Path.Combine(pf,"dm","dm-Fotowelt")}.Distinct())
                if(!string.IsNullOrWhiteSpace(program)) yield return Path.Combine(program,"Resources","photofun",kind);
            yield return CachePath;
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CEWE","hps");
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"hps");
        }
        public static string Find(string id,string kind,string uniqueName="") {
            foreach(string root in Roots(kind)) {
                if(!Directory.Exists(root))continue;
                // Current layouts store metadata and payload in the element-ID directory.
                var dirs=new List<string>();
                string direct=Path.Combine(root,id);
                if(Directory.Exists(direct))dirs.Add(direct);
                if(Path.GetFileName(root)==id)dirs.Add(root);
                if(dirs.Count==0 && (root.EndsWith("hps",StringComparison.OrdinalIgnoreCase)))
                    dirs.AddRange(Directory.EnumerateDirectories(root,id,SearchOption.AllDirectories));
                foreach(string dir in dirs) {
                    var files=Directory.GetFiles(dir,"*",SearchOption.AllDirectories);
                    string found=Choose(files,kind,uniqueName);
                    if(found!=null)return found;
                }
                if(!string.IsNullOrEmpty(uniqueName)) {
                    string stem=Path.GetFileNameWithoutExtension(uniqueName);
                    foreach(string ext in new[]{".svg",".clp"}) {
                        string found=Directory.EnumerateFiles(root,stem+ext,SearchOption.AllDirectories).FirstOrDefault();
                        if(found!=null)return found;
                    }
                }
            }
            if(Config.DownloadResources && attempted.Add(id) && Download(id))return Find(id,kind,uniqueName);
            return null;
        }
        static string Choose(IEnumerable<string> paths,string kind,string name) {
            string[] ext=kind=="backgrounds"?new[]{".jpg",".jpeg",".png",".webp",".bmp"}:new[]{".svg",".clp",".png"};
            return paths.Where(p=>ext.Contains(Path.GetExtension(p).ToLowerInvariant()))
                .Where(p=>!p.Replace('\\','/').Contains("/refinement/",StringComparison.OrdinalIgnoreCase))
                .Where(p=>!Path.GetFileName(p).Contains("preview",StringComparison.OrdinalIgnoreCase) && !Path.GetFileName(p).Contains("mask",StringComparison.OrdinalIgnoreCase))
                .OrderBy(p=>string.IsNullOrEmpty(name)||Path.GetFileNameWithoutExtension(p)!=Path.GetFileNameWithoutExtension(name)?1:0)
                .ThenBy(p=>Array.IndexOf(ext,Path.GetExtension(p).ToLowerInvariant())).FirstOrDefault();
        }
        public static bool Download(string id) {
            if(!long.TryParse(id,out long number)||number<=0)return false;
            try {
                string account="6822";
                string config=Path.Combine(Config.ProgramPath,"Resources","config","keyaccount.xml");
                if(File.Exists(config)){var doc=new XmlDocument{XmlResolver=null};doc.Load(config);string s=doc.SelectSingleNode("/config/keyAccount")?.InnerText;if(int.TryParse(s,out _))account=s;}
                string uri=$"https://delapi.photoprintit.com/api/designElements/{number}.zip?clientid=38&version=8002001&keyaccount={account}&language=de";
                using var client=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(60)};
                client.MaxResponseContentBufferSize=50*1024*1024;
                byte[] data=client.GetByteArrayAsync(uri).GetAwaiter().GetResult();
                if(data.Length>50*1024*1024)throw new InvalidDataException("Resource package too large");
                Directory.CreateDirectory(CachePath);
                using var zip=new ZipArchive(new MemoryStream(data),ZipArchiveMode.Read);
                long expanded=0;string boundary=Path.GetFullPath(CachePath)+Path.DirectorySeparatorChar;
                foreach(var entry in zip.Entries) {
                    expanded+=entry.Length;if(expanded>100*1024*1024)throw new InvalidDataException("Expanded resource package too large");
                    string target=Path.GetFullPath(Path.Combine(CachePath,entry.FullName));
                    if(!target.StartsWith(boundary,StringComparison.OrdinalIgnoreCase) || !entry.FullName.Replace('\\','/').StartsWith(id+"/",StringComparison.Ordinal))throw new InvalidDataException("Unsafe resource archive path");
                    if(string.IsNullOrEmpty(entry.Name)){Directory.CreateDirectory(target);continue;}
                    Directory.CreateDirectory(Path.GetDirectoryName(target));entry.ExtractToFile(target,true);
                }
                string manifest=JsonSerializer.Serialize(new{designElementId=number,url=uri,sha256=Convert.ToHexString(SHA256.HashData(data)),downloadedUtc=DateTime.UtcNow});
                File.WriteAllText(Path.Combine(CachePath,id,"download-source.json"),manifest);
                Log.Info("Downloaded original CEWE resource "+id);return true;
            } catch(Exception e){Log.Warning("Original CEWE resource "+id+" could not be downloaded: "+e.Message);return false;}
        }
    }
}
