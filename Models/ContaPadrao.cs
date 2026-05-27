using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Models;

/// <summary>
/// Conta do plano de contas padrão CVM/CPC. Estrutura HIERÁRQUICA via auto-referência (ContaPaiId).
///
/// Códigos seguem o padrão "1.01.03.02" (até 4 níveis):
///   nível 1: Grupo principal (Ativo / Passivo / PL)
///   nível 2: Subgrupo (Circulante / Não Circulante)
///   nível 3: Conta sintética
///   nível 4: Conta analítica
///
/// Contas totalizadoras (EhTotalizadora=true) NÃO recebem lançamento direto —
/// seu valor é a soma das contas filhas.
/// </summary>
public class ContaPadrao
{
    public int Id { get; set; }

    /// <summary>Código hierárquico ex: "1.01.03.02". Único na base.</summary>
    public string Codigo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public GrupoContaPrincipal GrupoPrincipal { get; set; }

    /// <summary>FK auto-referente — conta pai na hierarquia.</summary>
    public int? ContaPaiId { get; set; }
    public ContaPadrao? ContaPai { get; set; }

    /// <summary>Nível na hierarquia (1, 2, 3, 4). Calculado pelo código.</summary>
    public int Nivel { get; set; }

    /// <summary>
    /// Se true, esta conta totaliza filhas (não recebe valor direto no balanço).
    /// Ex: "1.01 Ativo Circulante" totaliza Caixa + Aplicações + Recebíveis...
    /// </summary>
    public bool EhTotalizadora { get; set; }

    /// <summary>Ordem de apresentação no balanço.</summary>
    public int Ordem { get; set; }

    public bool Ativa { get; set; } = true;

    // Navegação
    public List<ContaPadrao> Filhas { get; set; } = new();
}
