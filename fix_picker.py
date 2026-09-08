import re

with open('Views/AnalisesPage.xaml', 'r', encoding='utf-8') as f:
    xaml = f.read()

# Fix ItemDisplayBinding duplicate
xaml = re.sub(r'ItemDisplayBinding="\{Binding RazaoSocial\}"\s+ItemDisplayBinding="\{Binding RazaoSocial\}"', 'ItemDisplayBinding="{Binding RazaoSocial}"', xaml)

with open('Views/AnalisesPage.xaml', 'w', encoding='utf-8') as f:
    f.write(xaml)
