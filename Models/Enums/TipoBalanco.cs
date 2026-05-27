namespace BalancoPatrimonial.App.Models.Enums;

/// <summary>
/// Tipo do balanço patrimonial — característica detectada pelo parser de PDF.
/// </summary>
public enum TipoBalanco
{
    /// <summary>Balanço da empresa isoladamente, sem suas controladas.</summary>
    Individual = 1,

    /// <summary>
    /// Balanço consolidado da empresa + controladas (eliminadas as transações intragrupo).
    /// Em geral é o que se usa para análise de crédito de grupos econômicos.
    /// </summary>
    Consolidado = 2
}
