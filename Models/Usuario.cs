namespace BalancoPatrimonial.App.Models;

/// <summary>
/// Usuário do sistema (analista de crédito, gerente, administrador).
/// Será persistido no MySQL na Fase 3.
/// </summary>
public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash da senha (nunca armazenar texto plano). BCrypt na Fase 3.</summary>
    public string SenhaHash { get; set; } = string.Empty;

    /// <summary>"Administrador", "Analista", "Gerente"...</summary>
    public string Perfil { get; set; } = "Analista";

    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public DateTime? UltimoLogin { get; set; }
}
