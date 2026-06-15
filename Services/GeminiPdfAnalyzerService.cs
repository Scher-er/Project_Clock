using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Análise de PDF via Google Gemini API.
///
/// Endpoint:
///   POST https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={KEY}
///
/// Estratégia:
///   1. PDF inteiro em base64 → part "inline_data" no contents[0].parts
///   2. Usa function calling com modo "ANY" forçando a função 'registrar_balanco_extraido'
///      — garante saída JSON estruturada, sem parse de texto livre
///   3. Plano de contas vai no prompt como referência fechada
///      (Gemini DEVE retornar códigos existentes; matching é exato por código)
///
/// **Cobertura:** funciona melhor que o parser heurístico em PDFs:
///   - de empresas privadas (não-CVM)
///   - escaneados/com OCR ruim (Gemini vê imagens)
///   - com layouts incomuns (Gemini entende contexto, não só posição)
///
/// **Custo:** Gemini 2.5 Flash é gratuito até 15 req/min e 1500/dia em free tier.
/// Em conta paga: cerca de US$ 0,000075 por 1k tokens de input + US$ 0,0003 output.
/// Cada PDF de balanço consome ~5-15k tokens.
/// </summary>
public class GeminiPdfAnalyzerService : IPdfAiAnalyzerService
{
    private readonly IAiSettingsService _settingsService;
    private readonly IListagensService _listagens;
    private readonly ILogService _log;
    private readonly IAnaliseIaDao _analiseIaDao;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public string NomeServico => "GeminiPdfAnalyzerService";

    public GeminiPdfAnalyzerService(
        IAiSettingsService settingsService,
        IListagensService listagens,
        ILogService log,
        IAnaliseIaDao analiseIaDao)
    {
        _settingsService = settingsService;
        _listagens = listagens;
        _log = log;
        _analiseIaDao = analiseIaDao;
    }

    public async Task<bool> ConfiguradoAsync()
    {
        var s = await _settingsService.ObterAsync();
        return s.Configurado;
    }

    // ═════════════ TESTE DE CONEXÃO ═════════════

    public async Task<ResultadoOperacao<string>> GerarParecerAsync(AnaliseEmpresa analise)
    {
        var settings = await _settingsService.ObterAsync();
        if (!settings.Configurado)
            return ResultadoOperacao<string>.Falha("API key não configurada.");

        var ind = analise.Indicadores.LastOrDefault();
        if (ind is null)
            return ResultadoOperacao<string>.Falha("Empresa sem indicadores para basear o parecer.");

        try
        {
            var prompt = MontarPromptParecer(analise, ind);

            using var http = CriarHttpClient(settings);
            var payload = new
            {
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text =
                            "Você é um analista de crédito sênior de um banco. Redija pareceres técnicos, " +
                            "objetivos e em português do Brasil, baseados ESTRITAMENTE nos números fornecidos. " +
                            "Não invente dados. Seja conciso (2 a 4 parágrafos), cite os indicadores relevantes " +
                            "e termine com uma recomendação clara sobre concessão de crédito." }
                    }
                },
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new { temperature = 0.3, maxOutputTokens = 800 }
            };

            var url = MontarUrl(settings);
            var json = JsonSerializer.Serialize(payload, _jsonOpts);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await http.PostAsync(url, content);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return ResultadoOperacao<string>.Falha($"API retornou {(int)resp.StatusCode}. {ExtrairErroDaApi(body)}");

            var texto = ExtrairTextoResposta(body);
            if (string.IsNullOrWhiteSpace(texto))
                return ResultadoOperacao<string>.Falha("A IA não retornou texto de parecer.");

            await _log.RegistrarAsync(TipoEventoLog.Info, "PARECER_IA",
                $"Parecer gerado por IA para empresa '{analise.Empresa.RazaoSocial}'.");

            return ResultadoOperacao<string>.Ok(texto.Trim(), "Parecer gerado pela IA.");
        }
        catch (TaskCanceledException)
        {
            return ResultadoOperacao<string>.Falha("Timeout ao gerar o parecer.");
        }
        catch (Exception ex)
        {
            return ResultadoOperacao<string>.FalhaExcecao(ex);
        }
    }

    private static string MontarPromptParecer(AnaliseEmpresa a, IndicadoresBalanco ind)
    {
        var sb = new StringBuilder();
        string F(decimal? v) => v is decimal x ? x.ToString("N2", new CultureInfo("pt-BR")) : "n/d";
        string P(decimal? v) => v is decimal x ? (x * 100m).ToString("N1", new CultureInfo("pt-BR")) + "%" : "n/d";

        sb.AppendLine($"Empresa: {a.Empresa.RazaoSocial}");
        sb.AppendLine($"Ano-base: {ind.Ano}");
        if (a.Risco is AvaliacaoRisco r)
            sb.AppendLine($"Classificação interna: classe {r.Classe} ({r.NivelRisco}), score {r.Score}/100.");
        sb.AppendLine();
        sb.AppendLine("INDICADORES:");
        sb.AppendLine($"- Liquidez Corrente: {F(ind.LiquidezCorrente)}");
        sb.AppendLine($"- Liquidez Seca: {F(ind.LiquidezSeca)}");
        sb.AppendLine($"- Liquidez Geral: {F(ind.LiquidezGeral)}");
        sb.AppendLine($"- Capital Circulante Líquido: R$ {F(ind.CapitalCirculanteLiquido)}");
        sb.AppendLine($"- Endividamento Geral: {P(ind.EndividamentoGeral)}");
        sb.AppendLine($"- Composição do Endividamento: {P(ind.ComposicaoEndividamento)}");
        sb.AppendLine($"- Imobilização do PL: {P(ind.ImobilizacaoPL)}");
        if (ind.TemDre)
        {
            sb.AppendLine("RENTABILIDADE (DRE):");
            sb.AppendLine($"- Margem Líquida: {P(ind.MargemLiquida)}");
            sb.AppendLine($"- ROE: {P(ind.RetornoSobrePL)}");
            sb.AppendLine($"- ROA: {P(ind.RetornoSobreAtivo)}");
            sb.AppendLine($"- Cobertura de Juros: {F(ind.CoberturaJuros)}");
        }
        if (a.Evolucao.Count >= 2)
            sb.AppendLine($"\nObs: há {a.Evolucao.Count} anos de histórico disponíveis para análise de tendência.");
        sb.AppendLine("\nRedija o parecer de crédito.");
        return sb.ToString();
    }

    private static string ExtrairTextoResposta(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var parts = doc.RootElement.GetProperty("candidates")[0]
                .GetProperty("content").GetProperty("parts");
            var sb = new StringBuilder();
            foreach (var part in parts.EnumerateArray())
                if (part.TryGetProperty("text", out var t))
                    sb.Append(t.GetString());
            return sb.ToString();
        }
        catch { return string.Empty; }
    }

    public async Task<ResultadoOperacao<string>> TestarConexaoAsync()
    {
        var settings = await _settingsService.ObterAsync();
        if (!settings.Configurado)
            return ResultadoOperacao<string>.Falha("API key não configurada.");

        try
        {
            using var http = CriarHttpClient(settings);

            // Mensagem mínima: 1-2 tokens de saída só pra validar
            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = "Diga apenas: OK" }
                        }
                    }
                },
                generationConfig = new { maxOutputTokens = 16 }
            };

            var url = MontarUrl(settings);
            var json = JsonSerializer.Serialize(payload, _jsonOpts);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await http.PostAsync(url, content);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                return ResultadoOperacao<string>.Falha(
                    $"API retornou {(int)resp.StatusCode}. {ExtrairErroDaApi(body)}");
            }

            return ResultadoOperacao<string>.Ok(
                $"Conexão OK com modelo '{settings.Modelo}'.",
                "Gemini configurado corretamente.");
        }
        catch (TaskCanceledException)
        {
            return ResultadoOperacao<string>.Falha("Timeout na conexão com a API.");
        }
        catch (Exception ex)
        {
            return ResultadoOperacao<string>.FalhaExcecao(ex);
        }
    }

    // ═════════════ ANÁLISE PRINCIPAL ═════════════

    public async Task<ResultadoOperacao<ImportacaoPdfResultado>> AnalisarAsync(
        Stream pdfStream, string nomeArquivo)
    {
        var settings = await _settingsService.ObterAsync();
        if (!settings.Configurado)
            return ResultadoOperacao<ImportacaoPdfResultado>.Falha(
                "Gemini não está configurado. Defina a API key na Área de Testes.");

        try
        {
            // 1) Lê todo o PDF em memória + hash + base64
            pdfStream.Position = 0;
            using var mem = new MemoryStream();
            await pdfStream.CopyToAsync(mem);
            var bytes = mem.ToArray();

            var hash = CalcularHash(bytes);
            var base64 = Convert.ToBase64String(bytes);

            // 2) Carrega plano de contas como referência fechada
            var contas = (await _listagens.ListarContasPadraoAsync()).ToList();
            var contasAnaliticas = contas.Where(c => !c.EhTotalizadora).ToList();

            if (contasAnaliticas.Count == 0)
            {
                return ResultadoOperacao<ImportacaoPdfResultado>.Falha(
                    "Plano de contas vazio — execute os seeds antes de importar.");
            }

            // 3) Constrói payload
            var prompt = ConstruirPrompt(contasAnaliticas);
            var schemaFunc = ConstruirSchemaFuncao();

            var payload = new
            {
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = ConstruirPromptSistema() }
                    }
                },
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "application/pdf",
                                    data = base64
                                }
                            },
                            new { text = prompt }
                        }
                    }
                },
                tools = new[]
                {
                    new
                    {
                        function_declarations = new[]
                        {
                            new
                            {
                                name = "registrar_balanco_extraido",
                                description = "Registra os dados extraídos do balanço patrimonial PDF " +
                                              "no formato esperado pelo sistema de análise de crédito.",
                                parameters = schemaFunc
                            }
                        }
                    }
                },
                tool_config = new
                {
                    function_calling_config = new
                    {
                        mode = "ANY",
                        allowed_function_names = new[] { "registrar_balanco_extraido" }
                    }
                },
                generationConfig = new
                {
                    maxOutputTokens = settings.MaxTokens,
                    temperature = 0.0
                }
            };

            // 4) Request
            using var http = CriarHttpClient(settings);
            var url = MontarUrl(settings);
            var jsonPayload = JsonSerializer.Serialize(payload, _jsonOpts);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var resp = await http.PostAsync(url, content);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                await _log.RegistrarAsync(
                    TipoEventoLog.Erro,
                    "GEMINI_API_ERRO",
                    $"Gemini API retornou {(int)resp.StatusCode} ao analisar '{nomeArquivo}'.",
                    detalhes: body);
                return ResultadoOperacao<ImportacaoPdfResultado>.Falha(
                    $"Gemini API retornou {(int)resp.StatusCode}. {ExtrairErroDaApi(body)}");
            }

            // 5) Parse response
            var resultado = ConverterRespostaEmResultado(body, hash, nomeArquivo, bytes.Length, contasAnaliticas);

            await _log.RegistrarAsync(
                TipoEventoLog.Importacao,
                "ANALISOU_PDF_GEMINI",
                $"PDF '{nomeArquivo}' analisado via Gemini ({settings.Modelo}): " +
                $"{resultado.ContasMapeadas.Count} contas mapeadas, " +
                $"tipo={resultado.TipoDetectado}, multiplicador={resultado.MultiplicadorDetectado}.");

            return ResultadoOperacao<ImportacaoPdfResultado>.Ok(resultado);
        }
        catch (TaskCanceledException)
        {
            return ResultadoOperacao<ImportacaoPdfResultado>.Falha(
                "Timeout na análise — o PDF pode estar muito grande ou a rede está lenta. Tente novamente.");
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("GEMINI_PDF_ANALISE", ex);
            return ResultadoOperacao<ImportacaoPdfResultado>.FalhaExcecao(ex);
        }
    }

    // ═════════════ ANÁLISE AUTOMÁTICA COMPLETA ═════════════

    public async Task<ResultadoOperacao<AnaliseAutomaticaResultado>> AnalisarAutomaticoAsync(
        Stream pdfStream, string nomeArquivo)
    {
        var settings = await _settingsService.ObterAsync();
        if (!settings.Configurado)
            return ResultadoOperacao<AnaliseAutomaticaResultado>.Falha(
                "Gemini não está configurado. Defina a API key na Área de Testes.");

        try
        {
            pdfStream.Position = 0;
            using var mem = new MemoryStream();
            await pdfStream.CopyToAsync(mem);
            var bytes = mem.ToArray();
            var hash = CalcularHash(bytes);
            var base64 = Convert.ToBase64String(bytes);

            var contas = (await _listagens.ListarContasPadraoAsync()).ToList();
            var contasAnaliticas = contas.Where(c => !c.EhTotalizadora).ToList();
            if (contasAnaliticas.Count == 0)
            {
                return ResultadoOperacao<AnaliseAutomaticaResultado>.Falha(
                    "Plano de contas vazio — execute os seeds antes de importar.");
            }

            // ─── CACHE por hash: reaproveita análise anterior do mesmo PDF ───
            // Evita gastar requisição da API (importante no tier gratuito).
            string body = string.Empty;
            bool usouCache = false;
            var emCache = await _analiseIaDao.BuscarPorHashAsync(hash);
            if (emCache is not null && !string.IsNullOrWhiteSpace(emCache.JsonResposta))
            {
                body = emCache.JsonResposta;
                usouCache = true;
                await _log.RegistrarAsync(TipoEventoLog.Info, "GEMINI_CACHE_HIT",
                    $"Análise de '{nomeArquivo}' reaproveitada do cache local (sem nova requisição à API).");
            }

            if (!usouCache)
            {
            var prompt = ConstruirPromptAutomatico(contasAnaliticas);

            var payload = new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = ConstruirPromptSistemaAutomatico() } }
                },
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { inline_data = new { mime_type = "application/pdf", data = base64 } },
                            new { text = prompt }
                        }
                    }
                },
                tools = new[]
                {
                    new
                    {
                        function_declarations = new[]
                        {
                            new
                            {
                                name = "registrar_analise_completa",
                                description = "Registra os dados completos extraídos do PDF: empresa e todos os períodos.",
                                parameters = ConstruirSchemaAutomatico()
                            }
                        }
                    }
                },
                tool_config = new
                {
                    function_calling_config = new
                    {
                        mode = "ANY",
                        allowed_function_names = new[] { "registrar_analise_completa" }
                    }
                },
                generationConfig = new { maxOutputTokens = settings.MaxTokens, temperature = 0.0 }
            };

            using var http = CriarHttpClient(settings);
            var url = MontarUrl(settings);
            var jsonPayload = JsonSerializer.Serialize(payload, _jsonOpts);

            // Retry automático com backoff para erros transitórios (503 sobrecarga,
            // 429 limite de taxa, 500). Tenta até 3 vezes esperando 2s, 5s.
            int statusCode = 0;
            int[] esperasSegundos = { 2, 5 };
            for (int tentativa = 0; tentativa <= esperasSegundos.Length; tentativa++)
            {
                using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                using var resp = await http.PostAsync(url, content);
                body = await resp.Content.ReadAsStringAsync();
                statusCode = (int)resp.StatusCode;

                if (resp.IsSuccessStatusCode) break;

                bool transitorio = statusCode is 503 or 429 or 500;
                if (transitorio && tentativa < esperasSegundos.Length)
                {
                    await _log.RegistrarAsync(TipoEventoLog.Aviso, "GEMINI_API_RETRY",
                        $"Gemini retornou {statusCode} ('{nomeArquivo}'). Tentando novamente em {esperasSegundos[tentativa]}s...");
                    await Task.Delay(TimeSpan.FromSeconds(esperasSegundos[tentativa]));
                    continue;
                }

                // Falha definitiva
                await _log.RegistrarAsync(TipoEventoLog.Erro, "GEMINI_API_ERRO",
                    $"Gemini API retornou {statusCode} ao analisar '{nomeArquivo}'.", detalhes: body);
                var msg = statusCode is 503 or 429
                    ? $"O serviço do Gemini está sobrecarregado (erro {statusCode}). Aguarde alguns segundos e use 'Tentar novamente'."
                    : $"Gemini API retornou {statusCode}. {ExtrairErroDaApi(body)}";
                return ResultadoOperacao<AnaliseAutomaticaResultado>.Falha(msg);
            }

            // Guarda a resposta bruta no MongoDB (auditoria + cache por hash)
            await _analiseIaDao.SalvarAsync(new AnaliseIaBruta
            {
                HashPdf = hash,
                NomeArquivo = nomeArquivo,
                Modelo = settings.Modelo,
                JsonResposta = body,
                Sucesso = true
            });
            } // fim do if (!usouCache)

            var (resultado, pendencias) = ConverterRespostaAutomatica(body, hash, nomeArquivo, contasAnaliticas);

            if (usouCache)
                resultado.Avisos.Insert(0, "Análise reaproveitada de uma importação anterior do mesmo PDF (cache) — nenhuma requisição foi gasta.");

            // ─── PODER DO GEMINI: criar contas novas que ele sugeriu ───
            // Quando a IA não acha equivalente, ela indica o subgrupo destino.
            // Criamos a conta (com dedup e limite de segurança) e mapeamos o valor.
            if (pendencias.Count > 0)
            {
                var criadasPorChave = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                int novasCriadas = 0;
                const int LIMITE_NOVAS = 30;

                foreach (var pend in pendencias)
                {
                    var chave = $"{pend.SubgrupoCodigo}|{pend.Descricao.ToLowerInvariant()}";

                    if (!criadasPorChave.TryGetValue(chave, out var contaId))
                    {
                        // Subgrupo pai precisa existir e ser totalizadora
                        var pai = contas.FirstOrDefault(c =>
                            c.Codigo == pend.SubgrupoCodigo && c.EhTotalizadora);
                        if (pai is null) continue;

                        // Já existe conta com essa descrição no subgrupo? reaproveita
                        var existente = contas.FirstOrDefault(c =>
                            c.ContaPaiId == pai.Id &&
                            string.Equals(c.Descricao, pend.Descricao, StringComparison.OrdinalIgnoreCase));

                        if (existente is not null)
                        {
                            contaId = existente.Id;
                        }
                        else
                        {
                            if (novasCriadas >= LIMITE_NOVAS) continue;
                            var rNova = await _listagens.CriarContaAnaliticaAsync(pai.Id, pend.Descricao);
                            if (!rNova.Sucesso || rNova.Dados is null) continue;
                            contaId = rNova.Dados.Id;
                            contas.Add(rNova.Dados); // mantém cache local coerente
                            novasCriadas++;
                        }
                        criadasPorChave[chave] = contaId;
                    }

                    if (pend.PeriodoIndex >= 0 && pend.PeriodoIndex < resultado.Periodos.Count)
                        resultado.Periodos[pend.PeriodoIndex].ContasMapeadas[contaId] = pend.Valor;
                }

                if (novasCriadas > 0)
                    resultado.Avisos.Add($"{novasCriadas} conta(s) nova(s) criada(s) automaticamente para contas sem equivalente no plano.");
            }

            // ─── RECONCILIAÇÃO DETERMINÍSTICA (planilhamento automático) ───
            // Para cada subgrupo (1.01, 1.02, 2.01, 2.02, 2.03), comparamos a soma
            // das contas mapeadas com o SUBTOTAL IMPRESSO lido do PDF. Se houver
            // diferença, lançamos o que falta numa conta "Ajuste automático" daquele
            // subgrupo — garantindo que cada grupo (e o balanço inteiro) FECHE com o
            // PDF, mesmo que o modelo gratuito esqueça ou erre alguma conta.
            var codigoPorId = contas.ToDictionary(c => c.Id, c => c.Codigo);
            var subgrupos = new[] { "1.01", "1.02", "2.01", "2.02", "2.03" };
            int ajustes = 0;

            foreach (var pd in resultado.Periodos)
            {
                foreach (var codSub in subgrupos)
                {
                    if (!pd.SubtotaisImpressos.TryGetValue(codSub, out var alvo))
                    {
                        // Sem subtotal impresso: registra a comparação só com o extraído
                        decimal somaSemAlvo = 0;
                        foreach (var (contaId, valor) in pd.ContasMapeadas)
                            if (codigoPorId.TryGetValue(contaId, out var cod) &&
                                cod.StartsWith(codSub + ".", StringComparison.Ordinal))
                                somaSemAlvo += valor;
                        pd.Comparacoes.Add(new ComparacaoSubgrupo(codSub, RotuloSubgrupo(codSub), somaSemAlvo, null, 0));
                        continue;
                    }

                    decimal soma = 0;
                    foreach (var (contaId, valor) in pd.ContasMapeadas)
                        if (codigoPorId.TryGetValue(contaId, out var cod) &&
                            cod.StartsWith(codSub + ".", StringComparison.Ordinal))
                            soma += valor;

                    var dif = alvo - soma;
                    decimal ajusteAplicado = 0;

                    // tolerância de R$ 1 ou 0,1% do subtotal (o que for maior)
                    if (Math.Abs(dif) > Math.Max(1m, Math.Abs(alvo) * 0.001m))
                    {
                        var contaAjuste = await ObterOuCriarContaAjusteAsync(codSub, contas);
                        if (contaAjuste is not null)
                        {
                            pd.ContasMapeadas.TryGetValue(contaAjuste.Id, out var atual);
                            pd.ContasMapeadas[contaAjuste.Id] = atual + dif;
                            codigoPorId.TryAdd(contaAjuste.Id, contaAjuste.Codigo);
                            ajusteAplicado = dif;
                            ajustes++;
                        }
                    }

                    // Comparação final: extraído já considera o ajuste aplicado
                    pd.Comparacoes.Add(new ComparacaoSubgrupo(
                        codSub, RotuloSubgrupo(codSub), soma + ajusteAplicado, alvo, ajusteAplicado));
                }
            }

            if (ajustes > 0)
                resultado.Avisos.Add(
                    "Alguns grupos não fecharam exatamente com o que a IA extraiu; a diferença foi " +
                    "lançada em contas 'Ajuste automático (revisar)' pra o balanço bater com o PDF. " +
                    "Você pode revisar e redistribuir esses valores na tela de detalhe do balanço.");

            // Conferência final do balanço inteiro (após reconciliação) — só informativo
            foreach (var pd in resultado.Periodos)
            {
                if (pd.AtivoTotalImpresso is not decimal at || pd.PassivoPlTotalImpresso is not decimal pt) continue;
                decimal sa = 0, sp = 0;
                foreach (var (contaId, valor) in pd.ContasMapeadas)
                {
                    if (!codigoPorId.TryGetValue(contaId, out var cod)) continue;
                    if (cod.StartsWith("1", StringComparison.Ordinal)) sa += valor;
                    else if (cod.StartsWith("2", StringComparison.Ordinal)) sp += valor;
                }
                if (Math.Abs(sa - at) > Math.Max(1m, Math.Abs(at) * 0.01m) ||
                    Math.Abs(sp - pt) > Math.Max(1m, Math.Abs(pt) * 0.01m))
                    resultado.Avisos.Add(
                        $"{pd.Label}: não foi possível fechar 100% automaticamente (o PDF pode não ter " +
                        $"trazido os subtotais de grupo). Confira os valores na tela de detalhe.");
            }

            await _log.RegistrarAsync(TipoEventoLog.Importacao, "ANALISOU_PDF_GEMINI_AUTO",
                $"PDF '{nomeArquivo}' analisado (auto) via Gemini ({settings.Modelo}): " +
                $"empresa='{resultado.RazaoSocial}', {resultado.Periodos.Count} período(s), " +
                $"{resultado.TotalContas} contas totais.");

            return ResultadoOperacao<AnaliseAutomaticaResultado>.Ok(resultado);
        }
        catch (TaskCanceledException)
        {
            return ResultadoOperacao<AnaliseAutomaticaResultado>.Falha(
                "Timeout na análise — o PDF pode estar muito grande ou a rede lenta. Tente novamente.");
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("GEMINI_PDF_AUTO", ex);
            return ResultadoOperacao<AnaliseAutomaticaResultado>.FalhaExcecao(ex);
        }
    }

    // ═════════════ HTTP / URL ═════════════

    private static string MontarUrl(AiSettings settings)
        => $"{settings.Endpoint.TrimEnd('/')}/{settings.Modelo}:generateContent?key={Uri.EscapeDataString(settings.ApiKey)}";

    private static HttpClient CriarHttpClient(AiSettings settings)
        => new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(settings.TimeoutSegundos)
        };

    // ═════════════ PROMPTS ═════════════

    private static string ConstruirPromptSistema() => """
        Você é um analista contábil sênior especializado em balanços patrimoniais brasileiros.
        Trabalha para um banco que avalia crédito de empresas a partir das demonstrações financeiras.

        Sua tarefa é extrair dados estruturados de balanços em PDF, independentemente do formato
        de origem (CVM, B3, Receita Federal, contador local, escaneado, formato livre).

        Você SEMPRE usa a função 'registrar_balanco_extraido' para retornar os dados.
        Você NUNCA escreve texto livre na resposta — apenas a função.
        """;

    private string ConstruirPrompt(List<ContaPadrao> contasAnaliticas)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analise o balanço patrimonial em anexo e extraia os dados conforme as regras abaixo.");
        sb.AppendLine();
        sb.AppendLine("## PLANO DE CONTAS PADRÃO (use APENAS estes códigos)");
        sb.AppendLine();
        sb.AppendLine("Os códigos abaixo são as únicas contas analíticas que o sistema aceita.");
        sb.AppendLine("Não invente códigos — escolha o mais próximo do que aparece no PDF.");
        sb.AppendLine();

        foreach (var grupo in contasAnaliticas.GroupBy(c => c.GrupoPrincipal))
        {
            sb.AppendLine($"### {grupo.Key} (grupo {(int)grupo.Key})");
            foreach (var c in grupo.OrderBy(x => x.Ordem).ThenBy(x => x.Codigo))
            {
                sb.AppendLine($"- `{c.Codigo}` — {c.Descricao}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("## REGRAS DE EXTRAÇÃO");
        sb.AppendLine();
        sb.AppendLine("1. **Multiplicador**: detecte se valores estão em reais (1), milhares (1000) ou milhões (1000000).");
        sb.AppendLine("Procure por 'R$ mil', 'valores em milhares', 'R$ milhões', etc.");
        sb.AppendLine();
        sb.AppendLine("2. **Valores retornados**: sempre EM REAIS, ou seja, JÁ APLIQUE O MULTIPLICADOR.");
        sb.AppendLine("Ex: PDF diz 'Caixa: 1.234' com cabeçalho 'R$ mil' → retorne valor_em_reais = 1234000.");
        sb.AppendLine();
        sb.AppendLine("3. **Tipo**: 'Consolidado' se o balanço agrega controlada(s); 'Individual' se é só da controladora.");
        sb.AppendLine("Se o PDF tem AMBOS lado a lado, prefira o 'Consolidado'.");
        sb.AppendLine();
        sb.AppendLine("4. **Contas**: retorne SOMENTE contas analíticas (não retorne totalizadoras tipo 'Ativo Total').");
        sb.AppendLine("Linhas que você não conseguir mapear ao plano podem ser descartadas.");
        sb.AppendLine();
        sb.AppendLine("5. **Valores negativos** (deduções, prejuízos acumulados): retorne como número negativo.");
        sb.AppendLine();
        sb.AppendLine("6. **Período**: se o PDF tem balanços de múltiplos anos lado a lado, prefira o ANO MAIS RECENTE.");
        sb.AppendLine();
        sb.AppendLine("Agora, analise o PDF e chame a função 'registrar_balanco_extraido' com os dados.");
        return sb.ToString();
    }

    // ═════════════ SCHEMA DA FUNÇÃO ═════════════
    // No Gemini, function_declarations.parameters usa OpenAPI 3.0 Schema (similar ao Claude).

    private static object ConstruirSchemaFuncao() => new
    {
        type = "object",
        required = new[] { "tipo_balanco", "moeda", "multiplicador_detectado", "contas" },
        properties = new Dictionary<string, object>
        {
            ["tipo_balanco"] = new
            {
                type = "string",
                @enum = new[] { "Individual", "Consolidado" },
                description = "Tipo do balanço encontrado no PDF."
            },
            ["ano_exercicio"] = new
            {
                type = "integer",
                description = "Ano de exercício do balanço (4 dígitos)."
            },
            ["data_referencia"] = new
            {
                type = "string",
                description = "Data de referência do balanço no formato YYYY-MM-DD (geralmente 31/12 do ano)."
            },
            ["moeda"] = new
            {
                type = "string",
                description = "Código ISO da moeda (BRL, USD, EUR). Default: BRL."
            },
            ["multiplicador_detectado"] = new
            {
                type = "integer",
                description = "Multiplicador dos valores: 1 se em reais, 1000 se em milhares, 1000000 se em milhões. Retorne apenas um destes três números."
            },
            ["razao_social_pdf"] = new
            {
                type = "string",
                description = "Razão social da empresa conforme aparece no PDF (pra confirmação)."
            },
            ["cnpj_pdf"] = new
            {
                type = "string",
                description = "CNPJ encontrado no PDF (apenas dígitos). Opcional."
            },
            ["contas"] = new
            {
                type = "array",
                description = "Lista de contas analíticas extraídas, com valores em reais.",
                items = new
                {
                    type = "object",
                    required = new[] { "codigo_padrao", "valor_em_reais" },
                    properties = new Dictionary<string, object>
                    {
                        ["codigo_padrao"] = new
                        {
                            type = "string",
                            description = "Código exato do plano de contas (ex: '1.01.01')."
                        },
                        ["descricao_original"] = new
                        {
                            type = "string",
                            description = "Como a linha aparece no PDF (pra rastreabilidade)."
                        },
                        ["valor_em_reais"] = new
                        {
                            type = "number",
                            description = "Valor da conta em reais (multiplicador JÁ APLICADO)."
                        }
                    }
                }
            },
            ["observacoes"] = new
            {
                type = "string",
                description = "Observações sobre o parsing, qualidade do PDF, ambiguidades encontradas."
            }
        }
    };

    // ═════════════ PARSER DA RESPOSTA ═════════════

    /// <summary>
    /// Encontra a parte com 'functionCall' no response do Gemini e extrai
    /// o args (que é nosso JSON estruturado).
    ///
    /// Estrutura esperada:
    ///   candidates[0].content.parts[N].functionCall.args
    /// </summary>
    private static ImportacaoPdfResultado ConverterRespostaEmResultado(
        string responseJson, string hash, string nomeArquivo, long tamanho,
        List<ContaPadrao> contasAnaliticas)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) ||
            candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException(
                "Gemini não retornou candidates — resposta inesperada da API.");
        }

        var firstCandidate = candidates[0];
        if (!firstCandidate.TryGetProperty("content", out var contentObj) ||
            !contentObj.TryGetProperty("parts", out var parts))
        {
            throw new InvalidOperationException(
                "Gemini retornou estrutura sem content.parts.");
        }

        // Procura a primeira part com functionCall
        JsonElement? functionCall = null;
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("functionCall", out var fc))
            {
                functionCall = fc;
                break;
            }
        }

        if (functionCall is null)
        {
            throw new InvalidOperationException(
                "Gemini não retornou functionCall — resposta inesperada.");
        }

        if (!functionCall.Value.TryGetProperty("args", out var input))
        {
            throw new InvalidOperationException(
                "Gemini retornou functionCall sem args.");
        }

        var resultado = new ImportacaoPdfResultado
        {
            HashPdf = hash,
            NomeArquivo = nomeArquivo,
            TamanhoBytes = tamanho
        };

        // Tipo
        if (input.TryGetProperty("tipo_balanco", out var tipo))
        {
            resultado.TipoDetectado = tipo.GetString() == "Consolidado"
                ? TipoBalanco.Consolidado
                : TipoBalanco.Individual;
            resultado.ConfiancaTipo = 0.95;
        }

        // Ano + data
        if (input.TryGetProperty("ano_exercicio", out var ano) && ano.ValueKind == JsonValueKind.Number)
        {
            resultado.AnoDetectado = ano.GetInt32();
        }
        if (input.TryGetProperty("data_referencia", out var data) && data.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(data.GetString(), out var dt))
            {
                resultado.DataReferenciaDetectada = dt;
                resultado.AnoDetectado ??= dt.Year;
            }
        }

        // Moeda
        if (input.TryGetProperty("moeda", out var moeda) && moeda.ValueKind == JsonValueKind.String)
        {
            resultado.MoedaDetectada = moeda.GetString() ?? "BRL";
        }

        // Multiplicador
        if (input.TryGetProperty("multiplicador_detectado", out var mult) && mult.ValueKind == JsonValueKind.Number)
        {
            resultado.MultiplicadorDetectado = mult.GetInt32();
        }

        // Avisos
        if (input.TryGetProperty("observacoes", out var obs) && obs.ValueKind == JsonValueKind.String)
        {
            var texto = obs.GetString();
            if (!string.IsNullOrWhiteSpace(texto))
                resultado.Avisos.Add(texto);
        }

        if (input.TryGetProperty("razao_social_pdf", out var rs) && rs.ValueKind == JsonValueKind.String)
        {
            resultado.Avisos.Add($"PDF identifica: {rs.GetString()}");
        }

        // Matching das contas
        if (input.TryGetProperty("contas", out var contas) && contas.ValueKind == JsonValueKind.Array)
        {
            var porCodigo = contasAnaliticas.ToDictionary(c => c.Codigo, c => c);

            foreach (var c in contas.EnumerateArray())
            {
                if (!c.TryGetProperty("codigo_padrao", out var cod)) continue;
                if (!c.TryGetProperty("valor_em_reais", out var val)) continue;

                var codigo = cod.GetString();
                if (string.IsNullOrEmpty(codigo)) continue;

                var valor = val.ValueKind == JsonValueKind.Number
                    ? val.GetDecimal()
                    : 0m;

                if (porCodigo.TryGetValue(codigo, out var contaPadrao))
                {
                    resultado.ContasMapeadas[contaPadrao.Id] = valor;
                    resultado.LinhasExaminadas++;
                }
                else
                {
                    var desc = c.TryGetProperty("descricao_original", out var d)
                        ? d.GetString() ?? codigo
                        : codigo;
                    resultado.LinhasNaoMapeadas.Add(new LinhaNaoMapeada(
                        $"[{codigo}] {desc}", valor));
                }
            }
        }

        if (resultado.ContasMapeadas.Count == 0)
        {
            resultado.Avisos.Add(
                "Nenhuma conta foi mapeada. O PDF pode não ser um balanço patrimonial, " +
                "estar com qualidade ruim (escaneado), ou ter um formato muito atípico.");
        }

        return resultado;
    }

    // ═════════════ HELPERS ═════════════

    private static string CalcularHash(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var h = sha.ComputeHash(bytes);
        return Convert.ToHexString(h).ToLowerInvariant();
    }

    /// <summary>
    /// Normaliza um código de conta removendo zeros à esquerda de cada segmento.
    /// "1.01.01" → "1.1.1", "2.03" → "2.3". Usado pra casar códigos quando a IA
    /// retorna o formato sem zeros de preenchimento.
    /// </summary>
    private static string NormalizarCodigo(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return string.Empty;
        var segmentos = codigo.Split('.')
            .Select(s =>
            {
                var t = s.Trim().TrimStart('0');
                return t.Length == 0 ? "0" : t;
            });
        return string.Join(".", segmentos);
    }

    /// <summary>Tenta extrair a mensagem de erro do payload de erro do Gemini.</summary>
    private static string ExtrairErroDaApi(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err))
            {
                if (err.TryGetProperty("message", out var msg))
                    return msg.GetString() ?? body;
            }
        }
        catch { /* silenciar — retorna body bruto */ }
        return body.Length > 300 ? body[..300] + "..." : body;
    }

    // ═════════════ AUXILIARES DO FLUXO AUTOMÁTICO ═════════════

    private const string DescContaAjuste = "Ajuste automático (revisar)";

    private static string RotuloSubgrupo(string codigo) => codigo switch
    {
        "1.01" => "Ativo Circulante",
        "1.02" => "Ativo Não Circulante",
        "2.01" => "Passivo Circulante",
        "2.02" => "Passivo Não Circulante",
        "2.03" => "Patrimônio Líquido",
        _ => codigo
    };

    /// <summary>
    /// Retorna a conta "Ajuste automático" do subgrupo, criando-a se necessário.
    /// É onde a reconciliação lança a diferença pra o subgrupo fechar com o PDF.
    /// </summary>
    private async Task<ContaPadrao?> ObterOuCriarContaAjusteAsync(string codSubgrupo, List<ContaPadrao> contas)
    {
        var pai = contas.FirstOrDefault(c => c.Codigo == codSubgrupo && c.EhTotalizadora);
        if (pai is null) return null;

        var existente = contas.FirstOrDefault(c =>
            c.ContaPaiId == pai.Id &&
            string.Equals(c.Descricao, DescContaAjuste, StringComparison.OrdinalIgnoreCase));
        if (existente is not null) return existente;

        var r = await _listagens.CriarContaAnaliticaAsync(pai.Id, DescContaAjuste);
        if (!r.Sucesso || r.Dados is null) return null;
        contas.Add(r.Dados);
        return r.Dados;
    }

    private static string ConstruirPromptSistemaAutomatico() => """
        # PAPEL
        Você é um analista contábil sênior (CRC ativo) especializado em análise de balanços
        patrimoniais brasileiros para concessão de crédito bancário. Você domina a Lei 6.404/76,
        os pronunciamentos do CPC e a estrutura de demonstrações da CVM/B3. Já analisou milhares
        de balanços de S.A. abertas, fechadas, Ltdas e empresas do Simples.

        # MISSÃO
        Extrair, com PRECISÃO MÁXIMA, todos os dados de um PDF de balanço patrimonial,
        independentemente do formato de origem (CVM, B3, Receita, contador local, PDF escaneado).

        # METODOLOGIA OBRIGATÓRIA (siga nesta ordem)
        1. LEIA o documento inteiro primeiro. Identifique se é Balanço Patrimonial (BP) — procure
           por "ATIVO", "PASSIVO", "PATRIMÔNIO LÍQUIDO". Ignore DRE, DFC, notas explicativas.
        2. IDENTIFIQUE a empresa (razão social, CNPJ, tipo societário).
        3. DETECTE quantas colunas de valores existem. Cada coluna é um período (ano). Balanços
           são quase sempre COMPARATIVOS (ano atual + anterior). NÃO esqueça nenhuma coluna.
        4. DETECTE a unidade monetária no cabeçalho ("R$ mil", "em milhares", "R$ milhões").
        5. Para CADA coluna/período, mapeie CADA linha de conta ao plano de contas padrão.
        6. VALIDE: a soma do Ativo deve ser igual à soma de (Passivo + Patrimônio Líquido).
           Se não bater, REVISE seus números antes de responder — provavelmente você errou um
           sinal, esqueceu uma conta ou aplicou o multiplicador errado.
        7. Preencha o campo 'raciocinio' explicando o que você encontrou ANTES de extrair.

        # PRINCÍPIOS
        - PRECISÃO acima de tudo. É melhor mapear uma conta como mais genérica do que errar o valor.
        - NUNCA invente valores. Se uma conta não aparece no PDF, não a inclua.
        - NUNCA inclua totalizadoras (Ativo Total, Total do Passivo) na lista de contas — apenas
          contas analíticas (as linhas de detalhe).
        - Você SEMPRE responde chamando a função 'registrar_analise_completa'. NUNCA escreve texto livre.
        """;

    private string ConstruirPromptAutomatico(List<ContaPadrao> contasAnaliticas)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analise o balanço patrimonial em anexo e extraia EMPRESA + TODOS OS PERÍODOS,");
        sb.AppendLine("seguindo rigorosamente a metodologia e as regras abaixo.");
        sb.AppendLine();

        // ---- Plano de contas, agrupado e com os subgrupos (totalizadoras) visíveis ----
        sb.AppendLine("## PLANO DE CONTAS PADRÃO");
        sb.AppendLine("Mapeie cada linha do PDF para o código MAIS ESPECÍFICO que corresponda.");
        sb.AppendLine("Se nenhum código servir bem, deixe 'codigo_padrao' vazio e informe");
        sb.AppendLine("'subgrupo_sugerido' (o código do subgrupo onde a conta deveria entrar) —");
        sb.AppendLine("o sistema criará a conta nova automaticamente naquele subgrupo.");
        sb.AppendLine();
        foreach (var grupo in contasAnaliticas.GroupBy(c => c.GrupoPrincipal))
        {
            sb.AppendLine($"### {grupo.Key}");
            foreach (var c in grupo.OrderBy(x => x.Ordem).ThenBy(x => x.Codigo))
                sb.AppendLine($"- `{c.Codigo}` — {c.Descricao}");
            sb.AppendLine();
        }

        // ---- Sinônimos contábeis comuns (ajuda o modelo a casar termos) ----
        sb.AppendLine("## SINÔNIMOS CONTÁBEIS FREQUENTES (mapeie estes termos)");
        sb.AppendLine("- 'Disponibilidades', 'Caixa e bancos', 'Numerário' → Caixa e Equivalentes de Caixa");
        sb.AppendLine("- 'Duplicatas a receber', 'Clientes', 'Contas a receber de clientes' → Contas a Receber / Clientes");
        sb.AppendLine("- 'Estoques', 'Mercadorias', 'Produtos acabados', 'Almoxarifado' → Estoques");
        sb.AppendLine("- 'Aplicações financeiras', 'Títulos e valores mobiliários' → Aplicações Financeiras");
        sb.AppendLine("- 'Imobilizado', 'Ativo imobilizado', 'Bens do imobilizado' → Imobilizado");
        sb.AppendLine("- 'Intangível', 'Marcas e patentes', 'Ágio', 'Goodwill' → Intangível");
        sb.AppendLine("- 'Fornecedores', 'Duplicatas a pagar', 'Contas a pagar a fornecedores' → Fornecedores");
        sb.AppendLine("- 'Empréstimos', 'Financiamentos', 'Debêntures', 'Mútuos' → Empréstimos e Financiamentos");
        sb.AppendLine("- 'Obrigações sociais e trabalhistas', 'Salários a pagar', 'Provisões trabalhistas' → Obrigações Trabalhistas");
        sb.AppendLine("- 'Obrigações fiscais', 'Impostos a recolher', 'Tributos a pagar' → Obrigações Fiscais/Tributárias");
        sb.AppendLine("- 'Capital social', 'Capital subscrito/integralizado' → Capital Social");
        sb.AppendLine("- 'Reservas de lucros', 'Reserva legal', 'Reserva de capital' → Reservas");
        sb.AppendLine("- 'Lucros/prejuízos acumulados', 'Resultado do exercício' → Lucros/Prejuízos Acumulados");
        sb.AppendLine();

        // ---- Regras ----
        sb.AppendLine("## REGRAS DE EXTRAÇÃO");
        sb.AppendLine("1. EMPRESA: razao_social, cnpj (só dígitos), tipo_empresa (SaAberta, SaFechada,");
        sb.AppendLine("   Ltda, Eireli, Mei, Outro) e uf_atuacao (2 letras).");
        sb.AppendLine();
        sb.AppendLine("2. PERÍODOS: um item por COLUNA de valores do balanço. Balanços comparativos têm");
        sb.AppendLine("   2+ colunas (ex: 31/12/2024 e 31/12/2023) — extraia TODAS. Se o PDF traz");
        sb.AppendLine("   versão Individual (Controladora) E Consolidada, gere períodos para CADA uma.");
        sb.AppendLine("   Use 'mes'=0 para balanço anual (31/12); 1-12 só para períodos intermediários.");
        sb.AppendLine();
        sb.AppendLine("3. MULTIPLICADOR: leia o cabeçalho. 'Em R$ mil'/'milhares' → 1000;");
        sb.AppendLine("   'Em R$ milhões' → 1000000; sem indicação → 1. SEMPRE aplique o multiplicador,");
        sb.AppendLine("   retornando 'valor_em_reais' já em reais cheios.");
        sb.AppendLine("   Ex: tabela mostra '1.234' com 'R$ mil' → valor_em_reais = 1234000.");
        sb.AppendLine();
        sb.AppendLine("4. SINAIS: deduções (depreciação acumulada, provisão p/ devedores duvidosos,");
        sb.AppendLine("   ações em tesouraria, prejuízos acumulados) são NEGATIVAS. Valores entre");
        sb.AppendLine("   parênteses no PDF, ex '(1.500)', significam NEGATIVO.");
        sb.AppendLine();
        sb.AppendLine("5. NÃO inclua totalizadoras (Ativo Total, Total do Circulante, Total do Passivo,");
        sb.AppendLine("   Total do PL). Inclua apenas as linhas ANALÍTICAS (de detalhe).");
        sb.AppendLine();
        sb.AppendLine("6. SUBTOTAIS IMPRESSOS (muito importante): leia e informe os valores que o PDF");
        sb.AppendLine("   mostra para cada subtotal de grupo, EXATAMENTE como impressos:");
        sb.AppendLine("   - ativo_circulante_impresso         (linha 'ATIVO CIRCULANTE')");
        sb.AppendLine("   - ativo_nao_circulante_impresso     (linha 'ATIVO NÃO CIRCULANTE')");
        sb.AppendLine("   - passivo_circulante_impresso       (linha 'PASSIVO CIRCULANTE')");
        sb.AppendLine("   - passivo_nao_circulante_impresso   (linha 'PASSIVO NÃO CIRCULANTE')");
        sb.AppendLine("   - patrimonio_liquido_impresso       (linha 'PATRIMÔNIO LÍQUIDO' / 'PL TOTAL', pode ser negativo)");
        sb.AppendLine("   - ativo_total_impresso e passivo_pl_total_impresso (os totais gerais)");
        sb.AppendLine("   O sistema usa esses subtotais pra conferir e completar — então leia-os com");
        sb.AppendLine("   cuidado direto do documento. NÃO os calcule, apenas COPIE o que está impresso.");
        sb.AppendLine();
        sb.AppendLine("7. EXTRAIA TODAS AS LINHAS analíticas (de detalhe) com valor, cada uma no seu");
        sb.AppendLine("   subgrupo. Não inclua as linhas de subtotal/total na lista 'contas' (elas vão");
        sb.AppendLine("   nos campos *_impresso acima). Contas sem equivalente no plano: codigo_padrao");
        sb.AppendLine("   vazio + subgrupo_sugerido (ex '2.02'). Não invente valores nem duplique linhas.");
        sb.AppendLine();
        sb.AppendLine("8. RACIOCÍNIO: em 'raciocinio', descreva tipo do documento, nº de colunas e a");
        sb.AppendLine("   unidade monetária detectada.");
        sb.AppendLine();
        sb.AppendLine("9. DRE (se houver): se o PDF também trouxer a Demonstração do Resultado do");
        sb.AppendLine("   Exercício, preencha o objeto 'dre' de cada período com receita líquida,");
        sb.AppendLine("   lucro bruto, resultado operacional (EBIT), despesas financeiras e lucro");
        sb.AppendLine("   líquido. Se o documento NÃO tiver DRE (só balanço), omita 'dre' ou deixe 0.");
        sb.AppendLine();
        sb.AppendLine("Agora chame 'registrar_analise_completa' com TODOS os dados.");
        return sb.ToString();
    }

    private static object ConstruirSchemaAutomatico() => new
    {
        type = "object",
        required = new[] { "raciocinio", "empresa", "periodos" },
        properties = new Dictionary<string, object>
        {
            ["raciocinio"] = new
            {
                type = "string",
                description = "Análise do documento ANTES de extrair: tipo de demonstração, " +
                              "quantas colunas/períodos, unidade monetária detectada, se é " +
                              "individual/consolidado. Pense passo a passo aqui."
            },
            ["empresa"] = new
            {
                type = "object",
                required = new[] { "razao_social" },
                properties = new Dictionary<string, object>
                {
                    ["razao_social"] = new { type = "string", description = "Razão social completa." },
                    ["cnpj"] = new { type = "string", description = "CNPJ apenas dígitos (14). Vazio se não encontrar." },
                    ["tipo_empresa"] = new
                    {
                        type = "string",
                        @enum = new[] { "SaAberta", "SaFechada", "Ltda", "Eireli", "Mei", "Outro" },
                        description = "Tipo societário."
                    },
                    ["uf_atuacao"] = new { type = "string", description = "UF principal (2 letras). Opcional." }
                }
            },
            ["periodos"] = new
            {
                type = "array",
                description = "TODOS os períodos/colunas do balanço (não esqueça o ano comparativo).",
                items = new
                {
                    type = "object",
                    required = new[] { "ano", "tipo_balanco", "multiplicador", "contas" },
                    properties = new Dictionary<string, object>
                    {
                        ["ano"] = new { type = "integer", description = "Ano de exercício (4 dígitos)." },
                        ["mes"] = new { type = "integer", description = "Mês 1-12 se intermediário; 0 se anual." },
                        ["tipo_balanco"] = new
                        {
                            type = "string",
                            @enum = new[] { "Individual", "Consolidado" },
                            description = "Tipo do balanço deste período."
                        },
                        ["multiplicador"] = new { type = "integer", description = "1, 1000 ou 1000000 (detectado no cabeçalho)." },
                        ["ativo_total_impresso"] = new
                        {
                            type = "number",
                            description = "Valor de 'TOTAL DO ATIVO' EXATAMENTE como impresso no PDF (multiplicador aplicado)."
                        },
                        ["passivo_pl_total_impresso"] = new
                        {
                            type = "number",
                            description = "Valor de 'TOTAL DO PASSIVO + PATRIMÔNIO LÍQUIDO' impresso no PDF (multiplicador aplicado)."
                        },
                        ["ativo_circulante_impresso"] = new
                        {
                            type = "number",
                            description = "Subtotal impresso de 'ATIVO CIRCULANTE' (multiplicador aplicado)."
                        },
                        ["ativo_nao_circulante_impresso"] = new
                        {
                            type = "number",
                            description = "Subtotal impresso de 'ATIVO NÃO CIRCULANTE' (multiplicador aplicado)."
                        },
                        ["passivo_circulante_impresso"] = new
                        {
                            type = "number",
                            description = "Subtotal impresso de 'PASSIVO CIRCULANTE' (multiplicador aplicado)."
                        },
                        ["passivo_nao_circulante_impresso"] = new
                        {
                            type = "number",
                            description = "Subtotal impresso de 'PASSIVO NÃO CIRCULANTE' (multiplicador aplicado)."
                        },
                        ["patrimonio_liquido_impresso"] = new
                        {
                            type = "number",
                            description = "Subtotal impresso de 'PATRIMÔNIO LÍQUIDO' ou 'PL TOTAL' (multiplicador aplicado, pode ser negativo)."
                        },
                        ["dre"] = new
                        {
                            type = "object",
                            description = "Demonstração do Resultado do Exercício DESTE período, SE o PDF a contiver. " +
                                          "Se o documento não tiver DRE, omita ou deixe tudo 0. Valores em reais (multiplicador aplicado).",
                            properties = new Dictionary<string, object>
                            {
                                ["receita_liquida"] = new { type = "number", description = "Receita Líquida de Vendas/Serviços." },
                                ["lucro_bruto"] = new { type = "number", description = "Lucro Bruto (Receita - Custos)." },
                                ["resultado_operacional"] = new { type = "number", description = "Resultado Operacional / EBIT (antes do resultado financeiro e impostos)." },
                                ["despesas_financeiras"] = new { type = "number", description = "Despesas financeiras do período (valor positivo)." },
                                ["lucro_liquido"] = new { type = "number", description = "Lucro/Prejuízo Líquido do exercício (negativo se prejuízo)." }
                            }
                        },
                        ["contas"] = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                required = new[] { "valor_em_reais" },
                                properties = new Dictionary<string, object>
                                {
                                    ["codigo_padrao"] = new
                                    {
                                        type = "string",
                                        description = "Código do plano (ex: '1.01.01'). Vazio se for conta nova."
                                    },
                                    ["descricao_original"] = new
                                    {
                                        type = "string",
                                        description = "Texto exato da linha no PDF (sempre preencha)."
                                    },
                                    ["subgrupo_sugerido"] = new
                                    {
                                        type = "string",
                                        description = "Se codigo_padrao vazio: código do SUBGRUPO totalizador " +
                                                      "onde a conta deve ser criada (ex: '2.02' p/ Passivo Não Circulante)."
                                    },
                                    ["valor_em_reais"] = new
                                    {
                                        type = "number",
                                        description = "Valor em reais, multiplicador JÁ aplicado, negativo se dedução."
                                    }
                                }
                            }
                        }
                    }
                }
            },
            ["observacoes"] = new { type = "string", description = "Observações gerais sobre a qualidade/ambiguidades do PDF." }
        }
    };

    /// <summary>Conta que a IA não mapeou e pediu pra criar num subgrupo.</summary>
    private sealed record ContaNovaPendente(int PeriodoIndex, string SubgrupoCodigo, string Descricao, decimal Valor);

    private static (AnaliseAutomaticaResultado resultado, List<ContaNovaPendente> pendencias) ConverterRespostaAutomatica(
        string responseJson, string hash, string nomeArquivo, List<ContaPadrao> contasAnaliticas)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;
        var pendencias = new List<ContaNovaPendente>();

        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            throw new InvalidOperationException("Gemini não retornou candidates.");

        if (!candidates[0].TryGetProperty("content", out var contentObj) ||
            !contentObj.TryGetProperty("parts", out var parts))
            throw new InvalidOperationException("Gemini retornou estrutura sem content.parts.");

        JsonElement? args = null;
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("functionCall", out var fc) && fc.TryGetProperty("args", out var a))
            {
                args = a;
                break;
            }
        }
        if (args is null)
            throw new InvalidOperationException("Gemini não retornou functionCall com args.");

        var input = args.Value;
        var resultado = new AnaliseAutomaticaResultado { HashPdf = hash, NomeArquivo = nomeArquivo };

        // Empresa
        if (input.TryGetProperty("empresa", out var emp))
        {
            if (emp.TryGetProperty("razao_social", out var rs) && rs.ValueKind == JsonValueKind.String)
                resultado.RazaoSocial = rs.GetString() ?? "";
            if (emp.TryGetProperty("cnpj", out var cnpj) && cnpj.ValueKind == JsonValueKind.String)
                resultado.Cnpj = new string((cnpj.GetString() ?? "").Where(char.IsDigit).ToArray());
            if (emp.TryGetProperty("uf_atuacao", out var uf) && uf.ValueKind == JsonValueKind.String)
                resultado.UfAtuacao = uf.GetString();
            if (emp.TryGetProperty("tipo_empresa", out var te) && te.ValueKind == JsonValueKind.String)
            {
                resultado.TipoEmpresa = te.GetString() switch
                {
                    "SaAberta" => TipoEmpresa.SaAberta,
                    "SaFechada" => TipoEmpresa.SaFechada,
                    "Ltda" => TipoEmpresa.Ltda,
                    "Eireli" => TipoEmpresa.Eireli,
                    "Mei" => TipoEmpresa.Mei,
                    _ => TipoEmpresa.Outro
                };
            }
        }

        if (input.TryGetProperty("observacoes", out var obs) && obs.ValueKind == JsonValueKind.String)
        {
            var t = obs.GetString();
            if (!string.IsNullOrWhiteSpace(t)) resultado.Avisos.Add(t);
        }

        // Índices de mapeamento (exato + normalizado p/ tolerar "1.1.1" vs "1.01.01")
        var porCodigo = contasAnaliticas.ToDictionary(c => c.Codigo, c => c);
        var porCodigoNorm = contasAnaliticas
            .GroupBy(c => NormalizarCodigo(c.Codigo))
            .ToDictionary(g => g.Key, g => g.First());

        if (input.TryGetProperty("periodos", out var periodos) && periodos.ValueKind == JsonValueKind.Array)
        {
            int periodoIdx = -1;
            foreach (var p in periodos.EnumerateArray())
            {
                var pd = new PeriodoDetectado();

                if (p.TryGetProperty("ano", out var ano) && ano.ValueKind == JsonValueKind.Number)
                    pd.Ano = ano.GetInt32();
                if (pd.Ano < 2000 || pd.Ano > 2100) continue; // ano inválido → descarta período

                periodoIdx++; // índice do período que SERÁ adicionado

                if (p.TryGetProperty("mes", out var mes) && mes.ValueKind == JsonValueKind.Number)
                {
                    var m = mes.GetInt32();
                    pd.Mes = (m >= 1 && m <= 12) ? m : null;
                }

                if (p.TryGetProperty("tipo_balanco", out var tb) && tb.ValueKind == JsonValueKind.String)
                    pd.Tipo = tb.GetString() == "Consolidado" ? TipoBalanco.Consolidado : TipoBalanco.Individual;

                if (p.TryGetProperty("contas", out var contas) && contas.ValueKind == JsonValueKind.Array)
                {
                    foreach (var c in contas.EnumerateArray())
                    {
                        if (!c.TryGetProperty("valor_em_reais", out var val)) continue;
                        var valor = val.ValueKind == JsonValueKind.Number ? val.GetDecimal() : 0m;

                        var codigo = c.TryGetProperty("codigo_padrao", out var cod) ? cod.GetString() : null;
                        var descricao = c.TryGetProperty("descricao_original", out var d) ? d.GetString() : null;

                        if (!string.IsNullOrWhiteSpace(codigo))
                        {
                            // Mapeamento por código (exato → normalizado)
                            if (porCodigo.TryGetValue(codigo, out var cp))
                                pd.ContasMapeadas[cp.Id] = valor;
                            else if (porCodigoNorm.TryGetValue(NormalizarCodigo(codigo), out var cp2))
                                pd.ContasMapeadas[cp2.Id] = valor;
                            else
                                RegistrarPendencia(c, descricao, valor, periodoIdx, pendencias);
                        }
                        else
                        {
                            // Conta nova (IA não achou equivalente)
                            RegistrarPendencia(c, descricao, valor, periodoIdx, pendencias);
                        }
                    }
                }

                // Totais e subtotais impressos no PDF (gabarito pra reconciliação)
                if (p.TryGetProperty("ativo_total_impresso", out var ai) && ai.ValueKind == JsonValueKind.Number)
                    pd.AtivoTotalImpresso = ai.GetDecimal();
                if (p.TryGetProperty("passivo_pl_total_impresso", out var pi) && pi.ValueKind == JsonValueKind.Number)
                    pd.PassivoPlTotalImpresso = pi.GetDecimal();

                void CapturarSubtotal(string prop, string codSubgrupo)
                {
                    if (p.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number)
                        pd.SubtotaisImpressos[codSubgrupo] = v.GetDecimal();
                }
                CapturarSubtotal("ativo_circulante_impresso", "1.01");
                CapturarSubtotal("ativo_nao_circulante_impresso", "1.02");
                CapturarSubtotal("passivo_circulante_impresso", "2.01");
                CapturarSubtotal("passivo_nao_circulante_impresso", "2.02");
                CapturarSubtotal("patrimonio_liquido_impresso", "2.03");

                // DRE (se presente)
                if (p.TryGetProperty("dre", out var dre) && dre.ValueKind == JsonValueKind.Object)
                {
                    decimal Num(string prop) =>
                        dre.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : 0m;
                    pd.DreReceitaLiquida       = Num("receita_liquida");
                    pd.DreLucroBruto           = Num("lucro_bruto");
                    pd.DreResultadoOperacional = Num("resultado_operacional");
                    pd.DreDespesasFinanceiras  = Num("despesas_financeiras");
                    pd.DreLucroLiquido         = Num("lucro_liquido");
                }

                resultado.Periodos.Add(pd);
            }
        }

        if (resultado.Periodos.Count == 0)
            resultado.Avisos.Add("Nenhum período foi detectado. O PDF pode não ser um balanço patrimonial.");

        return (resultado, pendencias);
    }

    private static void RegistrarPendencia(JsonElement conta, string? descricao, decimal valor,
        int periodoIdx, List<ContaNovaPendente> pendencias)
    {
        var subgrupo = conta.TryGetProperty("subgrupo_sugerido", out var sg) ? sg.GetString() : null;
        if (string.IsNullOrWhiteSpace(subgrupo) || string.IsNullOrWhiteSpace(descricao))
            return; // sem destino ou sem nome → não dá pra criar
        pendencias.Add(new ContaNovaPendente(periodoIdx, subgrupo.Trim(), descricao.Trim(), valor));
    }
}
