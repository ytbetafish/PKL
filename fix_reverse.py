p = r'src\Pkl.Ckan\ModService.cs'
t = open(p, encoding='utf-8-sig').read()
old = '''            var repos = Registry?.Repositories?.Values ?? Array.Empty<CKAN.Repository>();'''
new = '''            var repos = Registry?.Repositories?.Values ?? Enumerable.Empty<CKAN.Repository>();'''
assert old in t, 'old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
