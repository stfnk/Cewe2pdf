# Corresponding source, rebuilding and replacing the PDF library

Application v0.4.4-book1: MIT. Dynamically linked iTextSharp.LGPLv2.Core 3.8.8: LGPL-2.0-only. Independent SvgToPng utility: MS-PL. Preserve all respective copyright and license notices.

The complete release includes:

- windows-x64/ — executable, application assembly, dependencies and their notices;
- cewe2pdf-source.zip — complete matching application, utility and test sources, project/build files and license inventory;
- upstream/itextsharp-lgplv2-core-3.8.8-source.zip — unmodified library source at commit fd11e71cce122b92b976115b49284f143b934af8;
- upstream-source-manifest.json — exact download URL and SHA-256.

Supply the complete set together when redistributing. A private Git link or an inaccessible source URL is not a substitute for this source copy. Application source and object code are provided so users can modify/relink against the library. No three-year written offer is substituted for the accompanying sources.

## Build the application

On Windows with .NET SDK 10 and NuGet access, extract cewe2pdf-source.zip and run:

```powershell
.\build.ps1
# or
.\build.ps1 -Dotnet 'C:\SDK\dotnet.exe'
```

Versions are pinned in the project files; resolved package versions, hashes, repositories and license metadata are in SBOM.spdx.json. Test extraction uses the independent PdfPig reader, not the removed AGPL library. System SDK/compiler components are prerequisites.

## Replace/rebuild the LGPL library

The PDF application is published with PublishSingleFile=false and PublishTrimmed=false. The library remains the separate file `windows-x64/iTextSharp.LGPLv2.Core.dll`; it is not embedded in Cewe2pdf.exe. Close running converters before replacing it. Modification for your own use and reverse engineering to debug those modifications are allowed.

Extract the supplied upstream archive. Its `src/iTextSharp.LGPLv2.Core/iTextSharp.LGPLv2.Core.csproj` contains the library build. Build its net10.0 target, retain a compatible public API/assembly identity, and replace the DLL in the application folder. Keep any changed dependencies compatible as well. Alternatively, change the application's PackageReference to a ProjectReference to your modified library project and rebuild with .NET SDK 10. Both application and library source are supplied for this purpose. See the upstream README for its own prerequisites.

Unmodified dependency sources are identified by source repository/commit and package hashes in SBOM.spdx.json and THIRD-PARTY-NOTICES.md. Microsoft .NET sources: https://github.com/dotnet/dotnet; SVG.NET: https://github.com/svg-net/SVG; SQLite: https://system.data.sqlite.org/ and https://www.sqlite.org/src. Their original terms remain in licenses/. No CEWE graphics, fonts or personal books form part of the source package.

Historical v0.4.1 distributions used AGPL iTextSharp 5; use their matching sources/notices when redistributing them. This new release does not change their terms.
