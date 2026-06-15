namespace BalancoPatrimonial.App.Models;

/// <summary>
/// Configurações da integração com Google Gemini API.
///
/// A API key é guardada em <c>SecureStorage</c> do MAUI (criptografado por
/// dispositivo) — não vai pro MySQL nem SQLite. Os demais campos podem ser
/// alterados, com defaults seguros.
/// </summary>
public class AiSettings
{
    /// <summary>API key do Google AI Studio (formato 'AIza...').</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Modelo da família Gemini. Opções (todas no tier gratuito da API):
    ///   - gemini-3.5-flash       (recomendado — qualidade quase-Pro, grátis)
    ///   - gemini-3.1-flash-lite  (mais rápido/leve, limite diário maior)
    ///   - gemini-2.5-pro         (geração anterior, mais lento)
    ///   - gemini-2.5-flash       (geração anterior)
    ///   - gemini-2.5-flash-lite  (geração anterior, leve)
    /// </summary>
    public string Modelo { get; set; } = "gemini-3.5-flash";

    /// <summary>
    /// Endpoint base (sem modelo nem método). A URL final é montada como:
    ///   {Endpoint}/{Modelo}:generateContent?key={ApiKey}
    /// </summary>
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";

    /// <summary>
    /// Tokens máximos da resposta. O Gemini 3.x usa "thinking" implícito que
    /// consome tokens de saída antes da resposta, então damos folga (32k).
    /// </summary>
    public int MaxTokens { get; set; } = 32768;

    /// <summary>Timeout do request (PDFs grandes podem demorar 30-60s).</summary>
    public int TimeoutSegundos { get; set; } = 180;

    /// <summary>True se a API key está preenchida.</summary>
    public bool Configurado => !string.IsNullOrWhiteSpace(ApiKey);
}

/// <summary>Chaves usadas pelo SecureStorage / Preferences pra essa configuração.</summary>
public static class AiSettingsKeys
{
    /// <summary>Chave do SecureStorage onde a API key fica guardada (criptografada).</summary>
    public const string ApiKey = "ai_gemini_api_key";

    /// <summary>Chave do Preferences onde o modelo selecionado fica guardado (não-sensível).</summary>
    public const string Modelo = "ai_gemini_modelo";
}
