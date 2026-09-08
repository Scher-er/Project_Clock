using CommunityToolkit.Mvvm.Input;

namespace BalancoPatrimonial.App.ViewModels;

public partial class AnalisesViewModel
{
    [RelayCommand]
    private async Task GerarParecerIaAsync()
    {
        if (_ultimaAnalise == null || _ultimaAnalise.Evolucao.Count == 0) return;

        IsBuscandoParecer = true;
        ParecerIa = "A IA está analisando os balanços históricos e os KPIs para gerar um parecer. Isso pode levar alguns segundos...";
        IsParecerVisivel = true;

        var resultado = await _controller.GerarParecerIaAsync(_ultimaAnalise);

        IsBuscandoParecer = false;

        if (resultado.Sucesso && !string.IsNullOrEmpty(resultado.Dados))
        {
            ParecerIa = resultado.Dados;
        }
        else
        {
            ParecerIa = "Falha ao gerar parecer IA: " + resultado.Mensagem;
        }
    }
}
