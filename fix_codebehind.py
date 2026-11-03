p = r'Plain Craft Launcher 2\Pages\PageSettings\PageSettings.xaml.cs'
lines = open(p, encoding='utf-8-sig').readlines()
out = []
skip = False
i = 0
while i < len(lines):
    line = lines[i]
    # Drop any line containing LabKspPath
    if 'LabKspPath' in line:
        i += 1
        continue
    # Drop BtnPickKspPath_Click method block
    if line.strip().startswith('private void BtnPickKspPath_Click('):
        # skip until matching closing brace at column 4
        i += 1
        while i < len(lines) and lines[i].strip() != '}':
            i += 1
        i += 1  # skip the '}'
        if i < len(lines) and lines[i].strip() == '':
            i += 1
        continue
    out.append(line)
    i += 1
open(p, 'w', encoding='utf-8-sig').write(''.join(out))
print('ok')
