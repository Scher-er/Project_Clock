using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Models;

/// <summary>
/// Balanço patrimonial de uma empresa em um ano específico.
/// Uma empresa pode ter múltiplos balanços (um por ano). Há, no máximo,
/// um balanço por empresa+ano+tipo (constraint UNIQUE no MySQL).
/// </summary>
public class Balanco
{
    public int Id { get; set; }

    public int EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }

    /// <summary>Ano de exercício (2020, 2021, 2022...).</summary>
    public int AnoExercicio { get; set; }

    /// <summary>Data de referência (geralmente 31/12 do ano de exercício).</summary>
    public DateTime DataReferencia { get; set; }

    public TipoBalanco TipoBalanco { get; set; }

    /// <summary>Data em que o balanço foi planilhado no sistema.</summary>
    public DateTime DataPlanilhamento { get; set; } = DateTime.Now;

    /// <summary>Usuário que realizou o planilhamento.</summary>
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    /// <summary>
    /// Origem dos dados — "PDF: relatorio_2023.pdf" ou "Manual".
    /// Hash MD5 do arquivo PDF (se importado) ajuda a detectar reimportações.
    /// </summary>
    public string? Origem { get; set; }
    public string? HashOrigemPdf { get; set; }

    public string? Observacoes { get; set; }

    /// <summary>
    /// Moeda em que os valores estão expressos. Padrão "BRL", suportar
    /// "USD" no futuro pra empresas com balanço em dólar.
    /// </summary>
    public string Moeda { get; set; } = "BRL";

    /// <summary>
    /// Multiplicador dos valores. Comum em DFs: "valores em milhares de reais"
    /// → MultiplicadorValores=1000. Persistido só pra rastreabilidade — os
    /// valores em ContaBalanco já são armazenados na unidade base (R$).
    /// </summary>
    public int MultiplicadorValores { get; set; } = 1;

    // Navegação
    public List<ContaBalanco> Contas { get; set; } = new();

    // ───── Totalizadores calculados (não persistidos) ─────

    public decimal AtivoTotal => SomaPorCodigo("1");
    public decimal AtivoCirculante => SomaPorCodigo("1.01");
    public decimal AtivoNaoCirculante => SomaPorCodigo("1.02");
    public decimal PassivoCirculante => SomaPorCodigo("2.01");
    public decimal PassivoNaoCirculante => SomaPorCodigo("2.02");
    public decimal PatrimonioLiquido => SomaPorCodigo("2.03");
    public decimal PassivoTotal => PassivoCirculante + PassivoNaoCirculante + PatrimonioLiquido;

    /// <summary>
    /// Soma todas as contas analíticas cujo código pertence à hierarquia do
    /// código informado. Como apenas contas analíticas são persistidas (as
    /// totalizadoras tipo "1", "2.03" não são salvas), a soma é feita por
    /// PREFIXO: "1" soma 1.01.01, 1.01.02, 1.02.01...; "2.03" soma 2.03.01...
    /// </summary>
    private decimal SomaPorCodigo(string codigoTotalizador)
        => Contas
            .Where(c => c.ContaPadrao?.Codigo is string cod
                        && (cod == codigoTotalizador
                            || cod.StartsWith(codigoTotalizador + ".", StringComparison.Ordinal)))
            .Sum(c => c.Valor);
}
