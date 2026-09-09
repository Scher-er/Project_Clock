import re

with open('Views/PlanilhamentoPage.xaml', 'r', encoding='utf-8') as f:
    code = f.read()

# Replace <Grid RowDefinitions="Auto, Auto, Auto, Auto, Auto, *, Auto" Padding="0">
# With a wrapper Grid
replacement = '''    <Grid>
        <Grid RowDefinitions="Auto, Auto, Auto, Auto, Auto, *, Auto" Padding="0">'''

code = code.replace('<Grid RowDefinitions="Auto, Auto, Auto, Auto, Auto, *, Auto" Padding="0">', replacement)

# Add the overlay at the end before </ContentPage>
overlay = '''
        <!-- LOADING OVERLAY -->
        <Grid BackgroundColor="{AppThemeBinding Light=#80FFFFFF, Dark=#80000000}" 
              IsVisible="{Binding IsBusy}"
              ZIndex="100">
            <VerticalStackLayout HorizontalOptions="Center" VerticalOptions="Center" Spacing="16">
                <ActivityIndicator IsRunning="True" Color="#1F6FEB" WidthRequest="48" HeightRequest="48"/>
                <Label Text="{Binding Subtitulo}" TextColor="{AppThemeBinding Light=#111, Dark=#FFF}" FontAttributes="Bold" HorizontalOptions="Center"/>
            </VerticalStackLayout>
        </Grid>
    </Grid>
'''
code = code.replace('</Grid>\n</ContentPage>', '</Grid>\n' + overlay + '</ContentPage>')

with open('Views/PlanilhamentoPage.xaml', 'w', encoding='utf-8') as f:
    f.write(code)
