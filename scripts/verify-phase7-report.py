"""Validate the actual generated PDF, required content, diagrams, paper orientation and secret boundary."""
import json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];sys.path.insert(0,str(root/'.tools/pdf'))
from pypdf import PdfReader
reader=PdfReader(root/'docs/final/MEMS-Assignment-2-Final-Report.pdf')
texts=[page.extract_text() for page in reader.pages];text='\n'.join(texts)
required=['Abdullah Rehan','Alex Fisher','MEMS.AppHost','MEMS.ServiceDefaults','MEMS.Admin','MEMS.Public','MEMS.Api','MEMS.Data','MEMS.Shared','Burnaby','Surrey','Richmond','Domain ERD','Identity and history ERD','AspNetUserRoles','SeedHistory','__EFMigrationsHistory','Assignment 1 deficiency','97/100','sessionStorage','aspire run','Copilot','Codex','unverified','publishing','No Azure']
for value in required:assert value.lower() in text.lower(),value
assert len(reader.pages)>=15
landscape=[i+1 for i,p in enumerate(reader.pages) if float(p.mediabox.width)>float(p.mediabox.height)]
assert len(landscape)==3
image_occurrences=0
for p in reader.pages:
    resources=p.get('/Resources')
    if resources and resources.get_object().get('/XObject'):
        for value in resources.get_object()['/XObject'].get_object().values():
            if value.get_object().get('/Subtype')=='/Image':image_occurrences+=1
assert image_occurrences>=10
assert 'â€' not in text and 'Â·' not in text
for filename,key in [('apphost-secrets.json','mems-sql-password'),('identity-seed.json','mems-identity-seed-password')]:
    private=json.loads((root/'.local'/filename).read_text(encoding='utf-8-sig'))['Parameters'][key]
    assert private not in text
result=dict(passed=True,pages=len(reader.pages),landscapeDiagramPages=landscape,embeddedScreenshotImages=image_occurrences,requiredSectionsPresent=True,teamNamesCorrect=True,knownCredentialsAbsent=True,mojibakeAbsent=True)
(root/'artifacts/phase7/pdf-verification.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result,indent=2))
