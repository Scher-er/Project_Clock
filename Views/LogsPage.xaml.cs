using System.Collections.ObjectModel;
using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.Views.Items;

namespace BalancoPatrimonial.App.Views;

public partial class LogsPage : ContentPage
{
    private readonly ILogController _controller;
    private readonly ObservableCollection<LogListItem> _items = new();

    public LogsPage(ILogController controller)
    {
        InitializeComponent();
        _controller = controller;
        lvLogs.ItemsSource = _items;
        dpData.Date = DateTime.Today;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { await CarregarAsync(); } catch { }
    }

    private async void OnDataSelecionada(object? sender, DateChangedEventArgs e)
        => await CarregarAsync();

    private async void OnRecarregarClicado(object? sender, EventArgs e)
        => await CarregarAsync();

    private async void OnRecarregar(object? sender, EventArgs e)
    {
        await CarregarAsync();
        refreshView.IsRefreshing = false;
    }

    private async void OnExportarClicado(object? sender, EventArgs e)
    {
        if (_items.Count == 0)
        {
            await DisplayAlert("Nada a exportar",
                "Não há logs para a data selecionada.", "OK");
            return;
        }

        try
        {
            var caminho = await _controller.ExportarXmlAsync(dpData.Date);
            await DisplayAlert("Exportado",
                $"{_items.Count} logs exportados para:\n\n{caminho}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Falha ao exportar: {ex.Message}", "OK");
        }
    }

    private async Task CarregarAsync()
    {
        try
        {
            var logs = (await _controller.ListarPorDataAsync(dpData.Date)).ToList();

            _items.Clear();
            foreach (var log in logs)
            {
                _items.Add(LogListItem.De(log));
            }

            var data = dpData.Date.ToString("dd/MM/yyyy");
            if (_items.Count == 0)
            {
                lblMensagemVazia.Text = $"Nenhum log para {data}.";
                painelVazio.IsVisible = true;
                lvLogs.IsVisible = false;
                lblSubtitulo.Text = $"Mostrando {data}.";
            }
            else
            {
                painelVazio.IsVisible = false;
                lvLogs.IsVisible = true;
                lblSubtitulo.Text = $"{_items.Count} eventos em {data}.";
            }
        }
        catch (Exception ex)
        {
            lblMensagemVazia.Text = $"Erro ao carregar logs: {ex.Message}";
            painelVazio.IsVisible = true;
            lvLogs.IsVisible = false;
        }
    }
}
