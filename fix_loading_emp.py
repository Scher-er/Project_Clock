import re

with open('Views/EmpresasPage.xaml', 'r', encoding='utf-8') as f:
    code = f.read()

replacement = '''    <Grid>
        <Grid RowDefinitions="Auto, Auto, *" Padding="24" RowSpacing="16">'''

code = code.replace('<Grid RowDefinitions="Auto, Auto, *" Padding="24" RowSpacing="16">', replacement)

overlay = '''
        <!-- LOADING OVERLAY -->
        <Grid BackgroundColor="{AppThemeBinding Light=#80FFFFFF, Dark=#80000000}" 
              IsVisible="{Binding IsBusy}"
              ZIndex="100">
            <VerticalStackLayout HorizontalOptions="Center" VerticalOptions="Center" Spacing="16">
                <ActivityIndicator IsRunning="True" Color="#1F6FEB" WidthRequest="48" HeightRequest="48"/>
                <Label Text="Carregando..." TextColor="{AppThemeBinding Light=#111, Dark=#FFF}" FontAttributes="Bold" HorizontalOptions="Center"/>
            </VerticalStackLayout>
        </Grid>
    </Grid>
'''
code = code.replace('</Grid>\n</ContentPage>', '</Grid>\n' + overlay + '</ContentPage>')

with open('Views/EmpresasPage.xaml', 'w', encoding='utf-8') as f:
    f.write(code)
