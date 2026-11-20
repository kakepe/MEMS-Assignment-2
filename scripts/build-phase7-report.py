"""Build editable offline HTML/Markdown documentation from actual evidence; PDF uses Chrome separately."""
import html,json,re
from pathlib import Path
root=Path(__file__).resolve().parents[1];final=root/'docs/final'
def readjson(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(name,text):(final/name).write_text(text,encoding='utf-8')
sql=readjson(final/'evidence/sql-snapshot.json')
dictionary=['# SQL Server data dictionary','',f"Live read-only metadata: {sql['database']}; captured {sql['capturedAt']}. No Identity row values are exported.",'','The domain and Identity/history ERDs cover all twelve tables; the following dictionary lists actual columns and keys. `timestamp` is SQL Server row-version semantics, not a date/time.','']
for table in sql['tables']:
    dictionary+=['## '+table,'','| Column | SQL type | Nullable | Keys |','| --- | --- | --- | --- |']
    for c in [x for x in sql['columns'] if x['tableName']==table]:
        t=c['sqlType'];length=c['maxLength']
        if t in ['nvarchar','nchar']:t+='(max)' if length<0 else '('+str(length//2)+')'
        elif t in ['varchar','char','varbinary','binary']:t+='(max)' if length<0 else '('+str(length)+')'
        keys=[]
        if c['identityColumn']:keys.append('IDENTITY')
        for k in sql['indexes']:
            if k['tableName']==table and k['columnName']==c['columnName'] and (k['primaryKey'] or k['uniqueKey']):keys.append(('PK' if k['primaryKey'] else 'UQ/AK')+': '+k['indexName'])
        for f in sql['foreignKeys']:
            if f['dependentTable']==table and f['dependentColumn']==c['columnName']:keys.append('FK → '+f['principalTable']+'.'+f['principalColumn'])
        dictionary.append(f"| {c['columnName']} | {t} | {'Yes' if c['nullable'] else 'No'} | {'; '.join(dict.fromkeys(keys)) or '—'} |")
    dictionary+=['']
write('data-dictionary.md','\n'.join(dictionary)+'\n')

spec=readjson(final/'evidence/openapi.json');operations=[]
for route,item in spec['paths'].items():
    for verb,op in item.items():
        if verb in ['get','post','put','delete','patch']:operations.append((verb.upper(),route,op))
api=['# Current API reference and verification','','Verified against live OpenAPI and actual HTTPS request results in `evidence/openapi.json` and `evidence/api-results.json`. This current reference supersedes the historical Phase 3 scaffold/mount descriptions.','','Direct base: https://localhost:7192. Public transport: https://localhost:7290/mems-public/api/mems-api followed by the canonical /api/... path. Root tenant UI lives at https://localhost:7290/{slug}; the legacy /mems-public/ entry redirects to the root.','','## Complete live OpenAPI operations','','| Method | Route | Documented statuses |','| --- | --- | --- |']
for verb,route,op in operations:api.append('| '+verb+' | `'+route+'` | '+', '.join(op.get('responses',{}))+' |')
api+=['','Health/liveness and readable docs may be excluded from the generated operation list: `/health`, `/alive`, `/api/docs`; machine-readable spec is `/openapi/v1.json`. These are operational endpoints, not missing domain CRUD.','','## Authentication and tenancy','','Anonymous users can read public discovery/election/media DTOs. Authenticated municipal API requests remain restricted to the stored assignment even on GET; the Public app deliberately omits credentials for anonymous information browsing. Super Admin alone manages municipalities; positions/candidates require the assigned municipal actor or Super Admin. Unknown slugs and foreign IDs fail safely. Client-supplied IDs or role headers cannot grant scope.','','Legitimate cookie flow: GET `/api/auth/csrf`; POST `/api/auth/login` with local credentials and `X-CSRF-TOKEN`; obtain a fresh principal-bound CSRF token; retain the Secure/HttpOnly Identity cookie. Modifying cookie requests require the token; PUT/DELETE/media changes also need the current quoted `If-Match` version. Do not put real credentials, cookie values or tokens in shared examples or a client asset. No JWT or new authentication mechanism was introduced.','','## JSON examples','','Candidate POST/PUT request (illustrative position ID; resolve an authorized actual parent first):','','```json',json.dumps(dict(positionId=1,name='Fictional Example Candidate',profile='Public election biography.',email='candidate@example.test',website='https://example.test'),indent=2),'```','','Position request:','','```json','{ "name": "Councillor", "seatsAvailable": 6 }','```','','Municipality request (Super Admin):','','```json',json.dumps(dict(slug='example-town',name='Example Town',province='British Columbia',electionDate='2026-10-17',description='Fictional election information.',brandColor='#245678',publicTitle='Example Town election guide'),indent=2),'```','','The actual public response and validation/error examples below are exported from live requests; concurrency versions are public opaque DTO fields, not credentials.']
for result in readjson(final/'evidence/api-results.json')['results']:
    api+=['','### '+result['method']+' '+result['url']+' → '+str(result['status']),'','```json',json.dumps(result['body'],indent=2),'```']
api+=['','## Validation, concurrency and errors','','Candidate queries support `search`, `positionId`, `page`, `pageSize` (1–100) and `sort=name|created`, with stable ID tie-breaking. Slugs are normalized and unique; names/biographies have bounded lengths; contact URLs/email, seats and parent ownership are validated in UI/services/API and appropriate SQL constraints. Unknown JSON properties are rejected. DTOs exclude EF navigations, Identity rows, password hashes, connection strings and filesystem names.','','Expected responses include 200/201/204 success; 400 safe validation/CSRF errors; 401 unauthenticated; 403 forbidden; 404 unknown/foreign record; 409 duplicate/dependency/concurrency conflict; generic 500 without internal details. Oversized/unsupported/spoofed media is rejected and cookie modifications require HTTPS. POST/PUT/DELETE and media scenarios are exercised by the fresh regression suite, not the screenshots alone.','','Full safe model definitions are included in the OpenAPI snapshot. The endpoint tests cover all domain CRUD, uploads, scope, error and authentication requirements without exposing a voter-selection endpoint.','']
write('api-reference.md','\n'.join(api))

def inline(s):
    s=html.escape(s)
    s=re.sub(r'`([^`]+)`',r'<code>\1</code>',s)
    s=re.sub(r'\*\*([^*]+)\*\*',r'<strong>\1</strong>',s)
    s=re.sub(r'\[([^]]+)\]\(([^)]+)\)',r'<a href="\2">\1</a>',s)
    return s
def markdown(text):
    lines=text.splitlines();out=[];i=0
    while i<len(lines):
        s=lines[i].strip()
        if not s:i+=1;continue
        if s.startswith('```'):
            chunk=[];i+=1
            while i<len(lines) and not lines[i].strip().startswith('```'):chunk.append(lines[i]);i+=1
            out.append('<pre>'+html.escape('\n'.join(chunk))+'</pre>');i+=1;continue
        if s.startswith('|'):
            rows=[]
            while i<len(lines) and lines[i].strip().startswith('|'):
                cells=lines[i].strip().strip('|').split('|')
                if not all(re.fullmatch(r'[\s:\-]+',c) for c in cells):rows.append(cells)
                i+=1
            out.append('<table><thead><tr>'+''.join('<th>'+inline(c.strip())+'</th>' for c in rows[0])+'</tr></thead><tbody>'+''.join('<tr>'+''.join('<td>'+inline(c.strip())+'</td>' for c in row)+'</tr>' for row in rows[1:])+'</tbody></table>');continue
        image=re.fullmatch(r'!\[([^]]*)\]\(([^)]+)\)',s)
        if image:
            out.append('<figure class="diagram"><h2>'+html.escape(image[1])+'</h2><img src="'+image[2]+'" alt="'+html.escape(image[1])+'"/></figure>');i+=1;continue
        heading=re.match(r'^(#{1,4}) (.+)',s)
        if heading:level=min(4,len(heading[1]));out.append(f'<h{level}>'+inline(heading[2])+f'</h{level}>');i+=1;continue
        if s.startswith('- ') or re.match(r'^\d+\. ',s):out.append('<p class="item">'+inline(s)+'</p>');i+=1;continue
        p=[s];i+=1
        while i<len(lines) and lines[i].strip() and not re.match(r'^(#|\||```|!\[|- |\d+\. )',lines[i].strip()):p.append(lines[i].strip());i+=1
        out.append('<p>'+inline(' '.join(p))+'</p>')
    return '\n'.join(out)

sections=['architecture.md','technical-guide.md','api-reference.md','deficiency-resolution.md','ai-usage.md','verification.md','rubric-and-readiness.md','publishing-and-package-plan.md','data-dictionary.md']
assert all((final/p).exists() for p in sections),'Finish verification/readiness sources before rendering the final report.'
style='''@page{size:A4;margin:15mm} @page wide{size:A4 landscape;margin:12mm}body{font:10.5pt/1.5 Arial,sans-serif;color:#183447;margin:0}h1{font-size:23pt;line-height:1.18;margin:0 0 18pt}h2{font-size:16pt;margin-top:22pt}h3{font-size:12pt}h1,h2,h3{break-after:avoid}p{overflow-wrap:anywhere}a{color:#245b73;text-decoration:none}code{font:9pt Consolas,monospace}pre{font:8.3pt/1.4 Consolas,monospace;background:#f0f4f7;padding:10pt;white-space:pre-wrap;overflow-wrap:anywhere;border:1px solid #d7e0e6}table{width:100%;border-collapse:collapse;table-layout:fixed;font-size:8pt;margin:12pt 0}th,td{text-align:left;vertical-align:top;border:1px solid #cbd5df;padding:5pt;overflow-wrap:anywhere}th{background:#e8eff4}thead{display:table-header-group}tr{break-inside:avoid}.section{break-before:page}.cover{break-after:page;padding:45mm 8mm 0}.cover h1{font-size:32pt}.cover .team{font-size:20pt}.cover .subtitle{font-size:15pt;color:#426175}.item{margin:6pt 0}.diagram{page:wide;break-before:page;break-after:page;margin:0;text-align:center}.diagram img{display:block;max-height:160mm;max-width:100%;margin:auto}.diagram h2{margin:0 0 9pt}.evidence{break-before:page;break-after:page;margin:0}.evidence img{max-width:100%;max-height:220mm;object-fit:contain;display:block;margin:auto}.evidence h2{margin-top:0}.caption{font-size:9pt;color:#40576b}.small{font-size:9pt}'''
cover='<section class="cover"><p>BCIT · COMP4976 · Assignment 2</p><h1>Municipal Elections<br/>Management System</h1><p class="subtitle">Final technical documentation and verification — review copy</p><p class="team">Abdullah Rehan<br/>Alex Fisher</p><p>Prepared November 17, 2026 (Pacific)</p><p>Seven-project .NET 10 solution · SQL Server · ASP.NET Identity · Aspire</p><p>No Azure deployment required. Start through <code>aspire run</code>.</p><p class="small">Prepared for student review. GitHub publishing and submission ZIP creation remain unapproved. Written instructor feedback and actual Copilot activity are unavailable; the report identifies those gaps rather than fabricating them.</p></section>'
body=cover
for name in sections:body+='<section class="section" data-source="'+name+'">'+markdown((final/name).read_text(encoding='utf-8'))+'</section>'
body+='<section class="section"><h1>Real application evidence</h1><p>Fresh Phase 7 screenshots and actual request/query exports. No mockups or mocked responses were used. The SQL image is a clearly labeled read-only live-query export, not an SSMS capture. Original full-resolution files and sanitized provenance accompany this report.</p></section>'
for title,file,caption in [
 ('Aspire orchestration','aspire-dashboard.png','Actual authenticated dashboard: retained SQL and application resources.'),
 ('Blazor Server administration','admin-dashboard.png','Actual Super Admin dashboard, with database-backed totals after test cleanup.'),
 ('Administration candidate management','admin-candidates.png','Existing authenticated candidate management interface.'),
 ('Burnaby public election site','public-burnaby.png','Tenant-scoped WASM candidate directory with existing portraits.'),
 ('Surrey public election site','public-surrey.png','Same reusable app, Surrey records and branding.'),
 ('Richmond public election site','public-richmond.png','Richmond fictional profiles and avatars.'),
 ('Personal voting guide','public-guide.png','Real anonymous guide. Choices stayed in the browser; this screenshot intentionally records demonstration evidence.'),
 ('Web API documentation','api-documentation.png','Real locally served API documentation.'),
 ('Web API testing','api-testing.png','Actual request/status export: 200,400,404,401,403. No cookies or tokens exported.'),
 ('SQL Server schema and data','sql-server.png','Actual read-only SQL metadata/count results rendered for legibility. No stored Identity row values are shown.')]:
    body+='<figure class="evidence"><h2>'+title+'</h2><p class="caption">'+caption+'</p><img src="evidence/'+file+'" alt="'+title+'"/></figure>'
document='<!doctype html><html lang="en"><head><meta charset="utf-8"><title>MEMS Assignment 2 — Abdullah Rehan and Alex Fisher</title><style>'+style+'</style></head><body>'+body+'</body></html>'
write('report.html',document)
write('README.md','# Final documentation review pack\n\nOpen `MEMS-Assignment-2-Final-Report.pdf` for the consolidated report. `report.html` and the adjacent Markdown are editable offline sources; `diagrams/` contains editable SVGs and `evidence/` contains curated actual screenshots/exports. `publication-manifest.json` is a dry-run path/hash list, not an archive or staged Git index.\n\nTeam names are confirmed. Actual Copilot activity and written grading feedback remain unverified; see the AI and deficiency sections. No repository has been initialized/published and no final submission ZIP has been created.\n')
print(f'Generated SQL dictionary, current API reference ({len(operations)} operations) and offline report HTML.')
