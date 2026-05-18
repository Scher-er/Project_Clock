using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Models;

/// <summary>
/// Empresa analisada pelo banco para fins de análise de crédito.
/// É a entidade central do sistema — toda análise gira em torno de uma empresa.
/// </summary>
public class Empresa
{
    public int Id { get; set; }

    /// <summary>CNPJ com 14 dígitos, sem formatação. Único na base.</summary>
    public string Cnpj { get; set; } = string.Empty;

    /// <summary>Razão social oficial.</summary>
    public string RazaoSocial { get; set; } = string.Empty;

    public string? NomeFantasia { get; set; }

    /// <summary>Grupo econômico ao qual a empresa pertence (opcional).</summary>
    public int? GrupoEconomicoId { get; set; }
    public GrupoEconomico? GrupoEconomico { get; set; }

    public TipoEmpresa TipoEmpresa { get; set; } = TipoEmpresa.Ltda;

    /// <summary>
    /// Rating de crédito atribuído pelo banco. Texto livre pra acomodar diferentes
    /// escalas (Moody's, S&amp;P, interna). Ex: "AAA", "BB+", "BR.3".
    /// </summary>
    public string? Rating { get; set; }

    /// <summary>Limite de crédito aprovado pelo banco, em reais.</summary>
    public decimal? LimiteCredito { get; set; }

    /// <summary>UF principal de atuação. Detalhe da cidade no campo abaixo.</summary>
    public string? UfAtuacao { get; set; }

    /// <summary>Cidade/região principal. Texto livre.</summary>
    public string? LocalAtuacao { get; set; }

    public DateTime DataCadastro { get; set; } = DateTime.Now;
    public bool Ativo { get; set; } = true;

    /// <summary>Caminho da foto/logo da empresa (cumpre o requisito 10 do trabalho).</summary>
    public string? CaminhoLogo { get; set; }

    // Navegação
    public List<SetorAtividade> Setores { get; set; } = new();
    public List<Balanco> Balancos { get; set; } = new();
}
