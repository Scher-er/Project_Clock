using Microsoft.Extensions.DependencyInjection;
using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.Views.Items;

namespace BalancoPatrimonial.App.Views;

/// <summary>
/// Máscara de visualização dos balanços armazenados de uma empresa.
/// Aberta como página modal a partir da EmpresasPage (botão "Ver Balanços").
///
/// Recebe o id e o nome da empresa via <see cref="Inicializar"/> antes de
/// ser exibida.
/// </summary>
public partial class BalancosEmpresaPage : ContentPage
{
    private readonly IBalancoController _balancoController;
    private int _empresaId;
    private string _empresaNome = string.Empty;

    public BalancosEmpresaPage(IBalancoController balancoController)
    {
        InitializeComponent();
        _balancoController = balancoController;
    }

    /// <summary>Define a empresa a visualizar. Chamar antes de abrir a página.</summary>
    public void Inicializar(int empresaId, string empresaNome)
    {
        _empresaId = empresaId;
        _empresaNome = empresaNome;
        lblTitulo.Text = $"Balanços · {empresaNome}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        var r = await _balancoController.ListarPorEmpresaAsync(_empresaId);

        if (!r.Sucesso || r.Dados is null)
        {
            lblSubtitulo.Text = "Erro ao carregar balanços.";
            painelVazio.IsVisible = true;
            lvBalancos.IsVisible = false;
            return;
        }

        var balancos = r.Dados.ToList();
        if (balancos.Count == 0)
        {
            lblSubtitulo.Text = $"{_empresaNome} ainda não tem balanços armazenados.";
            painelVazio.IsVisible = true;
            lvBalancos.IsVisible = false;
            return;
        }

        // Ordena por ano desc, depois tipo
        var itens = balancos
            .OrderByDescending(b => b.AnoExercicio)
            .ThenBy(b => b.TipoBalanco)
            .Select(b => new BalancoResumoItem(b))
            .ToList();

        lblSubtitulo.Text = $"{itens.Count} balanço(s) armazenado(s).";
        painelVazio.IsVisible = false;
        lvBalancos.IsVisible = true;
        lvBalancos.ItemsSource = itens;
    }

    private async void OnFecharClicado(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnBalancoTocado(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not int balancoId) return;

        var page = App.Services.GetRequiredService<DetalheBalancoPage>();
        page.Inicializar(balancoId);
        await Navigation.PushAsync(page);
    }
}
