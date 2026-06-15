using BalancoPatrimonial.App.Models.Enums;
using BalancoPatrimonial.App.Models.Log;

namespace BalancoPatrimonial.App.Views.Items;

/// <summary>
/// Wrapper de exibição de LogSistema. Pré-formata hora, cor do badge,
/// emoji e detalhes pra não precisar de IValueConverter no XAML.
/// </summary>
public class LogListItem
{
    public string Hora { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public string Usuario { get; init; } = string.Empty;
    public string Acao { get; init; } = string.Empty;
    public string Descricao { get; init; } = string.Empty;
    public Color CorTipo { get; init; } = Colors.Gray;
    public string Emoji { get; init; } = "•";
    public string? Detalhes { get; init; }
    public bool TemDetalhes => !string.IsNullOrEmpty(Detalhes);

    public static LogListItem De(LogSistema log)
    {
        var (cor, emoji) = CorEEmoji(log.TipoEvento);
        return new LogListItem
        {
            Hora = log.DataHora.ToString("HH:mm:ss"),
            Tipo = log.TipoEvento.ToString().ToUpperInvariant(),
            Usuario = log.Usuario,
            Acao = log.Acao,
            Descricao = log.Descricao,
            CorTipo = cor,
            Emoji = emoji,
            Detalhes = log.Detalhes
        };
    }

    private static (Color, string) CorEEmoji(TipoEventoLog tipo) => tipo switch
    {
        TipoEventoLog.Login        => (Color.FromArgb("#16A34A"), ""),  // verde
        TipoEventoLog.Logout       => (Color.FromArgb("#6B7280"), ""),  // cinza
        TipoEventoLog.Inclusao     => (Color.FromArgb("#1F3A60"), ""),  // azul corporativo
        TipoEventoLog.Alteracao    => (Color.FromArgb("#0891B2"), "️"), // ciano
        TipoEventoLog.Exclusao     => (Color.FromArgb("#DC2626"), "️"), // vermelho
        TipoEventoLog.Erro         => (Color.FromArgb("#B91C1C"), ""),  // vermelho escuro
        TipoEventoLog.Aviso        => (Color.FromArgb("#F59E0B"), "️"), // amarelo
        TipoEventoLog.Info         => (Color.FromArgb("#3B82F6"), "ℹ️"), // azul claro
        TipoEventoLog.Exportacao   => (Color.FromArgb("#7C3AED"), ""),  // roxo
        TipoEventoLog.Importacao   => (Color.FromArgb("#7C3AED"), ""),  // roxo
        _                          => (Color.FromArgb("#6B7280"), "•")
    };
}
