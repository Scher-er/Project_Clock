import re

with open('Views/PlanilhamentoPage.xaml', 'r', encoding='utf-8') as f:
    xaml = f.read()

# Replace txtBuscaEmpresa
xaml = re.sub(
    r'<Entry\s+x:Name="txtBuscaEmpresa"[\s\S]*?/>',
    '<Entry x:Name="txtBuscaEmpresa"\n                               BackgroundColor="Transparent"\n                               Placeholder="Buscar empresa por nome ou CNPJ..."\n                               VerticalOptions="Fill"\n                               Text="{Binding BuscaEmpresaText}"\n                               Focused="OnBuscaEmpresaFocused" />',
    xaml
)

with open('Views/PlanilhamentoPage.xaml', 'w', encoding='utf-8') as f:
    f.write(xaml)
