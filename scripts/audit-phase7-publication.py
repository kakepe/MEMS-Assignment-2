"""Dry-run source/package allowlist. Does not initialize Git, stage files or create an archive."""
import hashlib,json,os,re
from pathlib import Path
root=Path(__file__).resolve().parents[1]
skip={'.git','.local','.tools','artifacts','bin','obj','node_modules','.vs','.idea','.codex','.agents','.aws','.vscode','App_Data'}
suffixes={'.zip','.db','.sqlite','.sqlite3','.mdf','.ldf','.bak','.bacpac','.dacpac','.dump','.log','.trx','.dmp','.pfx','.p12','.key','.pem','.user'}
root_files={'MEMS.sln','global.json','NuGet.Config','README.md','AGENTS.md','.gitignore'}
allowed_roots={'src','scripts','tests','docs','.github'}
self_outputs={'docs/final/publication-manifest.json','docs/final/publication-audit.json'}
secrets=[]
for name,key in [('apphost-secrets.json','mems-sql-password'),('identity-seed.json','mems-identity-seed-password')]:
    secrets.append(json.loads((root/'.local'/name).read_text(encoding='utf-8-sig'))['Parameters'][key])
raw=(root/'.local/apphost-direct.log').read_bytes();log=raw.decode('utf-16' if raw[:2]==b'\xff\xfe' else 'utf-8',errors='replace')
match=re.search(r'login\?t=([^\s]+)',log)
if match:secrets.append(match[1])
files=[];excluded=[];bad=[]
for directory,dirs,names in os.walk(root):
    for name in list(dirs):
        if name in skip:
            excluded.append((Path(directory)/name).relative_to(root).as_posix()+'/');dirs.remove(name)
    for name in names:
        p=Path(directory)/name;rel=p.relative_to(root).as_posix();parts=Path(rel).parts;lower=name.lower()
        if rel in self_outputs:continue
        eligible=(len(parts)==1 and name in root_files) or (len(parts)>1 and parts[0] in allowed_roots)
        if not eligible or p.suffix.lower() in suffixes or lower.startswith('.env') or 'secrets' in lower or lower.startswith('appsettings.development.') or lower.startswith('appsettings.production.') or lower.startswith('appsettings.local.') or lower.endswith(('.db-shm','.db-wal')):
            excluded.append(rel);continue
        if not p.resolve().is_relative_to(root):raise RuntimeError('Publication candidate escapes workspace')
        data=p.read_bytes()
        if any(value.encode(enc) in data for value in secrets for enc in ['utf-8','utf-16-le']):bad.append(rel)
        if p.suffix.lower()=='.pdf':
            import sys;sys.path.insert(0,str(root/'.tools/pdf'))
            from pypdf import PdfReader
            text='\n'.join(page.extract_text() for page in PdfReader(p).pages)
            if any(value in text for value in secrets):bad.append(rel)
        files.append(dict(path=rel,bytes=len(data),sha256=hashlib.sha256(data).hexdigest()))
if bad:raise RuntimeError('Known secret found in candidate file(s): '+', '.join(sorted(set(bad))))
baseline=json.loads((root/'.local/phase7-source-baseline.json').read_text(encoding='utf-8-sig'))
for rel,digest in baseline['files'].items():
    if rel.startswith('src/') or re.match(r'\.github/prompts/phase-[1-6]-',rel):
        assert hashlib.sha256((root/rel).read_bytes()).hexdigest()==digest,rel
for name,digest in baseline['secrets'].items():assert hashlib.sha256((root/'.local'/name).read_bytes()).hexdigest()==digest
manifest=dict(purpose='Review-only allowlist for later explicitly approved source archive/Git staging; no ZIP or Git operation performed.',files=sorted(files,key=lambda x:x['path']))
(root/'docs/final/publication-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
result=dict(passed=True,gitRepositoryPresent=(root/'.git').exists(),trackedFiles='Not applicable: workspace is not a Git repository.',candidateFiles=len(files),candidateBytes=sum(x['bytes'] for x in files),knownSecretMatches=0,allApplicationSourceUnchanged=True,originalPhase1Through6PromptsUnchanged=True,existingSecretsUnchanged=True,excludedRootsOrFiles=sorted(excluded),archiveCreated=False,gitInitialized=False)
(root/'docs/final/publication-audit.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k!='excludedRootsOrFiles'},indent=2))
