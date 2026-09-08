import re

with open('Views/AnalisesPage.xaml', 'r', encoding='utf-8') as f:
    xaml = f.read()

# Fix IsVisible
xaml = re.sub(r'IsVisible="\{Binding IsPainelVazioVisivel\}"\s*Style="\{StaticResource Card\}"\s*IsVisible="True"',
              r'IsVisible="{Binding IsPainelVazioVisivel}"\n                    Style="{StaticResource Card}"', xaml)

# Fix Text MensagemVazia
xaml = re.sub(r'Text="\{Binding MensagemVazia\}"\s*Text="Selecione uma empresa acima\."',
              r'Text="{Binding MensagemVazia}"', xaml)

# Fix IsCarregandoVisivel
xaml = re.sub(r'IsVisible="\{Binding IsCarregandoVisivel\}"\s*IsVisible="False"',
              r'IsVisible="{Binding IsCarregandoVisivel}"', xaml)

with open('Views/AnalisesPage.xaml', 'w', encoding='utf-8') as f:
    f.write(xaml)
