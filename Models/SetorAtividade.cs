namespace BalancoPatrimonial.App.Models;

/// <summary>
/// Setor de atividade econômica (referência ao CNAE simplificado).
/// Uma empresa pode atuar em vários setores — relação N:N via tabela empresa_setor.
/// </summary>
public class SetorAtividade
{
    public int Id { get; set; }

    /// <summary>Código CNAE (ou código interno simplificado).</summary>
    public string Codigo { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
}
