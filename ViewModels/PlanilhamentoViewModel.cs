using System.Collections.ObjectModel;
using System.Globalization;
using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using BalancoPatrimonial.App.Services;
using BalancoPatrimonial.App.Views.Items;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalancoPatrimonial.App.ViewModels;

public partial class PlanilhamentoViewModel : BaseViewModel
{
    private readonly IPlanilhamentoController _controller;
    private readonly ISessaoUsuario _sessao;

    public PlanilhamentoViewModel(IPlanilhamentoController controller, ISessaoUsuario sessao)
    {
        _controller = controller;
        _sessao = sessao;
        Title = "Planilhamento";
        CarregarContasAnosETipos();
    }

    [ObservableProperty]
    private string _subtitulo = "Carregando plano de contas...";

    public List<Empresa> TodasEmpresas { get; private set; } = new();
    public ObservableCollection<Empresa> EmpresasBuscadas { get; } = new();

    [ObservableProperty]
    private string _buscaEmpresaText = string.Empty;

    [ObservableProperty]
    private bool _isBuscandoEmpresa;

    [ObservableProperty]
    private Empresa? _empresaSelecionada;

    [ObservableProperty]
    private bool _isPainelAddPeriodoVisivel;

    public ObservableCollection<string> AnosDisponiveis { get; } = new();
    [ObservableProperty]
    private string? _addAnoSelecionado;

    public ObservableCollection<string> MesesDisponiveis { get; } = new();
    [ObservableProperty]
    private string? _addMesSelecionado;

    public ObservableCollection<string> TiposDisponiveis { get; } = new();
    [ObservableProperty]
    private string? _addTipoSelecionado;

    public ObservableCollection<PeriodoPlanilhado> Periodos { get; } = new();
    public List<LinhaContaPlanilhamento> Linhas { get; private set; } = new();
    public ObservableCollection<ItemTabelaPlanilhamento> ItensTabela { get; } = new();
    public List<ContaPadrao> PlanoContas { get; private set; } = new();

    // Dicionário de resultados importados para cruzamento
    public Dictionary<(int ano, TipoBalanco tipo), Dre> DresImportadas { get; } = new();

    public event Action? PlanoCarregado;
    public event Action? ReconstruirTabelaAction;
    public event Action? AtualizarKpisAction;
    public event Action? FecharResultadosAction;
    public event Action<string>? MostrarMensagemAction;
    public event Action<AnaliseAutomaticaResultado>? MostrarRevisaoIaAction;

    private void CarregarContasAnosETipos()
    {
        int anoAtual = DateTime.Now.Year;
        for (int i = 0; i < 10; i++)
            AnosDisponiveis.Add((anoAtual - i).ToString());

        var meses = new[] { "Ano inteiro", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho", "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro" };
        foreach (var m in meses) MesesDisponiveis.Add(m);

        TiposDisponiveis.Add("Individual");
        TiposDisponiveis.Add("Consolidado");

        AddAnoSelecionado = (anoAtual - 1).ToString();
        AddMesSelecionado = "Ano inteiro";
        AddTipoSelecionado = "Individual";
    }

        public async Task RecarregarPlanoPreservandoValoresAsync()
    {
        var snapshot = new Dictionary<(int contaId, int perIdx), decimal>();
        foreach (var linha in Linhas.Where(l => l.EhEditavel))
            foreach (var kv in linha.Celulas)
                if (kv.Value.Valor != 0)
                    snapshot[(linha.Conta.Id, kv.Key)] = kv.Value.Valor;

        var rContas = await _controller.ListarPlanoDeContasAsync();
        if (!rContas.Sucesso || rContas.Dados is null) return;

        PlanoContas = rContas.Dados.ToList();
        Linhas = LinhaContaPlanilhamento.ConstruirHierarquia(PlanoContas);

        foreach (var ((contaId, perIdx), valor) in snapshot)
        {
            if (perIdx >= Periodos.Count) continue;
            var linha = Linhas.FirstOrDefault(l => l.Conta.Id == contaId);
            if (linha is not null && linha.EhEditavel)
                linha.ObterCelula(perIdx).Valor = valor;
        }

        AtualizarItensTabela();
        ReconstruirTabelaAction?.Invoke();
    }

    public async Task InicializarAsync() {
        IsBusy = true;
        try
        {
            var rEmpresas = await _controller.ListarEmpresasAsync();
            if (rEmpresas.Sucesso && rEmpresas.Dados is not null)
                TodasEmpresas = rEmpresas.Dados.ToList();

            var rContas = await _controller.ListarPlanoDeContasAsync();
            if (rContas.Sucesso && rContas.Dados is not null)
            {
                PlanoContas = rContas.Dados.ToList();
                Linhas = LinhaContaPlanilhamento.ConstruirHierarquia(PlanoContas);

                Periodos.Clear();
                Periodos.Add(new PeriodoPlanilhado
                {
                    Mes = null,
                    Ano = DateTime.Now.Year - 1,
                    Tipo = TipoBalanco.Individual
                });

                AtualizarSubtitulo();
                AtualizarItensTabela();
                PlanoCarregado?.Invoke();
                AtualizarKpisAction?.Invoke();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void AtualizarSubtitulo()
    {
        if (EmpresaSelecionada is not null)
            Subtitulo = $"Planilhando {EmpresaSelecionada.RazaoSocial} ({Periodos.Count} período(s))";
        else
            Subtitulo = $"Vínculo não definido ({Periodos.Count} período(s))";
    }

    public void AtualizarItensTabela()
    {
        ItensTabela.Clear();
        for (int idx = 0; idx < Linhas.Count; idx++)
        {
            var l = Linhas[idx];
            l.AtualizarCelulasVisiveis(Periodos.Count); // ensure the row has cells
            ItensTabela.Add(new ItemTabelaPlanilhamento { Tipo = TipoItemTabela.LinhaConta, Linha = l });
            
            if (l.EhEditavel && l.Pai is not null)
            {
                bool ultimaDoPai = (idx == Linhas.Count - 1) || !ReferenceEquals(Linhas[idx + 1].Pai, l.Pai);
                if (ultimaDoPai)
                {
                    ItensTabela.Add(new ItemTabelaPlanilhamento { Tipo = TipoItemTabela.BotaoAdicionar, ContaPaiIdParaAdicionar = l.Pai.Conta.Id });
                }
            }
        }
    }

}