namespace BalancoPatrimonial.App.Interfaces;

/// <summary>
/// Contrato base que todo Service (camada de regras de negócio) deve implementar.
///
/// Services concentram regras de negócio e orquestram chamadas a múltiplas DAOs.
/// Diferentes Services podem ter operações muito diferentes (parsing de PDF,
/// autenticação, geração de Excel...), então a interface base é propositalmente
/// enxuta — cada Service específico expõe sua própria sub-interface.
/// </summary>
public interface IService
{
    /// <summary>Nome do serviço para fins de logging e diagnóstico.</summary>
    string NomeServico { get; }
}
