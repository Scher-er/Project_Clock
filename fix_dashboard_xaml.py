import re

with open('Views/DashboardPage.xaml', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"', '<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"\n             xmlns:models="clr-namespace:BalancoPatrimonial.App.Models"')
code = code.replace('<DataTemplate>', '<DataTemplate x:DataType="models:Empresa">')

with open('Views/DashboardPage.xaml', 'w', encoding='utf-8') as f:
    f.write(code)
