import re

with open('Views/AnalisesPage.xaml', 'r', encoding='utf-8') as f:
    xaml = f.read()

# Add xmlns
xaml = xaml.replace('xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"',
                    'xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"\n             xmlns:viewmodels="clr-namespace:BalancoPatrimonial.App.ViewModels"\n             x:DataType="viewmodels:AnalisesViewModel"')

# Add bindings
xaml = xaml.replace('<Label x:Name="lblSubtitulo"', '<Label Text="{Binding Subtitulo}"')

xaml = xaml.replace('<Picker x:Name="pkEmpresa"', '<Picker x:Name="pkEmpresa" ItemsSource="{Binding Empresas}" ItemDisplayBinding="{Binding RazaoSocial}" SelectedItem="{Binding EmpresaSelecionada}"')
xaml = re.sub(r'SelectedIndexChanged="OnEmpresaSelecionada"', '', xaml)

xaml = xaml.replace('<Border x:Name="painelVazio"', '<Border x:Name="painelVazio" IsVisible="{Binding IsPainelVazioVisivel}"')
xaml = xaml.replace('<Label x:Name="lblMensagemVazia"', '<Label Text="{Binding MensagemVazia}"')

xaml = xaml.replace('<VerticalStackLayout x:Name="painelAnalise" IsVisible="False"', '<VerticalStackLayout x:Name="painelAnalise" IsVisible="{Binding IsPainelAnaliseVisivel}"')

xaml = xaml.replace('<Label x:Name="lblKpiAno" Text="-"', '<Label Text="{Binding KpiAno}"')
xaml = xaml.replace('<Label x:Name="lblKpiAtivo" Text="-"', '<Label Text="{Binding KpiAtivo}"')
xaml = xaml.replace('<Label x:Name="lblKpiPassivo" Text="-"', '<Label Text="{Binding KpiPassivo}"')
xaml = xaml.replace('<Label x:Name="lblKpiPL" Text="-"', '<Label Text="{Binding KpiPL}"')
xaml = xaml.replace('<Label x:Name="lblKpiLiquidez" Text="-"', '<Label Text="{Binding KpiLiquidez}"')
xaml = xaml.replace('<Label x:Name="lblKpiEndividamento" Text="-"', '<Label Text="{Binding KpiEndividamento}"')
xaml = xaml.replace('<Label x:Name="lblKpiComposicao" Text="-"', '<Label Text="{Binding KpiComposicao}"')
xaml = xaml.replace('<Label x:Name="lblKpiImobilizacao" Text="-"', '<Label Text="{Binding KpiImobilizacao}"')

# Remove x:Name from Labels
xaml = re.sub(r'<Label\s+x:Name="lblKpi[a-zA-Z]+"', '<Label', xaml)
xaml = re.sub(r'<Label\s+x:Name="lblSubtitulo"', '<Label', xaml)
xaml = re.sub(r'<Label\s+x:Name="lblMensagemVazia"', '<Label', xaml)

# Charts
xaml = xaml.replace('<lvc:CartesianChart x:Name="chartEvolucao"', '<lvc:CartesianChart Series="{Binding SeriesEvolucao}" XAxes="{Binding XAxesEvolucao}" YAxes="{Binding YAxesEvolucao}"')
xaml = xaml.replace('<Label x:Name="lblTituloComposicaoAtivo" Text="Composição do Ativo"', '<Label Text="{Binding TituloComposicaoAtivo}"')
xaml = xaml.replace('<lvc:PieChart x:Name="chartCompAtivo"', '<lvc:PieChart Series="{Binding SeriesCompAtivo}"')

xaml = xaml.replace('<Label x:Name="lblTituloComposicaoPassivo" Text="Composição Passivo/PL"', '<Label Text="{Binding TituloComposicaoPassivo}"')
xaml = xaml.replace('<lvc:PieChart x:Name="chartCompPassivoPL"', '<lvc:PieChart Series="{Binding SeriesCompPassivo}"')

xaml = xaml.replace('<lvc:CartesianChart x:Name="chartLiquidez"', '<lvc:CartesianChart Series="{Binding SeriesLiquidez}" XAxes="{Binding XAxesLiquidez}"')

# Parecer IA
xaml = xaml.replace('Clicked="OnGerarParecerIaClicado"', 'Command="{Binding GerarParecerIaCommand}"')

# Replace _isBuscandoParecer bindings (the button doesn't have an x:Name in the markup seen but we can do it later if needed)

with open('Views/AnalisesPage.xaml', 'w', encoding='utf-8') as f:
    f.write(xaml)
