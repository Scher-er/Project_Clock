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

[QueryProperty(nameof(EmpresaId), "id")]
public partial class CadastroEmpresaViewModel : BaseViewModel
{
    private readonly IEmpresaController _controller;
    private readonly IExportacaoController _exportController;
    private readonly IListagensService _listagens;

    private int _id;
    public int EmpresaId
    {
        get => _id;
        set 
        { 
            _id = value; 
            _ = CarregarAsync(); 
        }
    }

    [ObservableProperty]
    private string _cnpj = string.Empty;

    [ObservableProperty]
    private string _razaoSocial = string.Empty;

    [ObservableProperty]
    private string _nomeFantasia = string.Empty;

    [ObservableProperty]
    private string _uf = string.Empty;

    [ObservableProperty]
    private string _localAtuacao = string.Empty;

    [ObservableProperty]
    private string _limiteCredito = string.Empty;

    [ObservableProperty]
    private TipoEmpresaWrapper? _tipoSelecionado;

    [ObservableProperty]
    private string _ratingSelecionado = "(sem rating)";

    [ObservableProperty]
    private GrupoEconomico? _grupoSelecionado;

    [ObservableProperty]
    private string _contagemSetores = "Nenhum selecionado";

    [ObservableProperty]
    private string _subtitulo = string.Empty;

    [ObservableProperty]
    private bool _podeExportar;

    [ObservableProperty]
    private bool _temBalancos;

    [ObservableProperty]
    private string _qtdBalancos = "0";

    public ObservableCollection<TipoEmpresaWrapper> Tipos { get; } = new();
    public ObservableCollection<string> Ratings { get; } = new();
    public ObservableCollection<GrupoEconomico> Grupos { get; } = new();
    public ObservableCollection<SetorAtividade> Setores { get; } = new();
    public ObservableCollection<object> SetoresSelecionados { get; } = new();
    public ObservableCollection<BalancoListItem> Balancos { get; } = new();

    public CadastroEmpresaViewModel(IEmpresaController controller, IExportacaoController exportController, IListagensService listagens)
    {
        _controller = controller;
        _exportController = exportController;
        _listagens = listagens;

        SetoresSelecionados.CollectionChanged += (s, e) => AtualizarContagemSetores();
        PopularDropdownsEstaticos();
    }

    private void PopularDropdownsEstaticos()
    {
        Tipos.Add(new TipoEmpresaWrapper(TipoEmpresa.SaAberta, "S.A. de capital aberto"));
        Tipos.Add(new TipoEmpresaWrapper(TipoEmpresa.SaFechada, "S.A. de capital fechado"));
        Tipos.Add(new TipoEmpresaWrapper(TipoEmpresa.Ltda, "LTDA"));
        Tipos.Add(new TipoEmpresaWrapper(TipoEmpresa.Eireli, "EIRELI"));
        Tipos.Add(new TipoEmpresaWrapper(TipoEmpresa.Mei, "MEI"));
        Tipos.Add(new TipoEmpresaWrapper(TipoEmpresa.Outro, "Outro"));
        TipoSelecionado = Tipos[0];

        var ratings = new[] { "(sem rating)", "AAA", "AA+", "AA", "AA-", "A+", "A", "A-", "BBB+", "BBB", "BBB-", "BB+", "BB", "BB-", "B+", "B", "B-", "CCC", "CC", "C", "D" };
        foreach (var r in ratings) Ratings.Add(r);
        RatingSelecionado = Ratings[0];
    }

    private async Task CarregarAsync()
    {
        IsBusy = true;
        try
        {
            var gruposList = await _listagens.ListarGruposEconomicosAsync();
            Grupos.Clear();
            Grupos.Add(new GrupoEconomico { Id = 0, Nome = "(sem grupo)" });
            foreach (var g in gruposList) Grupos.Add(g);
            GrupoSelecionado = Grupos[0];

            var setoresList = await _listagens.ListarSetoresAsync();
            Setores.Clear();
            foreach (var s in setoresList) Setores.Add(s);

            if (_id <= 0)
            {
                Title = "Nova Empresa";
                Subtitulo = "Preencha os dados abaixo.";
                PodeExportar = false;
            }
            else
            {
                Title = "Editar Empresa";
                PodeExportar = true;
                var resultado = await _controller.BuscarCompletaAsync(_id);
                if (resultado.Sucesso && resultado.Dados != null)
                {
                    PreencherCampos(resultado.Dados);
                }
                else
                {
                    await Application.Current!.MainPage!.DisplayAlert("Aviso", "Não foi possível carregar: " + resultado.Mensagem, "OK");
                    await Shell.Current.GoToAsync("..");
                }
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Erro", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void PreencherCampos(Empresa e)
    {
        Subtitulo = $"Editando '{e.RazaoSocial}'.";
        Cnpj = FormatarCnpj(e.Cnpj);
        RazaoSocial = e.RazaoSocial;
        NomeFantasia = e.NomeFantasia ?? string.Empty;
        Uf = e.UfAtuacao ?? string.Empty;
        LocalAtuacao = e.LocalAtuacao ?? string.Empty;
        LimiteCredito = e.LimiteCredito?.ToString("N2", new CultureInfo("pt-BR")) ?? string.Empty;

        TipoSelecionado = Tipos.FirstOrDefault(t => t.Valor == e.TipoEmpresa) ?? Tipos[0];
        RatingSelecionado = string.IsNullOrEmpty(e.Rating) ? Ratings[0] : (Ratings.FirstOrDefault(r => r == e.Rating) ?? Ratings[0]);
        GrupoSelecionado = e.GrupoEconomicoId.HasValue ? (Grupos.FirstOrDefault(g => g.Id == e.GrupoEconomicoId.Value) ?? Grupos[0]) : Grupos[0];

        var idsVinculados = e.Setores.Select(s => s.Id).ToHashSet();
        SetoresSelecionados.Clear();
        foreach (var s in Setores)
        {
            if (idsVinculados.Contains(s.Id))
            {
                SetoresSelecionados.Add(s);
            }
        }

        Balancos.Clear();
        if (e.Balancos.Count > 0)
        {
            TemBalancos = true;
            QtdBalancos = e.Balancos.Count switch
            {
                1 => "1 balanço",
                _ => $"{e.Balancos.Count} balanços"
            };
            foreach (var b in e.Balancos)
            {
                Balancos.Add(BalancoListItem.De(b));
            }
        }
        else
        {
            TemBalancos = false;
        }
    }

    private void AtualizarContagemSetores()
    {
        var n = SetoresSelecionados.Count;
        ContagemSetores = n switch
        {
            0 => "Nenhum selecionado",
            1 => "1 selecionado (principal)",
            _ => $"{n} selecionados"
        };
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        IsBusy = true;
        try
        {
            var empresa = ConstruirEmpresa();
            var setoresIds = SetoresSelecionados.OfType<SetorAtividade>().Select(s => s.Id).ToList();

            if (_id <= 0)
            {
                var r = await _controller.CriarComSetoresAsync(empresa, setoresIds);
                if (r.Sucesso)
                {
                    await Application.Current!.MainPage!.DisplayAlert("Pronto", r.Mensagem, "OK");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Application.Current!.MainPage!.DisplayAlert("Erro", MontarMensagem(r.Mensagem, r.Erros), "OK");
                }
            }
            else
            {
                empresa.Id = _id;
                var r = await _controller.AtualizarAsync(empresa);
                if (r.Sucesso)
                {
                    await Application.Current!.MainPage!.DisplayAlert("Pronto", "Empresa atualizada.", "OK");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Application.Current!.MainPage!.DisplayAlert("Erro", MontarMensagem(r.Mensagem, r.Erros), "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Erro inesperado", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelarAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task ExportarAsync(string formato)
    {
        try
        {
            var resultado = formato switch
            {
                "Excel" => await _exportController.ExportarExcelAsync(_id),
                "PDF"   => await _exportController.ExportarPdfAsync(_id),
                _       => await _exportController.ExportarJsonAsync(_id)
            };

            if (!resultado.Sucesso || resultado.Dados is null)
            {
                await Application.Current!.MainPage!.DisplayAlert("Erro", resultado.Mensagem, "OK");
                return;
            }

            var caminho = resultado.Dados;
            var nomeArquivo = Path.GetFileName(caminho);

            var compartilhar = await Application.Current!.MainPage!.DisplayAlert(
                $"{formato} gerado",
                $"Arquivo salvo em:\n\n{caminho}\n\nDeseja compartilhar/abrir?",
                "Compartilhar", "Fechar");

            if (compartilhar)
            {
                try
                {
                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = nomeArquivo,
                        File = new ShareFile(caminho)
                    });
                }
                catch (Exception ex)
                {
                    await Application.Current!.MainPage!.DisplayAlert("Aviso",
                        $"Não foi possível abrir o compartilhamento, mas o arquivo está em:\n{caminho}\n\nErro: {ex.Message}",
                        "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Erro inesperado", ex.Message, "OK");
        }
    }

    private Empresa ConstruirEmpresa()
    {
        var tipo = TipoSelecionado?.Valor ?? TipoEmpresa.Ltda;
        var rating = RatingSelecionado;
        if (rating == "(sem rating)") rating = null;

        int? grupoId = null;
        if (GrupoSelecionado != null && GrupoSelecionado.Id > 0) grupoId = GrupoSelecionado.Id;

        decimal? limite = null;
        if (!string.IsNullOrWhiteSpace(LimiteCredito)
            && decimal.TryParse(LimiteCredito, NumberStyles.Any, new CultureInfo("pt-BR"), out var lim))
        {
            limite = lim;
        }

        return new Empresa
        {
            Cnpj = Cnpj ?? string.Empty,
            RazaoSocial = RazaoSocial?.Trim() ?? string.Empty,
            NomeFantasia = string.IsNullOrWhiteSpace(NomeFantasia) ? null : NomeFantasia.Trim(),
            UfAtuacao = string.IsNullOrWhiteSpace(Uf) ? null : Uf.Trim().ToUpper(),
            LocalAtuacao = string.IsNullOrWhiteSpace(LocalAtuacao) ? null : LocalAtuacao.Trim(),
            TipoEmpresa = tipo,
            Rating = rating,
            LimiteCredito = limite,
            GrupoEconomicoId = grupoId,
            Ativo = true
        };
    }

    private static string MontarMensagem(string mensagem, IReadOnlyList<string> erros)
    {
        if (erros.Count == 0) return mensagem;
        return $"{mensagem}\n\n• {string.Join("\n• ", erros)}";
    }

    private static string FormatarCnpj(string cnpj)
    {
        if (string.IsNullOrEmpty(cnpj) || cnpj.Length != 14) return cnpj;
        return $"{cnpj[..2]}.{cnpj[2..5]}.{cnpj[5..8]}/{cnpj[8..12]}-{cnpj[12..14]}";
    }
}

public record TipoEmpresaWrapper(TipoEmpresa Valor, string Nome)
{
    public override string ToString() => Nome;
}