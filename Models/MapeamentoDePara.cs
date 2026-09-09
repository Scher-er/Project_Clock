namespace BalancoPatrimonial.App.Models;

/// <summary>
/// Mapeamento De/Para de nomes de contas.
/// Permite que o sistema aprenda que "Disponibilidades" em um balanço
/// deve ser sempre traduzido para a ContaPadraoId = 1 (Caixa e Equivalentes).
/// Pode ser um mapeamento Global (EmpresaId = null) ou específo de uma empresa.
/// </summary>
public class MapeamentoDePara
{
    public int Id { get; set; }

    /// <summary>Texto original recebido do balanço (ex: "caixa e bancos"). Sempre salvo em minúsculas.</summary>
    public string TextoOriginal { get; set; } = string.Empty;

    public int ContaPadraoId { get; set; }
    public ContaPadrao? ContaPadrao { get; set; }

    /// <summary>Se nulo, o mapeamento é global. Se preenchido, só vale para esta empresa.</summary>
    public int? EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }
}
