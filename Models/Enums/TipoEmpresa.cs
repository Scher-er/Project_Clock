namespace BalancoPatrimonial.App.Models.Enums;

/// <summary>Natureza jurídica simplificada da empresa.</summary>
public enum TipoEmpresa
{
    SaAberta = 1,    // Sociedade Anônima de capital aberto (cotada em bolsa)
    SaFechada = 2,   // Sociedade Anônima de capital fechado
    Ltda = 3,        // Sociedade Limitada
    Eireli = 4,      // Empresa Individual de Responsabilidade Limitada
    Mei = 5,         // Microempreendedor Individual
    Outro = 99
}
