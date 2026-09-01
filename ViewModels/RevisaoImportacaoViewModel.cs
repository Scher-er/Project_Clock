using System.Collections.ObjectModel;
using BalancoPatrimonial.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalancoPatrimonial.App.ViewModels;

public partial class RevisaoImportacaoViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _subtitulo = string.Empty;

    [ObservableProperty]
    private bool _temAvisos;

    public ObservableCollection<string> Avisos { get; } = new();
    public ObservableCollection<PeriodoDetectado> Periodos { get; } = new();

    public void Inicializar(AnaliseAutomaticaResultado dados)
    {
        Title = "Revisão da Importação";
        var emp = string.IsNullOrWhiteSpace(dados.RazaoSocial) ? "(empresa não identificada)" : dados.RazaoSocial;
        Subtitulo = $"{emp} — {dados.Periodos.Count} período(s) detectado(s)";

        Avisos.Clear();
        if (dados.Avisos.Count > 0)
        {
            TemAvisos = true;
            foreach (var a in dados.Avisos) Avisos.Add(a);
        }
        else
        {
            TemAvisos = false;
        }

        Periodos.Clear();
        foreach (var p in dados.Periodos)
        {
            Periodos.Add(p);
        }
    }

    [RelayCommand]
    private async Task ContinuarAsync()
    {
        await Application.Current!.MainPage!.Navigation.PopAsync();
    }
}