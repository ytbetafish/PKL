p = r'src\Pkl.Ckan\ModService.cs'
t = open(p, encoding='utf-8-sig').read()
old = '''                try
                {
                    _registryManager.registry.Repositories.Clear();
                    _registryManager.registry.Repositories["PKL-Mirror"] = new CKAN.Repository("PKL-Mirror", MirrorUrl);
                    _registryManager.Save();
                }
                catch { /* best effort */ }'''
new = '''                try
                {
                    _registryManager.registry.RepositoriesClear();
                    _registryManager.registry.RepositoriesAdd(new CKAN.Repository("PKL-Mirror", MirrorUrl));
                    _registryManager.Save();
                }
                catch { /* best effort */ }'''
assert old in t, 'old not found'
t = t.replace(old, new, 1)
open(p, 'w', encoding='utf-8-sig').write(t)
print('ok')
