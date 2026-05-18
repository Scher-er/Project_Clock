using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Models.Log;

/// <summary>
/// Evento de log do sistema. Persistido no MongoDB (collection "logs")
/// e replicado em arquivo TXT diário (formato: logs/YYYY-MM-DD.txt).
///
/// O Id usa string porque MongoDB usa ObjectId (não int auto-incremento).
/// </summary>
public class LogSistema
{
    /// <summary>ObjectId do MongoDB. Vazio antes de persistir.</summary>
    public string Id { get; set; } = string.Empty;

    public DateTime DataHora { get; set; } = DateTime.Now;

    public TipoEventoLog TipoEvento { get; set; }

    /// <summary>Login do usuário responsável pelo evento (ou "SISTEMA").</summary>
    public string Usuario { get; set; } = "SISTEMA";

    /// <summary>Ação executada (verbo): "AUTENTICOU", "INCLUIU_EMPRESA", "EXPORTOU_PDF"...</summary>
    public string Acao { get; set; } = string.Empty;

    /// <summary>Descrição livre do evento — visível pro usuário final.</summary>
    public string Descricao { get; set; } = string.Empty;

    /// <summary>Entidade afetada (Empresa, Balanco, Usuario...) ou null.</summary>
    public string? Entidade { get; set; }

    /// <summary>ID da entidade afetada (quando aplicável).</summary>
    public string? EntidadeId { get; set; }

    /// <summary>Para erros: stack trace ou detalhe técnico.</summary>
    public string? Detalhes { get; set; }

    /// <summary>IP / dispositivo de origem (opcional).</summary>
    public string? Origem { get; set; }
}
