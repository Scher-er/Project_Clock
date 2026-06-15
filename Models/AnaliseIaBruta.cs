namespace BalancoPatrimonial.App.Models;

/// <summary>
/// Registro bruto de uma análise de PDF feita pela IA, guardado no MongoDB
/// para auditoria e para servir de cache (reaproveitar quando o mesmo PDF
/// voltar, evitando gastar requisições da API).
/// </summary>
public class AnaliseIaBruta
{
    public string Id { get; set; } = string.Empty;

    /// <summary>Hash SHA-256 do PDF (mesma chave usada no balanço).</summary>
    public string HashPdf { get; set; } = string.Empty;

    public string NomeArquivo { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;

    /// <summary>JSON cru retornado pela API (resposta completa).</summary>
    public string JsonResposta { get; set; } = string.Empty;

    public bool Sucesso { get; set; }
    public DateTime DataHora { get; set; } = DateTime.Now;
}
