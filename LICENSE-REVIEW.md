# License review — v0.4.4-book1, 2026-11-18

## Resolved software dependency issue

The new PDF application no longer references iTextSharp 5.5.13.4 or any AGPL library. It uses **iTextSharp.LGPLv2.Core 3.8.8**, NuGet-declared license **LGPL-2.0-only**, upstream commit `fd11e71cce122b92b976115b49284f143b934af8`. This is a maintained port of the older iTextSharp 4.1.6 LGPL line, not a relabelled copy of iTextSharp 5. No iTextSharp-5 library implementation was copied into the application.

Application code originated under Stefan Kreller's MIT license. Local modifications commissioned by Robert Brendel are distributed under MIT in this new release. Both copyrights remain. Historical releases that used iTextSharp 5 retain their original AGPL terms; this change does not retroactively relicense those binaries or change third-party copyrights.

The library and its use remain covered by LGPL-2.0-only. The release implements the source/relinking route in LGPL section 6(a): matching library source archive, complete application source/build instructions, license and copyright notices, and a separate replaceable `iTextSharp.LGPLv2.Core.dll`. SingleFile and trimming are disabled for the PDF application. The distribution permits modification for private use and reverse engineering to debug those modifications. The MIT license grants application-source modification rights. See SOURCE-CODE.md. The package is not described as license-free.

The independent SvgToPng process remains MS-PL, with SVG.NET 3.4.7 and ExCSS 4.2.3. It is not linked into the PDF process; no change to that boundary or license is needed. Test-only PdfPig is Apache-2.0 and is not included in the application payload. Resolved runtime/build/test dependencies and supplied notices are recorded separately in the SPDX inventory.

## Rights outside the software license

CEWE assets and fonts are not bundled with the software. The software dependency change does not grant redistribution rights to CEWE graphics, logos or font programs. Local resource use and font embedding remain subject to the applicable asset/font licenses. These rights cannot be granted by changing source code or marking a Git issue closed. The optional spine logo remains off by default. No private photobook or personal photo is in the software release.

## Evidence and scope

- Exact package metadata and source commit: `licenses/nuget/iTextSharp.LGPLv2.Core-3.8.8/` and SBOM.spdx.json.
- Matching source archive and SHA-256: upstream-source-manifest.json; supplied under upstream/ in the release.
- Full GNU Library GPL version 2 text: licenses/itextsharp-lgplv2-core-LICENSE.md.
- Upstream project: https://github.com/VahidN/iTextSharp.LGPLv2.Core
- Package declaration: https://www.nuget.org/packages/iTextSharp.LGPLv2.Core/3.8.8
- LGPL text: https://licenses.nuget.org/LGPL-2.0-only

This records a concrete engineering and distribution remedy, not a universal legal clearance for assets or future releases. The private issue tracker returned HTTP 403 during this work; no remote issue was edited or marked closed. The resolved code/dependency change is documented locally.
