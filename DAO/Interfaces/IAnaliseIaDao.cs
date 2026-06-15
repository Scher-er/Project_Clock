using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.DAO.Interfaces;

/// <summary>
/// Persistência (MongoDB) das respostas brutas da IA. Serve para auditoria e
/// como cache por hash de PDF.
/// </summary>
public interface IAnaliseIaDao
{
    /// <summary>Salva o registro bruto. Nunca lança (falha de Mongo não pode quebrar a análise).</summary>
    Task SalvarAsync(AnaliseIaBruta registro);

    /// <summary>Busca a análise bem-sucedida mais recente para um hash de PDF, ou null.</summary>
    Task<AnaliseIaBruta?> BuscarPorHashAsync(string hashPdf);
}
