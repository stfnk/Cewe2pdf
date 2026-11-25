"""Check the actual portable payload, matching LGPL source archive and SPDX inventory."""
import hashlib,json,sys,zipfile
from pathlib import Path
root=Path(sys.argv[1]).resolve()
windows=root/'windows-x64'
assert (windows/'iTextSharp.LGPLv2.Core.dll').is_file(), 'Replaceable LGPL DLL missing'
assert not any(p.name.lower()=='itextsharp.dll' for p in windows.rglob('*.dll')), 'Removed AGPL DLL present'
deps=json.loads((windows/'Cewe2pdf.deps.json').read_text())
assert not any(k.startswith('iTextSharp/') for k in deps['libraries']), 'AGPL package in executable graph'
assert 'iTextSharp.LGPLv2.Core/3.8.8' in deps['libraries']
sbom=json.loads((root/'SBOM.spdx.json').read_text())
assert not any(p['name']=='iTextSharp' for p in sbom['packages']), 'AGPL package in source/test graph'
assert any(p['name']=='iTextSharp.LGPLv2.Core' and p['licenseDeclared']=='LGPL-2.0-only' for p in sbom['packages'])
manifest=json.loads((root/'upstream-source-manifest.json').read_text())
source=root/'upstream'/manifest['archive']
assert hashlib.sha256(source.read_bytes()).hexdigest()==manifest['sha256'], 'Library source hash mismatch'
with zipfile.ZipFile(source) as z: assert z.testzip() is None
assert 'MIT License' in (root/'LICENSE').read_text(encoding='utf-8-sig')
assert 'GNU LIBRARY GENERAL PUBLIC LICENSE' in (root/'licenses/itextsharp-lgplv2-core-LICENSE.md').read_text(encoding='utf-8-sig')
print('PASS replaceable LGPL DLL, no AGPL runtime/test package, MIT application, complete matching LGPL source and license inventory')
