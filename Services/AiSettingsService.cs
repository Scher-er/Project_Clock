using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Persistência das configurações da Gemini.
///
/// **Defensivo:** todas as chamadas a <c>SecureStorage</c> e <c>Preferences</c>
/// estão envoltas em try/catch. Em apps Windows unpackaged (sem identidade
/// MSIX), essas APIs podem lançar exceções em algumas situações. Falhamos
/// graciosamente em vez de derrubar o app inteiro.
/// </summary>
public class AiSettingsService : IAiSettingsService
{
    public string NomeServico => "AiSettingsService";

    public async Task<AiSettings> ObterAsync()
    {
        var settings = new AiSettings();

        try
        {
            var apiKey = await SecureStorage.Default.GetAsync(AiSettingsKeys.ApiKey);
            if (!string.IsNullOrEmpty(apiKey))
                settings.ApiKey = apiKey;
        }
        catch
        {
            // SecureStorage indisponível (keychain ausente, sandbox issue, etc).
            // Settings continua com ApiKey vazia — usuário só não vai conseguir usar IA.
        }

        try
        {
            var modelo = Preferences.Default.Get(AiSettingsKeys.Modelo, string.Empty);
            if (!string.IsNullOrEmpty(modelo))
                settings.Modelo = modelo;
        }
        catch
        {
            // Preferences indisponível — usa o modelo default.
        }

        return settings;
    }

    public async Task SalvarAsync(AiSettings settings)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                await LimparApiKeyAsync();
            }
            else
            {
                await SecureStorage.Default.SetAsync(AiSettingsKeys.ApiKey, settings.ApiKey);
            }
        }
        catch
        {
            // Não conseguiu salvar a key — usuário vai perceber porque o status fica como não-configurado
        }

        try
        {
            Preferences.Default.Set(AiSettingsKeys.Modelo, settings.Modelo);
        }
        catch
        {
            // Preferences indisponível — modelo não persiste entre sessões
        }
    }

    public Task LimparApiKeyAsync()
    {
        try { SecureStorage.Default.Remove(AiSettingsKeys.ApiKey); } catch { }
        return Task.CompletedTask;
    }
}
