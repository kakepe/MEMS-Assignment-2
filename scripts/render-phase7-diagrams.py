"""Render editable, offline SVG engineering diagrams from the verified MEMS architecture."""
from html import escape
from pathlib import Path
out = Path(__file__).resolve().parents[1] / 'docs/final/diagrams'
out.mkdir(parents=True, exist_ok=True)
class Diagram:
    def __init__(self, title, width=1200, height=800):
        self.parts=[f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" role="img" aria-label="{escape(title)}"><title>{escape(title)}</title><defs><marker id="arrow" markerWidth="10" markerHeight="8" refX="9" refY="4" orient="auto"><path d="M0 0L10 4L0 8Z" fill="#40576b"/></marker></defs><style>text{{font-family:Arial,sans-serif;fill:#183447;font-size:18px}}.title{{font-size:21px;font-weight:bold}}.small{{font-size:16px}}.key{{font-family:Consolas,monospace;font-size:15px}}.label{{paint-order:stroke;stroke:#fff;stroke-width:5;stroke-linejoin:round}}.edge{{fill:none;stroke:#40576b;stroke-width:2;marker-end:url(#arrow)}}.dash{{stroke-dasharray:7 5}}</style><rect width="100%" height="100%" fill="white"/>']
    def text(self,x,y,value,kind='',color=None):
        color=f' style="fill:{color}"' if color else ''
        self.parts.append(f'<text x="{x}" y="{y}" class="{kind}"{color}>{escape(value)}</text>')
    def box(self,x,y,w,h,title,lines=(),fill='#edf4f7'):
        self.parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="9" fill="{fill}" stroke="#94aabb"/>')
        self.text(x+14,y+29,title,'title')
        for i,line in enumerate(lines): self.text(x+14,y+57+i*25,line,'small')
    def edge(self,path,label=None,x=0,y=0,dash=False):
        self.parts.append(f'<path d="{path}" class="edge {"dash" if dash else ""}"/>')
        if label:self.text(x,y,label,'small label')
    def table(self,x,y,w,title,rows):
        h=57+24*len(rows)
        self.box(x,y,w,h,title,fill='#f5f8fa')
        self.parts.append(f'<path d="M{x} {y+42}H{x+w}" stroke="#94aabb"/>')
        for i,row in enumerate(rows):self.text(x+12,y+65+24*i,row,'key')
        return h
    def save(self,name): (out/name).write_text(''.join(self.parts)+'</svg>',encoding='utf-8')

d=Diagram('MEMS system architecture and tenant isolation',1200,850)
d.box(370,15,790,95,'MEMS.AppHost — Aspire orchestration', ['References, configuration, WaitFor/readiness, SQL volume and dashboard.', 'Runtime resources: Admin, API, Public gateway/WASM, SQL Server.'])
d.parts.append('<rect x="370" y="145" width="790" height="675" rx="12" fill="#f8fafb" stroke="#8fa3b3" stroke-dasharray="8 5"/>')
d.text(390,175,'Server resources and shared server-side libraries','small')
d.edge('M760 110V145','orchestrates',775,134,True)
d.box(15,200,325,355,'MEMS.Public (browser WASM)',['One reusable app; Shared DTOs only.', 'Anonymous tenant experiences:'])
for y,label,color in [(300,'/burnaby — Burnaby only','#dbeafa'),(355,'/surrey — Surrey only','#dcefe5'),(410,'/richmond — Richmond only','#eee0ed')]:
    d.parts.append(f'<rect x="30" y="{y}" width="295" height="43" rx="6" fill="{color}"/>');d.text(42,y+27,label)
d.text(30,492,'Guide choices: native sessionStorage','small');d.text(30,519,'Tenant ID keys; never sent to backend.','small')
d.box(405,210,285,95,'Public gateway',['Root SPA + same-origin API proxy.','Managed runtime, not project #8.'])
d.box(815,210,310,95,'MEMS.Admin',['Shared Blazor Server UI at /admin.','Identity login + server authorization.'])
d.box(405,380,285,100,'MEMS.Api',['WebAPI + safe JSON DTOs.','Slug + actor checks, scoped media.'])
d.box(815,380,310,120,'MEMS.Data',['Shared services, EF Core and Identity.','Admin: a fresh scope per operation.','Tenant filters + validated writes.'])
d.edge('M340 255H405','HTTPS',345,242)
d.edge('M545 305V380','JSON GETs for public data',410,350)
d.edge('M690 430H815','services',720,418)
d.edge('M970 305V380','shared authorized services',830,345)
d.edge('M815 283H750V405H690','API discovery/probe',700,328,True)
d.box(405,535,285,70,'MEMS.Shared',['Safe DTOs + validation contracts.'])
d.edge('M405 565H340','contracts',344,590,True)
d.box(815,575,310,130,'SQL Server — Docker',['Database: MEMSAssignment2.','Tenant-aware keys/FKs + Identity.','No voter selections or vote totals.'])
d.edge('M970 500V575','EF Core / SQL Server',980,545)
d.text(825,740,'Volume: mems-assignment2-sql-data','small');d.text(825,766,'Mounted at /var/opt/mssql','small')
d.box(405,665,285,100,'MEMS.ServiceDefaults',['Admin/API health, logging, tracing,','service discovery. Server-only library.'])
d.box(15,660,325,100,'Aspire dashboard',['Readiness, structured logs, traces.','No browser voting telemetry.'])
d.edge('M405 715H340','OTLP',345,700,True)
d.text(20,805,'Isolation: URL slug → tenant-aware service/query → composite SQL ownership constraints. Super Admin has authorized system scope.','small')
d.save('architecture.svg')

d=Diagram('SQL Server domain ERD',1250,760)
d.text(20,30,'Domain ERD — actual SQL tables, PK/FK/UQ keys','title')
d.table(20,65,365,'Municipalities',['PK MunicipalityId : int','UQ Slug : nvarchar(80)','UQ Name : nvarchar(100)','Province : nvarchar(50)','ElectionDate : date','Description : nvarchar(2000)','LogoFileName : nvarchar(160)?','BrandColor : nvarchar(7)','PublicTitle : nvarchar(120)','RowVersion : rowversion'])
d.table(450,65,325,'Positions',['PK PositionId : int','FK MunicipalityId : int','Name : nvarchar(100)','SeatsAvailable : int [1..100]','RowVersion : rowversion','AK (MunicipalityId,PositionId)','UQ (MunicipalityId,Name)'])
d.table(850,65,375,'Candidates',['PK CandidateId : int','FK MunicipalityId : int','FK PositionId : int','Name : nvarchar(100)','Profile : nvarchar(4000)','PhotoFileName : nvarchar(160)?','Email : nvarchar(254)?','Website : nvarchar(300)?','DateCreated : datetime2 UTC','RowVersion : rowversion','UQ DemoSeedKey : nvarchar(80)?'])
d.edge('M385 140H450','1 : 0..*',388,126)
d.edge('M775 190H850','1 : 0..*',780,176)
d.edge('M200 362V440H1040V387','Municipality 1 : 0..* Candidate (direct FK)',390,427)
d.box(20,500,1205,210,'Database integrity and privacy',[
 'Candidate (MunicipalityId, PositionId) → Position (MunicipalityId, PositionId): cross-tenant parent assignments fail in SQL.',
 'Restrict parent deletes while positions, candidates or administrator assignments depend on the municipality/position.',
 'Normalized unique slugs, scoped position names, length/format checks and row-version optimistic concurrency.',
 'DemoSeedKey is a nullable filtered unique seed identity; completed seed batches do not reset edited/deleted samples.',
 'No ballot, voter selection, running tally or voter account table exists. Identity and history tables appear on the next ERD.'
 ])
d.save('erd-domain.svg')

d=Diagram('SQL Server Identity and history ERD',1250,850)
d.text(20,28,'Identity and history ERD — all remaining SQL tables','title')
d.table(15,70,245,'Municipalities',['PK MunicipalityId','Referenced tenant owner.'])
d.table(295,70,280,'AspNetUsers',['PK Id : nvarchar(128)','AK (Id,Scope)','FK MunicipalityId : int?','Scope : int [1 System;2 Tenant]','UQ NormalizedUserName','PasswordHash + Identity fields'])
d.table(630,70,240,'AspNetUserRoles',['PK/FK (UserId,RoleId)','Scope : int','FK (UserId,Scope)','FK (RoleId,Scope)'])
d.table(925,70,310,'AspNetRoles',['PK Id : nvarchar(128)','AK (Id,Scope)','FK MunicipalityId : int?','Scope : int','UQ NormalizedName'])
d.edge('M260 130H295','0..1',262,116)
d.edge('M575 165H630','1 : *',582,151)
d.edge('M925 165H870','1 : *',878,151)
d.edge('M140 70V42H1080V70','Municipality 0..1 : 0..* named roles',405,56)
d.table(15,410,260,'AspNetUserClaims',['PK Id : int','FK UserId','ClaimType, ClaimValue'])
d.table(325,410,300,'AspNetUserLogins',['PK (LoginProvider,ProviderKey)','FK UserId','ProviderDisplayName'])
d.table(670,410,300,'AspNetUserTokens',['PK (UserId,LoginProvider,Name)','FK UserId','Value'])
d.table(1010,410,225,'AspNetRoleClaims',['PK Id : int','FK RoleId','ClaimType, ClaimValue'])
d.edge('M385 271V335H145V410','1 : 0..* user-owned claims/logins/tokens',320,330)
d.edge('M435 271V410')
d.edge('M485 271V350H815V410')
d.edge('M1080 247V410','1 : 0..*',1095,340)
d.table(15,650,330,'SeedHistory',['PK SeedKey : nvarchar(80)','CompletedAtUtc : datetime2'])
d.table(390,650,400,'__EFMigrationsHistory',['PK MigrationId : nvarchar(150)','ProductVersion : nvarchar(32)'])
d.text(825,682,'Scope 2 user: exactly one tenant.','small');d.text(825,709,'Scope 1 user: no tenant (system).','small')
d.text(825,736,'Membership scope must match user + role.','small');d.text(825,763,'Named tenant-role matching also validated','small');d.text(825,787,'by Identity store; claims/tokens use cascades.','small')
d.save('erd-identity.svg')
print('Rendered architecture and two complete SQL ERD SVGs.')
