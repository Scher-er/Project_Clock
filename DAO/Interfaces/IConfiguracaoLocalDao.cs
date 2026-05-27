using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.DAO.Interfaces;

public interface IConfiguracaoLocalDao : IDao<ConfiguracaoLocal>
{
    /// <summary>
    /// Retorna o valor da configuração pela chave, ou null se não existir.
    /// </summary>
    Task<string?> ObterValorAsync(string chave);

    /// <summary>
    /// Define o valor da chave (upsert — insere se não existe, atualiza se existe).
    /// </summary>
    Task DefinirValorAsync(string chave, string valor);
}
