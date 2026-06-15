using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Implementação do parser de PDF usando <c>UglyToad.PdfPig</c>.
///
/// **Estratégia (heurística, não 100% precisa):**
///   1. Calcula hash do PDF (pra detecção de reimportação)
///   2. Extrai todas as palavras posicionadas (com coordenadas X,Y)
///   3. Agrupa palavras em linhas por proximidade vertical (mesma Y ± tolerância)
///   4. Procura padrões "&lt;descrição&gt; ... &lt;valor numérico&gt;" em cada linha
///   5. Detecta metadados (multiplicador, tipo, ano) por palavras-chave
///   6. Faz matching das descrições contra o plano de contas padrão
///      usando normalização (sem acento, lowercase) + "contains" + score
///
/// **Limitações conhecidas:**
///   - PDFs com layout muito complexo (tabelas com bordas, células mescladas)
///     podem ter linhas mal-agrupadas
///   - Matching é por similaridade textual — pode confundir contas com nomes parecidos
///   - **O usuário sempre revisa antes de salvar** — esse é o contrato.
/// </summary>
public class PdfParserService : IPdfParserService
{
    private readonly IListagensService _listagens;
    private readonly ILogService _log;
    public string NomeServico => "PdfParserService";

    // Tolerância vertical pra considerar duas palavras "na mesma linha" (em pontos PDF)
    private const double ToleranciaY = 3.0;

    public PdfParserService(IListagensService listagens, ILogService log)
    {
        _listagens = listagens;
        _log = log;
    }

    public async Task<ResultadoOperacao<ImportacaoPdfResultado>> AnalisarAsync(
        Stream pdfStream, string nomeArquivo)
    {
        try
        {
            // 1) Hash SHA-256 do PDF
            pdfStream.Position = 0;
            var hash = await CalcularHashAsync(pdfStream);
            pdfStream.Position = 0;

            var tamanho = pdfStream.Length;

            // 2) Extrair linhas
            var linhasPdf = ExtrairLinhasDoPdf(pdfStream, out var qtdPaginas);

            // 3) Detectar metadados
            var textoCompleto = string.Join("\n", linhasPdf.Select(l => l.Texto));

            var resultado = new ImportacaoPdfResultado
            {
                HashPdf = hash,
                NomeArquivo = nomeArquivo,
                TamanhoBytes = tamanho,
                QtdPaginas = qtdPaginas,
                LinhasExaminadas = linhasPdf.Count
            };

            DetectarMultiplicador(textoCompleto, resultado);
            DetectarTipo(textoCompleto, resultado);
            DetectarPeriodo(textoCompleto, resultado);

            // 4) Carregar plano de contas pra matching
            var contas = (await _listagens.ListarContasPadraoAsync()).ToList();
            if (contas.Count == 0)
            {
                resultado.Avisos.Add(
                    "Plano de contas padrão vazio. Importação parcial — só metadados foram detectados.");
                return ResultadoOperacao<ImportacaoPdfResultado>.Ok(resultado,
                    "PDF analisado, mas sem plano de contas pra matching.");
            }

            // 5) Tentar mapear cada linha contra uma conta padrão
            MapearLinhasParaContas(linhasPdf, contas, resultado);

            await _log.RegistrarAsync(
                TipoEventoLog.Importacao,
                "ANALISOU_PDF",
                $"PDF '{nomeArquivo}' analisado: {resultado.ContasMapeadas.Count} contas mapeadas, " +
                $"{resultado.LinhasNaoMapeadas.Count} linhas não mapeadas, multiplicador={resultado.MultiplicadorDetectado}.");

            return ResultadoOperacao<ImportacaoPdfResultado>.Ok(resultado);
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("ANALISAR_PDF", ex);
            return ResultadoOperacao<ImportacaoPdfResultado>.FalhaExcecao(ex);
        }
    }

    // ───────── Hash ─────────

    private static async Task<string> CalcularHashAsync(Stream stream)
    {
        using var sha = SHA256.Create();
        var bytes = await sha.ComputeHashAsync(stream);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // ───────── Extração de linhas posicionadas ─────────

    /// <summary>
    /// Lê o PDF e agrupa palavras em "linhas" baseado nas coordenadas Y.
    /// Linhas com mesma Y (± tolerância) são tratadas como uma só.
    /// </summary>
    private static List<LinhaPdf> ExtrairLinhasDoPdf(Stream stream, out int qtdPaginas)
    {
        var linhas = new List<LinhaPdf>();
        using var doc = PdfDocument.Open(stream);
        qtdPaginas = doc.NumberOfPages;

        foreach (var pagina in doc.GetPages())
        {
            var palavras = pagina.GetWords().ToList();
            if (palavras.Count == 0) continue;

            // Agrupa por Y (a página PDF tem origem no canto inferior esquerdo,
            // então palavras na "mesma linha" têm Y próximos)
            var grupos = palavras
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom / ToleranciaY) * ToleranciaY)
                .OrderByDescending(g => g.Key);  // de cima pra baixo

            foreach (var grupo in grupos)
            {
                var palavrasOrdenadas = grupo
                    .OrderBy(w => w.BoundingBox.Left)
                    .ToList();
                var texto = string.Join(" ", palavrasOrdenadas.Select(w => w.Text));
                linhas.Add(new LinhaPdf(
                    Pagina: pagina.Number,
                    Y: grupo.Key,
                    Texto: texto,
                    Palavras: palavrasOrdenadas));
            }
        }
        return linhas;
    }

    // ───────── Detecção de metadados ─────────

    private static readonly Regex RegexMil =
        new(@"R\$\s*mil|valores\s+em\s+milhares|em\s+R\$\s*mil",
            RegexOptions.IgnoreCase);

    private static readonly Regex RegexMilhao =
        new(@"R\$\s*milh(õ|o)es|valores\s+em\s+milh(õ|o)es",
            RegexOptions.IgnoreCase);

    private static void DetectarMultiplicador(string texto, ImportacaoPdfResultado r)
    {
        if (RegexMilhao.IsMatch(texto))
        {
            r.MultiplicadorDetectado = 1_000_000;
            r.Avisos.Add("Valores detectados em **milhões** de reais.");
        }
        else if (RegexMil.IsMatch(texto))
        {
            r.MultiplicadorDetectado = 1_000;
            r.Avisos.Add("Valores detectados em **milhares** de reais.");
        }
        // Default: 1 (já é o valor inicial)
    }

    private static void DetectarTipo(string texto, ImportacaoPdfResultado r)
    {
        // Contagem de menções
        var textoNormalizado = Normalizar(texto);
        var qtdConsolidado = ContarOcorrencias(textoNormalizado, "consolidad");
        var qtdIndividual = ContarOcorrencias(textoNormalizado, "individual")
                          + ContarOcorrencias(textoNormalizado, "controladora");

        if (qtdConsolidado > qtdIndividual && qtdConsolidado >= 2)
        {
            r.TipoDetectado = TipoBalanco.Consolidado;
            r.ConfiancaTipo = Math.Min(1.0, qtdConsolidado / 10.0);
        }
        else if (qtdIndividual > 0)
        {
            r.TipoDetectado = TipoBalanco.Individual;
            r.ConfiancaTipo = Math.Min(1.0, qtdIndividual / 10.0);
        }
    }

    private static readonly Regex RegexAno = new(@"\b(20[1-3]\d)\b", RegexOptions.Compiled);
    private static readonly Regex RegexDataReferencia =
        new(@"31\s*[\/\-\.\s]\s*12\s*[\/\-\.\s]\s*(20[1-3]\d)|31\s+de\s+dezembro\s+de\s+(20[1-3]\d)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static void DetectarPeriodo(string texto, ImportacaoPdfResultado r)
    {
        // Procura primeiro por "31/12/AAAA" — mais confiável
        var match = RegexDataReferencia.Match(texto);
        if (match.Success)
        {
            var anoStr = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            if (int.TryParse(anoStr, out var ano))
            {
                r.AnoDetectado = ano;
                r.DataReferenciaDetectada = new DateTime(ano, 12, 31);
                return;
            }
        }

        // Fallback: pega o ano que aparece mais vezes no texto
        var anos = RegexAno.Matches(texto)
            .Select(m => int.Parse(m.Value))
            .Where(a => a >= 2010 && a <= DateTime.Now.Year + 1)
            .GroupBy(a => a)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        if (anos is not null)
        {
            r.AnoDetectado = anos.Key;
            r.DataReferenciaDetectada = new DateTime(anos.Key, 12, 31);
        }
    }

    // ───────── Matching de linhas com plano de contas ─────────

    /// <summary>
    /// Regex pra capturar UM valor monetário no formato brasileiro:
    ///   1.234.567,89
    ///   1234567,89
    ///   1.234.567 (sem decimais)
    ///   (1.234,56)  — negativo entre parênteses
    /// </summary>
    private static readonly Regex RegexValor =
        new(@"(?<neg>\()?(?<valor>-?\d{1,3}(?:\.\d{3})*(?:,\d{1,2})?)\)?",
            RegexOptions.Compiled);

    private void MapearLinhasParaContas(
        List<LinhaPdf> linhas, List<ContaPadrao> contas, ImportacaoPdfResultado r)
    {
        // Indexa as contas analíticas (não totalizadoras) pra matching
        // — totalizadoras serão recalculadas pelo planilhamento
        var contasParaMatch = contas
            .Where(c => !c.EhTotalizadora)
            .Select(c => new ContaIndexada(c, Normalizar(c.Descricao)))
            .ToList();

        foreach (var linha in linhas)
        {
            // 1) Extrai valor numérico da linha (o último valor da direita,
            //    que é o do exercício mais recente em PDFs com 2+ colunas)
            var (descricao, valor) = SepararDescricaoEValor(linha.Texto);
            if (valor is null) continue;
            if (string.IsNullOrWhiteSpace(descricao)) continue;
            if (descricao.Length < 4) continue;  // muito curto, provavelmente número

            // 2) Busca conta padrão com maior similaridade
            var descNormalizada = Normalizar(descricao);
            var melhor = EncontrarMelhorMatch(descNormalizada, contasParaMatch);

            if (melhor is null)
            {
                r.LinhasNaoMapeadas.Add(new LinhaNaoMapeada(descricao, valor.Value));
                continue;
            }

            // 3) Aplica multiplicador
            var valorEmReais = valor.Value * r.MultiplicadorDetectado;

            // 4) Se já tinha um valor pra essa conta, mantém a primeira aparição
            //    (PDFs costumam ter consolidado + individual; pegamos o primeiro)
            if (!r.ContasMapeadas.ContainsKey(melhor.Conta.Id))
            {
                r.ContasMapeadas[melhor.Conta.Id] = valorEmReais;
            }
        }
    }

    /// <summary>
    /// Tenta separar uma linha em "descrição" e "valor numérico final".
    /// Pega o ÚLTIMO número que aparece na linha (PDFs em geral têm valores à direita).
    /// </summary>
    private static (string descricao, decimal? valor) SepararDescricaoEValor(string linha)
    {
        var matches = RegexValor.Matches(linha);
        if (matches.Count == 0) return (linha, null);

        // Pega o último match (mais à direita)
        var ultimoMatch = matches[^1];
        if (!TryParseValorBR(ultimoMatch.Groups["valor"].Value, out var valor))
        {
            return (linha, null);
        }

        // Negativo se entre parênteses
        if (ultimoMatch.Groups["neg"].Success) valor = -valor;

        // A descrição é tudo antes do último número
        var descricao = linha[..ultimoMatch.Index].Trim();
        return (descricao, valor);
    }

    /// <summary>Parse de valor BR: "1.234.567,89" → 1234567.89m.</summary>
    private static bool TryParseValorBR(string raw, out decimal valor)
    {
        // Remove pontos (separador de milhares) e troca vírgula por ponto (decimal)
        var ajustado = raw.Replace(".", "").Replace(",", ".");
        return decimal.TryParse(ajustado, NumberStyles.Any, CultureInfo.InvariantCulture, out valor);
    }

    /// <summary>
    /// Algoritmo de matching simples: a conta padrão cuja descrição normalizada
    /// está contida na descrição da linha (ou vice-versa) com maior overlap vence.
    /// </summary>
    private static ContaIndexada? EncontrarMelhorMatch(
        string descLinha, List<ContaIndexada> contas)
    {
        ContaIndexada? melhor = null;
        var melhorScore = 0.0;

        foreach (var c in contas)
        {
            var score = Similaridade(descLinha, c.DescricaoNorm);
            if (score > melhorScore)
            {
                melhorScore = score;
                melhor = c;
            }
        }

        // Threshold mínimo pra aceitar (evita matches falsos pra descrições genéricas)
        return melhorScore >= 0.6 ? melhor : null;
    }

    /// <summary>
    /// Score de similaridade entre 0 e 1.
    /// "contains" puro: se uma string está dentro da outra, retorna a razão do tamanho.
    /// Caso contrário: razão de palavras em comum.
    /// </summary>
    private static double Similaridade(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
        if (a == b) return 1.0;
        if (a.Contains(b)) return (double)b.Length / a.Length;
        if (b.Contains(a)) return (double)a.Length / b.Length;

        // Fallback: razão de palavras em comum
        var palavrasA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var palavrasB = b.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (palavrasA.Length == 0 || palavrasB.Length == 0) return 0;

        var comum = palavrasA.Intersect(palavrasB).Count();
        return (double)comum / Math.Max(palavrasA.Length, palavrasB.Length);
    }

    // ───────── helpers ─────────

    /// <summary>
    /// Normaliza pra matching: minúsculas + sem acentos + sem pontuação extra.
    /// "Caixa e Equivalentes de Caixa" → "caixa e equivalentes de caixa"
    /// </summary>
    private static string Normalizar(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return string.Empty;
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalizado)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant()
            .Replace("(", " ").Replace(")", " ")
            .Replace(".", " ").Replace(",", " ")
            .Replace("  ", " ")
            .Trim();
    }

    private static int ContarOcorrencias(string texto, string termo)
    {
        var n = 0;
        var idx = 0;
        while ((idx = texto.IndexOf(termo, idx, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            n++;
            idx += termo.Length;
        }
        return n;
    }

    // ───────── records auxiliares ─────────

    private record LinhaPdf(int Pagina, double Y, string Texto, IReadOnlyList<Word> Palavras);
    private record ContaIndexada(ContaPadrao Conta, string DescricaoNorm);
}
