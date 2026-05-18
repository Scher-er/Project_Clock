namespace BalancoPatrimonial.App.Models.Enums;

/// <summary>
/// Grupo principal de uma conta no balanço patrimonial.
/// Segue a estrutura padrão CVM/CPC.
/// </summary>
public enum GrupoContaPrincipal
{
    /// <summary>Código CVM: 1.x.x — bens e direitos.</summary>
    Ativo = 1,

    /// <summary>Código CVM: 2.01.x e 2.02.x — obrigações.</summary>
    Passivo = 2,

    /// <summary>Código CVM: 2.03.x — capital próprio.</summary>
    PatrimonioLiquido = 3
}
