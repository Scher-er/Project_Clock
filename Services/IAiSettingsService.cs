using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Persiste configurações da integração com Gemini API.
///
/// **Importante:** a API key fica em <c>SecureStorage</c> (criptografada por
/// dispositivo via Keychain no iOS/macOS, Keystore no Android, DPAPI no Windows).
/// Nunca vai pro MySQL ou SQLite, então não é roubada via dump de banco.
/// </summary>
public interface IAiSettingsService : IService
{
    /// <summary>Carrega configurações atuais. Se nada foi configurado, retorna defaults com ApiKey vazia.</summary>
    Task<AiSettings> ObterAsync();

    /// <summary>Salva configurações (API key vai pro SecureStorage, modelo vai pro Preferences).</summary>
    Task SalvarAsync(AiSettings settings);

    /// <summary>Remove a API key do SecureStorage (logout da integração).</summary>
    Task LimparApiKeyAsync();
}
