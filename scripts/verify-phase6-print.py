"""Inspect real Chromium print-to-PDF output. Uses the existing workspace pypdf tool."""
import json, re, sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / '.tools/pdf'))
from pypdf import PdfReader

root = Path(__file__).resolve().parents[1] / 'artifacts/phase6'
fields = json.loads((root / 'print-fields.json').read_text())
normal = lambda value: re.sub(r'\s+', ' ', value).strip()
checks = []
for name in ['burnaby-guide', 'empty-guide', 'long-guide']:
    for size, dimensions in [('Letter', (612, 792)), ('A4', (595.28, 841.89))]:
        path = root / f'{name}-{size}.pdf'
        pdf = PdfReader(path)
        pages = [normal(page.extract_text()) for page in pdf.pages]
        text = ' '.join(pages)
        for page in pdf.pages:
            assert abs(float(page.mediabox.width) - dimensions[0]) < 1
            assert abs(float(page.mediabox.height) - dimensions[1]) < 1
        for required in ['My voting guide', fields['municipality'], 'Election date', 'October 17, 2026', 'Print date']:
            assert required in text, (path.name, required)
        assert 'Preparing date' not in text
        for hidden in ['Find candidates', 'Clear guide', 'Change municipality', 'Manage', 'Explore municipalities']:
            assert hidden not in text, (path.name, hidden)
        if name == 'burnaby-guide':
            assert len(pages) == 1
            for candidate in fields['candidates']:
                assert normal(candidate['name']) in text
                assert normal(candidate['position']) in text
        elif name == 'empty-guide':
            assert len(pages) == 1 and 'No candidates selected' in text
        else:
            assert len(pages) > 1
            for candidate in fields['longNames']:
                assert any(normal(candidate) in page for page in pages), 'Candidate row split or missing'
        checks.append(dict(file=path.name, pages=len(pages), correctPaperDimensions=True, requiredFieldsPresent=True, controlsHidden=True, rowsNotSplit=True))
result = dict(passed=True, checks=checks)
(root / 'print-verification.json').write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps(result, indent=2))
