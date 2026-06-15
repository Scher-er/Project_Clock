using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Views.Items;

/// <summary>
/// Representa um período de planilhamento (mês+ano OU só ano).
/// Cada coluna da tabela de planilhamento é um destes.
/// </summary>
public class PeriodoPlanilhado
{
    private static readonly string[] _abrevMes =
        { "JAN", "FEV", "MAR", "ABR", "MAI", "JUN", "JUL", "AGO", "SET", "OUT", "NOV", "DEZ" };

    /// <summary>Mês (1-12). Null = balanço anual (referência em 31/12).</summary>
    public int? Mes { get; init; }
    public int Ano { get; init; }
    public TipoBalanco Tipo { get; init; }

    /// <summary>Label curto pra header da coluna. Ex: "MAR/2024" ou "2024".</summary>
    public string Label => Mes.HasValue
        ? $"{_abrevMes[Mes.Value - 1]}/{Ano}"
        : Ano.ToString();

    /// <summary>Sufixo do tipo. "I" pra Individual, "C" pra Consolidado.</summary>
    public string TipoLabel => Tipo == TipoBalanco.Individual ? "I" : "C";

    /// <summary>Label completo incluindo tipo. Ex: "MAR/2024 (I)".</summary>
    public string LabelCompleto => $"{Label} ({TipoLabel})";

    /// <summary>
    /// Data de referência no formato Balanco:
    /// - Mensal: último dia do mês
    /// - Anual: 31/12 do ano
    /// </summary>
    public DateTime DataReferencia => Mes.HasValue
        ? new DateTime(Ano, Mes.Value, DateTime.DaysInMonth(Ano, Mes.Value))
        : new DateTime(Ano, 12, 31);

    public override bool Equals(object? obj)
        => obj is PeriodoPlanilhado p && p.Mes == Mes && p.Ano == Ano && p.Tipo == Tipo;

    public override int GetHashCode() => HashCode.Combine(Mes, Ano, Tipo);
}
