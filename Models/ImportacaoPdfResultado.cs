using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Models;

/// <summary>
/// Resultado da análise/parse de um PDF de balanço.
///
/// O parser **não persiste** nada. Apenas extrai informação e devolve pra UI
/// preencher os campos do planilhamento. O usuário sempre revisa antes de salvar.
/// </summary>
public class ImportacaoPdfResultado
{
    /// <summary>Hash SHA-256 do PDF — usado pra detectar reimportação.</summary>
    public string HashPdf { get; set; } = string.Empty;

    /// <summary>Nome do arquivo de origem (sem caminho).</summary>
    public string NomeArquivo { get; set; } = string.Empty;

    /// <summary>Tamanho do PDF em bytes.</summary>
    public long TamanhoBytes { get; set; }

    /// <summary>Quantidade de páginas do PDF.</summary>
    public int QtdPaginas { get; set; }

    /// <summary>
    /// Multiplicador detectado: 1 (sem multiplicador), 1000 (R$ mil), 1000000 (R$ milhões).
    /// Default: 1.
    /// </summary>
    public int MultiplicadorDetectado { get; set; } = 1;

    /// <summary>Moeda detectada (BRL, USD). Default: BRL.</summary>
    public string MoedaDetectada { get; set; } = "BRL";

    /// <summary>Tipo do balanço detectado (Consolidado/Individual).</summary>
    public TipoBalanco TipoDetectado { get; set; } = TipoBalanco.Individual;

    /// <summary>Confiança da detecção do tipo (0.0 a 1.0).</summary>
    public double ConfiancaTipo { get; set; }

    /// <summary>Ano detectado pelo texto (4 dígitos, geralmente do cabeçalho).</summary>
    public int? AnoDetectado { get; set; }

    /// <summary>Data de referência detectada (ex: 31/12/2023).</summary>
    public DateTime? DataReferenciaDetectada { get; set; }

    /// <summary>
    /// Contas mapeadas (chave = id da ContaPadrao, valor = R$ encontrado).
    /// O parser já aplica o multiplicador antes de devolver — os valores aqui são em reais.
    /// </summary>
    public Dictionary<int, decimal> ContasMapeadas { get; set; } = new();

    /// <summary>
    /// Linhas do PDF que pareciam ter "descrição + valor" mas não casaram com nenhuma
    /// conta padrão. Mostradas pro usuário pra revisão manual.
    /// </summary>
    public List<LinhaNaoMapeada> LinhasNaoMapeadas { get; set; } = new();

    /// <summary>Quantidade total de linhas examinadas (pra estatística no toast).</summary>
    public int LinhasExaminadas { get; set; }

    /// <summary>Mensagens informativas / avisos pro usuário.</summary>
    public List<string> Avisos { get; set; } = new();
}

/// <summary>Uma linha "descrição + valor" extraída do PDF que não bateu com nenhuma conta padrão.</summary>
public record LinhaNaoMapeada(string Descricao, decimal Valor);
