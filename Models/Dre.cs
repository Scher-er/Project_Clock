using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Models;

/// <summary>
/// Demonstração do Resultado do Exercício (valores-chave). Espelha um balanço
/// por empresa+ano+tipo e alimenta os indicadores de rentabilidade.
/// </summary>
public class Dre
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int AnoExercicio { get; set; }
    public TipoBalanco TipoBalanco { get; set; } = TipoBalanco.Individual;

    public decimal ReceitaLiquida { get; set; }
    public decimal LucroBruto { get; set; }
    public decimal ResultadoOperacional { get; set; }   // EBIT
    public decimal DespesasFinanceiras { get; set; }     // valor positivo (despesa)
    public decimal LucroLiquido { get; set; }

    public int UsuarioId { get; set; }
    public string? Origem { get; set; }
    public string? HashOrigemPdf { get; set; }
    public DateTime DataPlanilhamento { get; set; } = DateTime.Now;

    /// <summary>Há ao menos um valor preenchido (a IA encontrou DRE no PDF).</summary>
    public bool TemDados =>
        ReceitaLiquida != 0 || LucroBruto != 0 || ResultadoOperacional != 0
        || DespesasFinanceiras != 0 || LucroLiquido != 0;
}
