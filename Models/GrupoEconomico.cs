namespace BalancoPatrimonial.App.Models;

/// <summary>
/// Grupo econômico — agrupa empresas relacionadas societariamente
/// (ex: "Grupo Itaú", "Grupo JBS"). Uma empresa pertence a no máximo um grupo.
/// </summary>
public class GrupoEconomico
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime DataCadastro { get; set; } = DateTime.Now;
    public bool Ativo { get; set; } = true;

    /// <summary>Carregado sob demanda pelas DAOs (Fase 3).</summary>
    public List<Empresa> Empresas { get; set; } = new();
}
