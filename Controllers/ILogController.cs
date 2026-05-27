using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models.Enums;
using BalancoPatrimonial.App.Models.Log;

namespace BalancoPatrimonial.App.Controllers;

/// <summary>
/// Controller dos Logs. Não usa <see cref="IController{T}"/> porque logs:
///   - Não têm CRUD (não se edita um log; ele é imutável por design)
///   - Usam string como ID (ObjectId do MongoDB)
///
/// Só herda do marcador base <see cref="IController"/>.
/// </summary>
public interface ILogController : IController
{
    Task<IEnumerable<LogSistema>> ListarPorDataAsync(DateTime dia);
    Task RegistrarAsync(TipoEventoLog tipo, string acao, string descricao);

    /// <summary>
    /// Exporta logs de um dia para XML e retorna o caminho do arquivo criado.
    /// O XML fica em FileSystem.AppDataDirectory/exportacoes/logs_<data>.xml
    /// </summary>
    Task<string> ExportarXmlAsync(DateTime dia);
}
