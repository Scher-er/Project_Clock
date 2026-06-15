namespace BalancoPatrimonial.App.Models;

/// <summary>
/// Dados agregados de um balanço de um ano específico, prontos pra alimentar
/// gráficos e indicadores da AnalisesPage. Calculado a partir das contas
/// analíticas pelo AnaliseService — não fica no banco.
/// </summary>
public class EvolucaoBalancoPonto
{
    public int Ano { get; set; }
    public string TipoBalanco { get; set; } = string.Empty;

    public decimal AtivoCirculante { get; set; }
    public decimal AtivoNaoCirculante { get; set; }
    public decimal AtivoTotal => AtivoCirculante + AtivoNaoCirculante;

    public decimal PassivoCirculante { get; set; }
    public decimal PassivoNaoCirculante { get; set; }
    public decimal PassivoTotal => PassivoCirculante + PassivoNaoCirculante;
    public decimal PatrimonioLiquido { get; set; }
    public decimal PassivoMaisPL => PassivoTotal + PatrimonioLiquido;

    // Componentes específicos (pra liquidez seca/imediata/geral)
    public decimal Disponibilidades { get; set; }      // 1.01.01 Caixa e Equivalentes
    public decimal Estoques { get; set; }              // 1.01.04 Estoques
    public decimal RealizavelLongoPrazo { get; set; }  // 1.02.01 Realizável a LP

    /// <summary>Capital Circulante Líquido = Ativo Circ. - Passivo Circ.</summary>
    public decimal CapitalCirculanteLiquido => AtivoCirculante - PassivoCirculante;
}

/// <summary>
/// Indicadores derivados de um balanço (análise financeira clássica).
/// Valores null quando o denominador é zero (UI mostra "—").
/// </summary>
public class IndicadoresBalanco
{
    public int Ano { get; set; }

    // Liquidez
    public decimal? LiquidezCorrente { get; set; }
    public decimal? LiquidezSeca { get; set; }
    public decimal? LiquidezImediata { get; set; }
    public decimal? LiquidezGeral { get; set; }
    public decimal CapitalCirculanteLiquido { get; set; }

    // Endividamento / estrutura
    public decimal? EndividamentoGeral { get; set; }
    public decimal? ComposicaoEndividamento { get; set; }
    public decimal? ParticipacaoCapitalTerceiros { get; set; }
    public decimal? ImobilizacaoPL { get; set; }
    public decimal? ImobilizacaoRecursosNaoCorrentes { get; set; }

    // ─── Rentabilidade (dependem da DRE — null se não houver DRE do ano) ───
    public bool TemDre { get; set; }
    public decimal? MargemBruta { get; set; }          // Lucro Bruto / Receita
    public decimal? MargemOperacional { get; set; }    // EBIT / Receita
    public decimal? MargemLiquida { get; set; }        // Lucro Líquido / Receita
    public decimal? RetornoSobrePL { get; set; }       // ROE = LL / PL
    public decimal? RetornoSobreAtivo { get; set; }    // ROA = LL / Ativo
    public decimal? GiroAtivo { get; set; }            // Receita / Ativo
    public decimal? CoberturaJuros { get; set; }       // EBIT / Despesas Financeiras
}

/// <summary>Uma linha de análise vertical (peso da conta/grupo no total).</summary>
public record AnaliseVerticalItem(string Rotulo, decimal Valor, decimal? PercentualDoTotal);

/// <summary>Uma linha de análise horizontal (variação entre dois anos).</summary>
public record AnaliseHorizontalItem(string Rotulo, decimal ValorAnterior, decimal ValorAtual, decimal? VariacaoPercentual);

/// <summary>Classificação de risco de crédito derivada dos indicadores.</summary>
public class AvaliacaoRisco
{
    public int Score { get; set; }                 // 0-100 (maior = mais saudável)
    public string Classe { get; set; } = "—";      // A (baixo risco) a D (alto)
    public string NivelRisco { get; set; } = "—";  // "Risco baixo"...
    public string Parecer { get; set; } = string.Empty;
    public List<string> PontosFortes { get; set; } = new();
    public List<string> PontosAtencao { get; set; } = new();
}

/// <summary>
/// Resultado completo da análise de uma empresa.
/// </summary>
public class AnaliseEmpresa
{
    public Empresa Empresa { get; set; } = null!;
    public List<EvolucaoBalancoPonto> Evolucao { get; set; } = new();
    public List<IndicadoresBalanco> Indicadores { get; set; } = new();

    public ComposicaoBalanco? ComposicaoAtivo { get; set; }
    public ComposicaoBalanco? ComposicaoPassivoPL { get; set; }

    public List<AnaliseVerticalItem> AnaliseVertical { get; set; } = new();
    public List<AnaliseHorizontalItem> AnaliseHorizontal { get; set; } = new();
    public AvaliacaoRisco? Risco { get; set; }

    /// <summary>Tipo de balanço usado na análise (Individual ou Consolidado).</summary>
    public string TipoAnalisado { get; set; } = string.Empty;
}

public class ComposicaoBalanco
{
    public string Titulo { get; set; } = string.Empty;
    public List<FatiaComposicao> Fatias { get; set; } = new();
}

public record FatiaComposicao(string Rotulo, decimal Valor);
