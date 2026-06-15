using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Models;

/// <summary>
/// Resultado da análise automática completa de um PDF pelo Gemini.
///
/// Diferente do <see cref="ImportacaoPdfResultado"/> (que extrai 1 período),
/// este captura TUDO que a IA conseguiu inferir do documento:
///   - Dados da empresa (pra cadastro automático)
///   - Múltiplos períodos (cada coluna do planilhamento)
///   - Tipo de cada período (Individual / Consolidado)
///
/// O app usa isso pra: cadastrar a empresa se não existir, criar os períodos
/// e preencher todas as contas — sem intervenção manual.
/// </summary>
public class AnaliseAutomaticaResultado
{
    public string HashPdf { get; set; } = string.Empty;
    public string NomeArquivo { get; set; } = string.Empty;

    // ─── Empresa detectada ───
    public string RazaoSocial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public TipoEmpresa TipoEmpresa { get; set; } = TipoEmpresa.Outro;
    public string? UfAtuacao { get; set; }

    // ─── Períodos detectados (cada um vira uma coluna) ───
    public List<PeriodoDetectado> Periodos { get; set; } = new();

    // ─── Mensagens / avisos ───
    public List<string> Avisos { get; set; } = new();

    public bool TemEmpresa => !string.IsNullOrWhiteSpace(RazaoSocial);
    public int TotalContas => Periodos.Sum(p => p.ContasMapeadas.Count);
}

/// <summary>Um período detectado no PDF, com suas contas.</summary>
public class PeriodoDetectado
{
    public int Ano { get; set; }

    /// <summary>Mês (1-12) ou null se balanço anual.</summary>
    public int? Mes { get; set; }

    public TipoBalanco Tipo { get; set; } = TipoBalanco.Individual;

    /// <summary>Total do Ativo conforme IMPRESSO no PDF (gabarito de conferência).</summary>
    public decimal? AtivoTotalImpresso { get; set; }

    /// <summary>Total de Passivo + PL conforme IMPRESSO no PDF (gabarito).</summary>
    public decimal? PassivoPlTotalImpresso { get; set; }

    /// <summary>
    /// Subtotais IMPRESSOS por subgrupo (código → valor), lidos direto do PDF:
    /// "1.01"→Ativo Circulante, "1.02"→Ativo Não Circ., "2.01"→Passivo Circ.,
    /// "2.02"→Passivo Não Circ., "2.03"→Patrimônio Líquido. Usados pra
    /// reconciliação determinística (cada subgrupo fecha com seu subtotal).
    /// </summary>
    public Dictionary<string, decimal> SubtotaisImpressos { get; set; } = new();

    /// <summary>Contas mapeadas (id da ContaPadrao → valor em reais).</summary>
    public Dictionary<int, decimal> ContasMapeadas { get; set; } = new();

    /// <summary>
    /// Comparação por subgrupo (extraído pelo app x impresso no PDF), preenchida
    /// durante a reconciliação. Alimenta a tela de revisão lado a lado.
    /// </summary>
    public List<ComparacaoSubgrupo> Comparacoes { get; set; } = new();

    // ─── DRE (valores-chave, se o PDF tiver Demonstração do Resultado) ───
    public decimal DreReceitaLiquida { get; set; }
    public decimal DreLucroBruto { get; set; }
    public decimal DreResultadoOperacional { get; set; }
    public decimal DreDespesasFinanceiras { get; set; }
    public decimal DreLucroLiquido { get; set; }

    public bool TemDre =>
        DreReceitaLiquida != 0 || DreLucroBruto != 0 || DreResultadoOperacional != 0
        || DreDespesasFinanceiras != 0 || DreLucroLiquido != 0;

    /// <summary>Label legível pra mensagens (ex: "2024 (Consolidado)").</summary>
    public string Label
    {
        get
        {
            var per = Mes.HasValue ? $"{Mes:D2}/{Ano}" : Ano.ToString();
            var tp = Tipo == TipoBalanco.Consolidado ? "Consolidado" : "Individual";
            return $"{per} ({tp})";
        }
    }
}

/// <summary>
/// Comparação de um subgrupo entre o valor que entrou no app (soma das contas
/// extraídas/reconciliadas) e o subtotal impresso lido do PDF.
/// </summary>
public record ComparacaoSubgrupo(
    string Codigo,
    string Rotulo,
    decimal Extraido,
    decimal? Impresso,
    decimal Ajuste)
{
    /// <summary>Diferença residual após o ajuste (idealmente ~0).</summary>
    public decimal? Diferenca => Impresso is decimal imp ? imp - Extraido : null;

    /// <summary>True se o subgrupo fecha com o impresso (tolerância R$ 1).</summary>
    public bool Fecha => Diferenca is decimal d && Math.Abs(d) <= 1m;
}
