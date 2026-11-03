p = r'Plain Craft Launcher 2\metadata.json'
t = open(p, encoding='utf-8-sig').read()
t = t.replace('"base": "2.15.1"', '"base": "1.0.0"')
t = t.replace('"upstream": "2.12.1"', '"upstream": "2.15.1"')
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
