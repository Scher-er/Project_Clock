using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Service que prepara dados de análise financeira a partir dos balanços
/// salvos. Fornece:
///   - Evolução por ano (pra gráfico de linhas)
///   - Composição do último balanço (pra gráfico de pizza)
///   - Indicadores financeiros clássicos (liquidez, endividamento)
/// </summary>
public interface IAnaliseService : IService
{
    /// <summary>Análise completa de uma empresa. Vazio se ela não tem balanços.</summary>
    Task<ResultadoOperacao<AnaliseEmpresa>> AnalisarAsync(int empresaId);
}
