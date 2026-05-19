namespace BalancoPatrimonial.App.Models;

/// <summary>
/// Encapsula o resultado de uma operação de negócio.
/// Evita uso de exceções pra fluxo de controle e padroniza o retorno
/// entre Controllers e Views (a View sempre sabe checar <see cref="Sucesso"/>).
/// </summary>
public class ResultadoOperacao<T>
{
    public bool Sucesso { get; init; }
    public string Mensagem { get; init; } = string.Empty;
    public T? Dados { get; init; }
    public List<string> Erros { get; init; } = new();

    public static ResultadoOperacao<T> Ok(T? dados = default, string mensagem = "Operação realizada com sucesso")
        => new() { Sucesso = true, Dados = dados, Mensagem = mensagem };

    public static ResultadoOperacao<T> Falha(string mensagem, List<string>? erros = null)
        => new() { Sucesso = false, Mensagem = mensagem, Erros = erros ?? new() };

    public static ResultadoOperacao<T> FalhaExcecao(Exception ex)
        => new()
        {
            Sucesso = false,
            Mensagem = "Erro inesperado: " + ex.Message,
            Erros = new() { ex.ToString() }
        };
}
