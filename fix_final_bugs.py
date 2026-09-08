import re

with open('Views/AnalisesPage.xaml', 'r', encoding='utf-8') as f:
    xaml = f.read()

xaml = xaml.replace('<Label Text="{Binding Subtitulo}"\n                       Text="Selecione uma empresa para ver evoluA Ao e indicadores."',
                    '<Label Text="{Binding Subtitulo}"')

# Also fix if the encoding got weird characters. Wait, I shouldn't mess with evoluA Ao if it's already mangled, I'll just use a regex.
xaml = re.sub(r'<Label Text="\{Binding Subtitulo\}"\s+Text=".*?"', '<Label Text="{Binding Subtitulo}"', xaml)

with open('Views/AnalisesPage.xaml', 'w', encoding='utf-8') as f:
    f.write(xaml)

with open('Views/AnalisesPage.xaml.cs', 'r', encoding='utf-8') as f:
    cs = f.read()

cs = cs.replace('var difStr = (it.Diferenca > 0 ? "+" : "") + FormatarCurto(it.Diferenca);',
                'var diferenca = it.ValorAtual - it.ValorAnterior;\n            var difStr = (diferenca > 0 ? "+" : "") + FormatarCurto(diferenca);')

with open('Views/AnalisesPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(cs)
