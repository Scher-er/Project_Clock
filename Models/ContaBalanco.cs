namespace BalancoPatrimonial.App.Models;

/// <summary>
/// Valor de uma conta padrão dentro de um balanço específico.
/// Ex: "Balanço da Empresa X em 2023 → Conta Caixa = R$ 1.500.000,00"
/// </summary>
public class ContaBalanco
{
    public int Id { get; set; }

    public int BalancoId { get; set; }
    public Balanco? Balanco { get; set; }

    public int ContaPadraoId { get; set; }
    public ContaPadrao? ContaPadrao { get; set; }

    /// <summary>Valor em R$ (sempre na unidade base, não em milhares).</summary>
    public decimal Valor { get; set; }

    /// <summary>
    /// Detalhe textual da conta na DF original. Pode diferir da descrição
    /// padrão (ex: a empresa chamou de "Disponibilidades" e mapeamos pra
    /// "Caixa e Equivalentes de Caixa"). Útil pra auditoria.
    /// </summary>
    public string? DescricaoOriginal { get; set; }
}
