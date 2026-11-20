# Phase 2 development seeds

Initial domain batch: `election-samples-v1`. Initial account batch: `administrator-samples-v1`. Both are recorded in SeedHistory. Setup uses an EF execution strategy, fresh scoped Identity managers/context, a serializable SQL transaction, and a transaction-owned SQL application lock. API is the sole startup migration/seed owner; Admin waits for API health through Aspire.

Once a batch marker exists, that batch does not run again. Deleted samples remain deleted, edited profiles/photos remain edited, and existing administrator passwords are not reset. Future seed additions must use a new explicit version, not clear markers. Never delete the markers to repair individual records. Existing records encountered on an initial batch are reused, not overwritten; conflicting account/role assignments fail rather than silently changing ownership.

| Tenant / slug | Fictional candidates |
| --- | --- |
| Burnaby / burnaby | Vivian Kessler; Adrian Solano; Mei Tanaka; Rafael Baptiste; Sara Benali; Patrick Rowden |
| Surrey / surrey | Amara Iqbal; Gregory Chao; Noor Whitmore; Julian Mensah; Elena Varga; Hassan Beaumont |
| Richmond / richmond | River Shah; Tessa Laurent; Mina Ortega; Noah Lin; Anika Sterling; Leo Faris |
| Pitt Meadows / pitt-meadows | Mara Ellison; Owen Hartwell; Priya Nand; Eli Mercer; Nadia Farooq; Caleb Winslow |
| Maple Ridge / maple-ridge | Simone Calder; Daniel Hwang; Aisha Moreno; Marcus Bellamy; Leena Desai; Thomas Okafor |

Each tenant initially has Mayor (1 seat), Councilor (6 seats, retaining Assignment 1 spelling), and School Trustee (2 seats). These are demonstration seat counts, not claims about actual municipal elections. Two fictional candidates per position give 15 positions and 30 candidates overall. Election date October 17, 2026 and relevant model limits are retained from Assignment 1. Sample contacts use example.org. Logos and colors are original demonstration branding, not official municipal marks.

| Account | Scope | Assignment / roles |
| --- | --- | --- |
| super | System | Super Admin; no tenant assignment |
| Burnabyadmin | Municipality | Burnaby; Municipality Admin + Burnaby Admin |
| Surreyadmin | Municipality | Surrey; Municipality Admin + Surrey Admin |
| Richmondadmin | Municipality | Richmond; Municipality Admin + Richmond Admin |
| pm | Municipality | Pitt Meadows; Municipality Admin + Pitt Meadows Admin |

Seven roles: Super Admin, Municipality Admin, and one named Admin group for each of the five municipalities. The rubric's `Super` and Assignment 1's `super` identify the same normalized Identity account, not two case-variant accounts. No Maple Ridge administrator account is required or invented.

The 24 original fictional biographies/keys and packaged JPEG portraits were selectively adapted from Assignment 1, retaining contributor attribution and provenance. No database, upload, production secret or data-protection key was copied. The original portraits were documented there as synthetic, AI-generated demonstration assets. Six new Richmond profiles have distinct source-owned SVG initial avatars clearly labeled FICTIONAL DEMO AVATAR; these are placeholders, not photographs or AI-generated portraits. Five new SVG tenant logos are also fictional. Asset files live in MEMS.Api/wwwroot/images; no image-upload feature is implemented in this phase.

The rubric development/demo password is kept only in ignored `.local/identity-seed.json`, supplied to API through a secret Aspire parameter. It is used only when creating missing initial development accounts. Password hashing uses ASP.NET Core Identity; lockout remains five failures for fifteen minutes. The existing SQL development secret file is preserved unchanged. Data-protection keys are separate, ignored, and protected with Windows DPAPI on this machine. No production bootstrap credential or finished login interface was added.
