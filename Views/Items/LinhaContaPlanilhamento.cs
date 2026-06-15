using System.Globalization;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BalancoPatrimonial.App.Views.Items;

/// <summary>
/// Linha da tabela de planilhamento. Representa uma conta padrão com
/// suas referências hierárquicas (pai/filhas) e um dicionário de células
/// por índice de período.
///
/// O índice de período corresponde à posição na lista <c>_periodos</c>
/// da PlanilhamentoPage. Quando o usuário remove um período, os índices
/// são re-mapeados.
/// </summary>
public class LinhaContaPlanilhamento
{
    public ContaPadrao Conta { get; init; } = null!;
    public LinhaContaPlanilhamento? Pai { get; set; }
    public List<LinhaContaPlanilhamento> Filhas { get; } = new();

    public int Nivel => Conta.Nivel;
    public string Codigo => Conta.Codigo;
    public string Descricao => Conta.Descricao;
    public GrupoContaPrincipal Grupo => Conta.GrupoPrincipal;
    public bool EhTotalizadora => Conta.EhTotalizadora;
    public bool EhEditavel => !EhTotalizadora;

    /// <summary>Indentação visual baseada no nível (16px por nível).</summary>
    public Thickness Indentacao => new((Nivel - 1) * 16, 0, 0, 0);

    /// <summary>Atributos de fonte (negrito pra totalizadoras).</summary>
    public FontAttributes Estilo => EhTotalizadora ? FontAttributes.Bold : FontAttributes.None;

    /// <summary>Células por índice de período. Lazy: cria sob demanda.</summary>
    public Dictionary<int, CelulaPlanilhamento> Celulas { get; } = new();

    /// <summary>Obtém (ou cria) a célula para o índice do período.</summary>
    public CelulaPlanilhamento ObterCelula(int perIdx)
    {
        if (!Celulas.TryGetValue(perIdx, out var c))
        {
            c = new CelulaPlanilhamento(this, perIdx);
            Celulas[perIdx] = c;
        }
        return c;
    }

    /// <summary>
    /// Recomputa o valor desta linha NUMA COLUNA específica (índice de período).
    /// Chamado pelas filhas após edição. Propaga pro pai recursivamente.
    /// </summary>
    public void RecomputarColuna(int perIdx)
    {
        if (!EhTotalizadora) return;
        var soma = Filhas.Sum(f => f.ObterCelula(perIdx).Valor);
        ObterCelula(perIdx).DefinirValorSemPropagar(soma);
        Pai?.RecomputarColuna(perIdx);
    }

    /// <summary>
    /// Constrói a hierarquia a partir de uma lista plana de contas padrão.
    /// Retorna a lista achatada em ordem de exibição (depth-first pre-order).
    /// </summary>
    public static List<LinhaContaPlanilhamento> ConstruirHierarquia(IEnumerable<ContaPadrao> contas)
    {
        var todas = contas.OrderBy(c => c.Ordem).ThenBy(c => c.Codigo).ToList();
        var linhas = todas.ToDictionary(c => c.Id, c => new LinhaContaPlanilhamento { Conta = c });

        var raizes = new List<LinhaContaPlanilhamento>();
        foreach (var c in todas)
        {
            var l = linhas[c.Id];
            if (c.ContaPaiId is null)
            {
                raizes.Add(l);
            }
            else if (linhas.TryGetValue(c.ContaPaiId.Value, out var pai))
            {
                l.Pai = pai;
                pai.Filhas.Add(l);
            }
        }

        var achatada = new List<LinhaContaPlanilhamento>();
        void Visitar(LinhaContaPlanilhamento l)
        {
            achatada.Add(l);
            foreach (var f in l.Filhas.OrderBy(f => f.Conta.Ordem).ThenBy(f => f.Conta.Codigo))
                Visitar(f);
        }
        foreach (var r in raizes.OrderBy(r => r.Conta.Ordem).ThenBy(r => r.Conta.Codigo))
            Visitar(r);

        return achatada;
    }
}

/// <summary>
/// Célula da tabela (valor de uma conta em um período).
/// Quando o usuário edita o Entry, dispara recálculo do pai naquela coluna.
/// </summary>
public class CelulaPlanilhamento : ObservableObject
{
    private static readonly CultureInfo _ptBR = new("pt-BR");

    public LinhaContaPlanilhamento Linha { get; }
    public int PeriodoIdx { get; set; }    // mutável pra suportar remoção/re-mapping

    private decimal _valor;
    public decimal Valor
    {
        get => _valor;
        set
        {
            if (SetProperty(ref _valor, value))
            {
                OnPropertyChanged(nameof(ValorTexto));
                OnPropertyChanged(nameof(ValorFormatado));
                if (Linha.EhEditavel)
                {
                    Linha.Pai?.RecomputarColuna(PeriodoIdx);
                }
            }
        }
    }

    /// <summary>String pra binding two-way no Entry. Aceita formato BR (1.234,56) ou inglês.</summary>
    public string ValorTexto
    {
        get => _valor == 0 ? string.Empty : _valor.ToString("N2", _ptBR);
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Valor = 0;
                return;
            }
            // Tenta parsing brasileiro primeiro
            if (decimal.TryParse(value, NumberStyles.Any, _ptBR, out var v1))
            {
                Valor = v1;
                return;
            }
            // Fallback: limpa pontos, troca vírgula por ponto, parseia invariant
            var limpo = value.Replace(".", "").Replace(",", ".");
            if (decimal.TryParse(limpo, NumberStyles.Any, CultureInfo.InvariantCulture, out var v2))
            {
                Valor = v2;
            }
        }
    }

    public string ValorFormatado => _valor.ToString("N2", _ptBR);

    public CelulaPlanilhamento(LinhaContaPlanilhamento linha, int periodoIdx)
    {
        Linha = linha;
        PeriodoIdx = periodoIdx;
    }

    /// <summary>Atualiza o valor sem propagar (uso interno pra recálculo de totalizadoras).</summary>
    public void DefinirValorSemPropagar(decimal valor)
    {
        if (_valor != valor)
        {
            _valor = valor;
            OnPropertyChanged(nameof(Valor));
            OnPropertyChanged(nameof(ValorTexto));
            OnPropertyChanged(nameof(ValorFormatado));
        }
    }

    /// <summary>
    /// Converte um texto digitado (formato BR "1.234,56" ou inglês "1234.56")
    /// em decimal. Retorna null quando o texto não representa um número válido.
    /// Centraliza a lógica de parsing usada pelos Entries da planilha.
    /// </summary>
    public static decimal? TentarParsear(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;

        // Tenta o formato brasileiro primeiro (1.234,56)
        if (decimal.TryParse(texto, NumberStyles.Any, _ptBR, out var v1))
            return v1;

        // Fallback: remove separador de milhar, troca vírgula por ponto, parseia invariant
        var limpo = texto.Replace(".", "").Replace(",", ".");
        if (decimal.TryParse(limpo, NumberStyles.Any, CultureInfo.InvariantCulture, out var v2))
            return v2;

        return null;
    }
}
