using System.Globalization;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Views.Items;

/// <summary>
/// Wrapper de exibição de um Balanço no card de balanços planilhados
/// da CadastroEmpresaPage. Pré-formata as strings pra binding direto.
/// </summary>
public class BalancoListItem
{
    public int Id { get; init; }
    public int Ano { get; init; }
    public string TipoTexto { get; init; } = string.Empty;
    public string DataPlanilhamento { get; init; } = string.Empty;
    public string Origem { get; init; } = string.Empty;
    public string IconeOrigem { get; init; } = "";
    public Color CorBadgeTipo { get; init; } = Colors.Gray;

    public static BalancoListItem De(Balanco b)
    {
        var (icone, _) = IconeECor(b.Origem);
        return new BalancoListItem
        {
            Id = b.Id,
            Ano = b.AnoExercicio,
            TipoTexto = b.TipoBalanco.ToString(),
            DataPlanilhamento = b.DataPlanilhamento.ToString("dd/MM/yyyy HH:mm", new CultureInfo("pt-BR")),
            Origem = b.Origem ?? "Manual",
            IconeOrigem = icone,
            CorBadgeTipo = b.TipoBalanco == TipoBalanco.Consolidado
                ? Color.FromArgb("#7C3AED")   // roxo
                : Color.FromArgb("#1F3A60")   // azul corporativo
        };
    }

    private static (string icone, Color cor) IconeECor(string? origem) => origem switch
    {
        "PDF"      => ("", Color.FromArgb("#0891B2")),
        "PDF + IA" => ("", Color.FromArgb("#16A34A")),
        _          => ("️", Color.FromArgb("#6B7280"))
    };
}
