namespace BalancoPatrimonial.App.Models;

/// <summary>
/// Configuração local da aplicação. Persistida no SQLite (banco local do dispositivo).
/// Estrutura key-value pra flexibilidade — qualquer preferência nova vira uma linha nova.
///
/// Chaves padronizadas (constantes em <see cref="ChavesConfiguracao"/>):
///   - "tema"            → "claro" | "escuro"
///   - "ultimo_usuario"  → login do último usuário autenticado
///   - "ultimo_filtro"   → último filtro aplicado na página Empresas
///   - "ultima_pesquisa" → último termo pesquisado
/// </summary>
public class ConfiguracaoLocal
{
    public int Id { get; set; }
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public DateTime DataAlteracao { get; set; } = DateTime.Now;
}

public static class ChavesConfiguracao
{
    public const string Tema = "tema";
    public const string UltimoUsuario = "ultimo_usuario";
    public const string UltimoFiltro = "ultimo_filtro";
    public const string UltimaPesquisa = "ultima_pesquisa";
    public const string IdiomaInterface = "idioma";
}
