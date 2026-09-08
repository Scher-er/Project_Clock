using System.Collections.ObjectModel;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using BalancoPatrimonial.App.Views.Items;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalancoPatrimonial.App.ViewModels;

public partial class PlanilhamentoViewModel
{
    private bool _atualizandoTextoPorSelecao = false;

    partial void OnBuscaEmpresaTextChanged(string value)
    {
        if (_atualizandoTextoPorSelecao) return;

        var termo = (value ?? string.Empty).Trim().ToLowerInvariant();

        if (EmpresaSelecionada is not null && !string.Equals(termo, EmpresaSelecionada.RazaoSocial, StringComparison.OrdinalIgnoreCase))
        {
            EmpresaSelecionada = null;
            AtualizarSubtitulo();
        }

        if (string.IsNullOrEmpty(termo))
        {
            IsBuscandoEmpresa = false;
            EmpresasBuscadas.Clear();
            return;
        }

        var digitosTermo = new string(termo.Where(char.IsDigit).ToArray());

        var filtradas = TodasEmpresas
            .Where(emp =>
                emp.RazaoSocial.ToLowerInvariant().Contains(termo)
                || (digitosTermo.Length >= 2 && (emp.Cnpj ?? "").Contains(digitosTermo)))
            .Take(10)
            .ToList();

        EmpresasBuscadas.Clear();
        foreach (var emp in filtradas)
            EmpresasBuscadas.Add(emp);

        IsBuscandoEmpresa = true;
    }

    [RelayCommand]
    private void BuscaEmpresaFocused()
    {
        if (!_atualizandoTextoPorSelecao && !string.IsNullOrWhiteSpace(BuscaEmpresaText) && EmpresaSelecionada == null)
            IsBuscandoEmpresa = true;
    }

    [RelayCommand]
    private void EmpresaSelecionadaResult(Empresa? emp)
    {
        if (emp == null) return;
        _atualizandoTextoPorSelecao = true;
        BuscaEmpresaText = emp.RazaoSocial;
        EmpresaSelecionada = emp;
        _atualizandoTextoPorSelecao = false;
        
        IsBuscandoEmpresa = false;
        FecharResultadosAction?.Invoke();
        AtualizarSubtitulo();
    }

    [RelayCommand]
    private void AdicionarPeriodoClicado()
    {
        IsPainelAddPeriodoVisivel = true;
    }

    [RelayCommand]
    private void ConfirmarAdicionarPeriodo()
    {
        if (string.IsNullOrEmpty(AddAnoSelecionado) || string.IsNullOrEmpty(AddTipoSelecionado)) return;

        int ano = int.Parse(AddAnoSelecionado);
        int? mes = null;
        if (!string.IsNullOrEmpty(AddMesSelecionado) && AddMesSelecionado != "Ano inteiro")
        {
            var mArr = new[] { "Ano inteiro", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho", "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro" };
            mes = Array.IndexOf(mArr, AddMesSelecionado);
        }

        var tp = AddTipoSelecionado == "Consolidado" ? TipoBalanco.Consolidado : TipoBalanco.Individual;

        if (Periodos.Any(p => p.Ano == ano && p.Mes == mes && p.Tipo == tp))
        {
            MostrarMensagemAction?.Invoke("Este período já está adicionado na tabela.");
            return;
        }

        Periodos.Add(new PeriodoPlanilhado { Ano = ano, Mes = mes, Tipo = tp });
        IsPainelAddPeriodoVisivel = false;
        AtualizarSubtitulo();
        ReconstruirTabelaAction?.Invoke();
        AtualizarKpisAction?.Invoke();
    }

    [RelayCommand]
    private void CancelarAdicionarPeriodo()
    {
        IsPainelAddPeriodoVisivel = false;
    }

    [RelayCommand]
    private void RemoverPeriodo(PeriodoPlanilhado p)
    {
        if (Periodos.Count <= 1)
        {
            MostrarMensagemAction?.Invoke("Você precisa de pelo menos 1 período na planilha.");
            return;
        }
        Periodos.Remove(p);
        AtualizarSubtitulo();
        ReconstruirTabelaAction?.Invoke();
        AtualizarKpisAction?.Invoke();
    }

    [RelayCommand]
    private async Task LimparAsync()
    {
        Periodos.Clear();
        Periodos.Add(new PeriodoPlanilhado { Mes = null, Ano = DateTime.Now.Year - 1, Tipo = TipoBalanco.Individual });
        Linhas = LinhaContaPlanilhamento.ConstruirHierarquia(PlanoContas);
        EmpresaSelecionada = null;
        _atualizandoTextoPorSelecao = true;
        BuscaEmpresaText = string.Empty;
        _atualizandoTextoPorSelecao = false;
        IsBuscandoEmpresa = false;
        DresImportadas.Clear();
        AtualizarSubtitulo();
        ReconstruirTabelaAction?.Invoke();
        AtualizarKpisAction?.Invoke();
    }
}
