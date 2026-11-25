"""Independent PDF checks after the .NET integration test.
Usage: python verify_render.py path/to/synthetic.pdf path/to/rendered.png
Render first with: pdftoppm -f 1 -singlefile -scale-to 1200 -png synthetic.pdf rendered
Requires pypdf and Pillow. Does not modify files.
"""
import sys
from pypdf import PdfReader
from PIL import Image
reader = PdfReader(sys.argv[1])
text = reader.pages[0].extract_text()
for expected in ['Kulturtest 11.62', 'Nur Absatz ohne Span', 'ÄÖÜ äöü ß €', 'Fallback']:
    assert expected in text, expected
names = [str(f.get_object()['/BaseFont']) for f in reader.pages[0]['/Resources']['/Font'].values()]
assert any('Bold' in n for n in names), names
assert any('Italic' in n for n in names), names
image = Image.open(sys.argv[2]).convert('RGB')
# Test first field, above the separate plain-p-only second field and page numbers.
crop = image.crop((0, 0, image.width, image.height // 2))
visible = sum(1 for r,g,b in crop.get_flattened_data() if min(r,g,b) < 150)
assert visible > 1000, visible
print('PASS text, bold/italic faces and visible rendered pixels:', visible, names)
