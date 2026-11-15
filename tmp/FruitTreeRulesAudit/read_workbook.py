import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET


ns = {'x': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
path = Path(__file__).with_name('Nature_Paradise_13_Fruit_Trees.xlsx')
with zipfile.ZipFile(path) as book:
    relationships = ET.fromstring(book.read('xl/_rels/workbook.xml.rels'))
    parts = {rel.attrib['Id']: rel.attrib['Target'] for rel in relationships}
    names = ET.fromstring(book.read('xl/workbook.xml'))
    shared = []
    if 'xl/sharedStrings.xml' in book.namelist():
        values = ET.fromstring(book.read('xl/sharedStrings.xml'))
        shared = [''.join(node.text or '' for node in cell.findall('.//x:t', ns)) for cell in values]
    for sheet in names.findall('.//x:sheet', ns):
        target = parts[next(value for key, value in sheet.attrib.items() if key.endswith('}id'))]
        target = target.lstrip('/') if target.startswith('/') else 'xl/' + target
        print('\n' + sheet.attrib['name'])
        for row in ET.fromstring(book.read(target)).findall('.//x:row', ns):
            tokens = []
            for cell in row.findall('x:c', ns):
                value = cell.findtext('x:v', namespaces=ns)
                kind = cell.get('t')
                if kind == 's':
                    value = shared[int(value)]
                elif kind == 'inlineStr':
                    value = ''.join(node.text or '' for node in cell.findall('.//x:t', ns))
                formula = cell.findtext('x:f', namespaces=ns)
                if formula:
                    value = f'={formula} [{value}]'
                if value:
                    tokens.append(f"{cell.get('r')}={value}")
            if tokens:
                print(' | '.join(tokens))
