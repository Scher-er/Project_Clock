using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models.Enums;
using BalancoPatrimonial.App.Models.Log;

namespace BalancoPatrimonial.App.DAO.Interfaces;

/// <summary>
/// Acesso aos logs do sistema. Note que o tipo de ID é <c>string</c> (ObjectId do MongoDB).
/// É por isso que <see cref="IDao{T,TId}"/> é genérico em TId.
/// </summary>
public interface ILogDao : IDao<LogSistema, string>
{
    /// <summary>Lista logs de um único dia (00:00 até 23:59:59 do dia informado).</summary>
    Task<IEnumerable<LogSistema>> ListarPorDataAsync(DateTime dia);

    /// <summary>Lista logs num intervalo de datas.</summary>
    Task<IEnumerable<LogSistema>> ListarPorIntervaloAsync(DateTime inicio, DateTime fim);

    /// <summary>Lista logs filtrados por tipo (Login, Erro, Exclusao...).</summary>
    Task<IEnumerable<LogSistema>> ListarPorTipoAsync(TipoEventoLog tipo, DateTime? desde = null);

    /// <summary>Lista logs de um usuário específico.</summary>
    Task<IEnumerable<LogSistema>> ListarPorUsuarioAsync(string usuario, DateTime? desde = null);
}
