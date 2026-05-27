namespace BalancoPatrimonial.App.Models.Enums;

/// <summary>
/// Categoriza um evento registrado no log do sistema.
/// Usado para filtros e exportação XML organizada.
/// </summary>
public enum TipoEventoLog
{
    Login = 1,
    Logout = 2,
    Inclusao = 3,
    Alteracao = 4,
    Exclusao = 5,
    Importacao = 6,
    Exportacao = 7,
    Erro = 8,
    Aviso = 9,
    Info = 10
}
