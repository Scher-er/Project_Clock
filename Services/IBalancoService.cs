using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Regras de negócio do Balanço Patrimonial:
///   - Validação: Ativo Total = Passivo Total + PL
///   - Cálculo de totalizadores (a partir das contas filhas)
///   - Detecção de duplicidade (mesmo empresa+ano+tipo) e por hash do PDF
/// </summary>
public interface IBalancoService : IService
{
    /// <summary>Persiste um balanço novo (com todas as contas) em uma transação.</summary>
    Task<ResultadoOperacao<int>> SalvarAsync(Balanco balanco, bool substituir = false);

    /// <summary>Indica se já existe um balanço ativo com empresa+ano+tipo.</summary>
    Task<bool> ExisteAsync(int empresaId, int anoExercicio, TipoBalanco tipo);

    /// <summary>Carrega balanço completo com contas e plano de contas.</summary>
    Task<ResultadoOperacao<Balanco>> CarregarAsync(int balancoId);

    /// <summary>Exclui balanço (cascateia conta_balanco automaticamente no MySQL).</summary>
    Task<ResultadoOperacao<bool>> ExcluirAsync(int balancoId);

    /// <summary>Atualiza um balanço existente substituindo suas contas.</summary>
    Task<ResultadoOperacao<bool>> AtualizarComContasAsync(Balanco balanco);

    /// <summary>
    /// Lista todos os balanços de uma empresa (resumo, sem contas).
    /// Útil pra exibir o histórico na tela da empresa.
    /// </summary>
    Task<ResultadoOperacao<IEnumerable<Balanco>>> ListarPorEmpresaAsync(int empresaId);

    /// <summary>
    /// Verifica se Ativo Total = Passivo + PL (margem de erro configurável pra arredondamentos).
    /// Retorna a diferença encontrada.
    /// </summary>
    BalanceamentoResultado ValidarBalanceamento(Balanco balanco, decimal toleranciaReais = 1.00m);
}

/// <summary>Resultado da validação de balanceamento.</summary>
public record BalanceamentoResultado(
    bool Balanceado,
    decimal AtivoTotal,
    decimal PassivoMaisPL,
    decimal Diferenca,
    string Mensagem);
