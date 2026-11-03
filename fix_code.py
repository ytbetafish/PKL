p = r'Plain Craft Launcher 2\metadata.json'
t = open(p, encoding='utf-8-sig').read()
t = t.replace('"code": 524', '"code": 99999')
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
