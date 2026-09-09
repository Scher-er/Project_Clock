import re

with open('Views/PlanilhamentoPage.xaml', 'r', encoding='utf-8') as f:
    code = f.read()

replacement = '''                <Button Grid.Column="3" Text="Importar com IA"
                        Style="{StaticResource PrimaryButton}"
                        VerticalOptions="End" WidthRequest="150"
                        Clicked="OnImportarPdfComIaClicado"/>

                <Button Grid.Column="4" Text="Importar B3 (Mock)"
                        Style="{StaticResource PrimaryButton}" BackgroundColor="#2E8B57"
                        VerticalOptions="End" WidthRequest="150"
                        Clicked="OnImportarB3Clicado"/>'''

code = code.replace('''                <Button Grid.Column="3" Text="Importar com IA"
                        Style="{StaticResource PrimaryButton}"
                        VerticalOptions="End" WidthRequest="150"
                        Clicked="OnImportarPdfComIaClicado"/>''', replacement)

code = code.replace('<Grid ColumnDefinitions="*, *, *, *"', '<Grid ColumnDefinitions="*, *, *, *, *"')

with open('Views/PlanilhamentoPage.xaml', 'w', encoding='utf-8') as f:
    f.write(code)
