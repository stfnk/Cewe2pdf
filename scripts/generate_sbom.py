"""Generate SPDX 2.3 JSON from restored project graphs and actual NuGet archives."""
import datetime,hashlib,json,re,uuid,subprocess,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def sid(value):return 'SPDXRef-'+re.sub('[^A-Za-z0-9.-]','-',value)
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def source_digest(p):return hashlib.sha256(p.read_bytes().replace(b'\r\n',b'\n')).hexdigest()
packages=[];relationships=[];notices=[];seen={}
for project,ownid,license in [('Cewe2pdf','SPDXRef-Cewe2pdf','MIT'),('SvgToPng','SPDXRef-SvgToPng','MS-PL'),('tests','SPDXRef-Tests','MIT')]:
 asset_path=ROOT/project/('obj/IntegrationTests/project.assets.json' if project=='tests' else 'obj/project.assets.json')
 asset=json.loads(asset_path.read_text());cache=Path(next(iter(asset['packageFolders'])))
 packages.append({'SPDXID':ownid,'name':project,'versionInfo':'v0.4.4-book1','downloadLocation':'NOASSERTION','filesAnalyzed':False,'licenseConcluded':license,'licenseDeclared':license,'copyrightText':'Copyright 2026 Robert Brendel; original PDF application copyright 2020 Stefan Kreller'})
 libs=dict(asset['libraries'])
 # Self-contained runtime is not listed as an ordinary library in project.assets.json.
 runtime=cache/'microsoft.netcore.app.runtime.win-x64'
 download=next(d for d in asset['project']['frameworks']['net10.0']['downloadDependencies'] if d['name']=='Microsoft.NETCore.App.Runtime.win-x64') if project!='tests' else None
 version=download['version'].strip('[]').split(',')[0].strip() if download else json.loads((ROOT/'windows-x64/Cewe2pdf.runtimeconfig.json').read_text())['runtimeOptions']['includedFrameworks'][0]['version']
 libs['Microsoft.NETCore.App.Runtime.win-x64/'+version]={'type':'package','path':'microsoft.netcore.app.runtime.win-x64/'+version}
 for key,lib in libs.items():
  if lib['type']!='package':continue
  name,version=key.split('/',1);pid=sid('NuGet-'+key);folder=cache/lib['path'];nuspec=next(folder.glob('*.nuspec'));tree=ET.parse(nuspec)
  find=lambda n:next((e for e in tree.iter() if e.tag.split('}')[-1]==n),None)
  text=lambda n:find(n).text if find(n) is not None else None
  import shutil
  notice_dir=ROOT/'licenses'/'nuget'/(name+'-'+version)
  notice_dir.mkdir(parents=True,exist_ok=True)
  shutil.copy2(nuspec,notice_dir/nuspec.name)
  for supplied in folder.rglob('*'):
   if supplied.is_file() and any(supplied.name.lower().startswith(prefix) for prefix in ('license','copying','notice','third-party-notices')):
    target=notice_dir/supplied.relative_to(folder);target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(supplied,target)
  nupkg=next(folder.glob('*.nupkg'));lic=text('license') or ('AGPL-3.0-only' if name=='iTextSharp' else 'LicenseRef-SQLite-Public-Domain' if 'SQLite' in name else 'NOASSERTION')
  if find('license') is not None and find('license').get('type')=='file':lic='NOASSERTION'
  repo=find('repository');repo=repo.attrib if repo is not None else {}
  buildonly=project=='tests' or name=='Microsoft.NET.ILLink.Tasks'
  relationships.append({'spdxElementId':ownid,'relationshipType':'BUILD_DEPENDENCY_OF' if buildonly else 'DEPENDS_ON','relatedSpdxElement':pid} if not buildonly else {'spdxElementId':pid,'relationshipType':'BUILD_DEPENDENCY_OF','relatedSpdxElement':ownid})
  if pid in seen:continue
  p={'SPDXID':pid,'name':name,'versionInfo':version,'downloadLocation':f'https://api.nuget.org/v3-flatcontainer/{name.lower()}/{version.lower()}/{name.lower()}.{version.lower()}.nupkg','filesAnalyzed':False,'checksums':[{'algorithm':'SHA256','checksumValue':digest(nupkg)}],'licenseDeclared':lic,'licenseConcluded':lic,'copyrightText':text('copyright') or 'NOASSERTION','externalRefs':[{'referenceCategory':'PACKAGE-MANAGER','referenceType':'purl','referenceLocator':f'pkg:nuget/{name}@{version}'}]}
  if repo.get('url'):p['sourceInfo']='Upstream: '+repo['url']+'; commit: '+repo.get('commit','not declared')
  if buildonly:p['comment']='Build-only tooling; not an application runtime dependency.'
  if name=='iTextSharp':p['comment']='Retain upstream additional disclaimer and PDF producer metadata; see licenses/itextsharp-LICENSE.md.'
  p['supplier']='Organization: '+(text('authors') or 'NOASSERTION')
  packages.append(p);seen[pid]=p
  notices.append(f"## {name} {version}\n\nDeclared license: {lic}. Copyright: {p['copyrightText']}.\n\nUpstream: {repo.get('url') or text('projectUrl') or text('licenseUrl') or 'See package metadata'}. Commit: {repo.get('commit','not declared')}.\n\nPackage SHA-256: `{digest(nupkg)}`. Exact package: {p['downloadLocation']}. Supplied license/notice files are preserved under licenses/.\n")
# Explicit source/dependency licensing boundaries.
relationships.append({'spdxElementId':'SPDXRef-Tests','relationshipType':'BUILD_DEPENDENCY_OF','relatedSpdxElement':'SPDXRef-Cewe2pdf'})
relationships.extend([{'spdxElementId':'SPDXRef-DOCUMENT','relationshipType':'DESCRIBES','relatedSpdxElement':'SPDXRef-Cewe2pdf'},{'spdxElementId':'SPDXRef-DOCUMENT','relationshipType':'DESCRIBES','relatedSpdxElement':'SPDXRef-SvgToPng'}])
files=[]
tracked=subprocess.check_output(['git','-C',str(ROOT),'ls-files','--cached','--others','--exclude-standard','-z']).decode().split('\0')
for p in sorted(ROOT/name for name in tracked if name):
 if not p.is_file():continue
 rel=p.relative_to(ROOT)
 if any(x in {'.git','bin','obj','test-artifacts'} for x in rel.parts) or p.name=='SBOM.spdx.json' or p.suffix in {'.pdf','.jpg','.png'}:continue
 if rel.parts[0]=='windows-x64':continue  # Release payload inventory is added after packaging below.
 fid=sid('File-'+rel.as_posix());files.append({'SPDXID':fid,'fileName':'./'+rel.as_posix(),'checksums':[{'algorithm':'SHA256','checksumValue':source_digest(p)}],'licenseConcluded':'NOASSERTION','copyrightText':'NOASSERTION'})
 relationships.append({'spdxElementId':'SPDXRef-SvgToPng' if rel.parts[0]=='SvgToPng' else 'SPDXRef-Tests' if rel.parts[0]=='tests' else 'SPDXRef-Cewe2pdf','relationshipType':'CONTAINS','relatedSpdxElement':fid})
now=datetime.datetime.now(datetime.timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ')
doc={'spdxVersion':'SPDX-2.3','dataLicense':'CC0-1.0','SPDXID':'SPDXRef-DOCUMENT','name':'cewe2pdf-v0.4.4-book1-source-and-resolved-dependencies','documentNamespace':'https://spdx.org/spdxdocs/cewe2pdf-'+str(uuid.uuid4()),'creationInfo':{'created':now,'creators':['Person: Robert Brendel','Tool: cewe2pdf-generate-sbom.py'],'comment':'AI-assisted technical inventory. Main PDF and independent SVG utility have separate dependency graphs; no assertion of CEWE asset/font rights.'},'packages':packages,'files':files,'relationships':relationships,'hasExtractedLicensingInfos':[{'licenseId':'LicenseRef-SQLite-Public-Domain','name':'SQLite public domain dedication','extractedText':'All deliverable SQLite code has been dedicated to the public domain. Anyone is free to copy, modify, publish, use, compile, sell, or distribute the original SQLite code for any purpose. See the upstream copyright statement and package licenseUrl; this is a public-domain dedication, not an SPDX-listed license.','seeAlsos':['https://www.sqlite.org/copyright.html']}]}
# Verify the selected PDF implementation and retain the independent SVG utility boundary.
main=json.loads((ROOT/'Cewe2pdf/obj/project.assets.json').read_text())
assert not any(k.startswith(('Svg/','ExCSS/','iTextSharp/')) for k in main['libraries']), 'Unexpected SVG or AGPL dependency in PDF process'
assert any(k.startswith('iTextSharp.LGPLv2.Core/') for k in main['libraries']), 'LGPL PDF library missing'
(ROOT/'THIRD-PARTY-NOTICES.md').write_text('# Third-party software notices\n\nCopyrights and licenses apply separately. Main application: MIT; dynamically linked PDF library: LGPL-2.0-only; independent SvgToPng: MS-PL. License texts are in licenses/. See LICENSE-REVIEW.md and SOURCE-CODE.md. No CEWE asset or font is distributed.\n\n'+'\n'.join(notices),encoding='utf8')
# Refresh notice hash after generation.
for f in files:
 if f['fileName']=='./THIRD-PARTY-NOTICES.md':f['checksums'][0]['checksumValue']=source_digest(ROOT/'THIRD-PARTY-NOTICES.md')
(ROOT/'SBOM.spdx.json').write_text(json.dumps(doc,indent=2,ensure_ascii=False)+'\n',encoding='utf8')
print('SPDX 2.3:',len(packages),'packages,',len(files),'source files; separated SVG dependency graph verified')

