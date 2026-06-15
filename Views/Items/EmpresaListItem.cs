using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Views.Items;

/// <summary>
/// Wrapper de Empresa pra exibição na CollectionView. Pré-formata os campos
/// (CNPJ com pontuação, limite em moeda BR, lista de balanços agrupada)
/// pra a View não precisar de converters XAML.
/// </summary>
public class EmpresaListItem
{
    public int Id { get; init; }
    public string RazaoSocial { get; init; } = string.Empty;
    public string Cnpj { get; init; } = string.Empty;
    public string? Rating { get; init; }
    public bool TemRating => !string.IsNullOrEmpty(Rating);
    public Color CorRating { get; init; } = Colors.Gray;
    public string LinhaIdentificacao { get; init; } = string.Empty;
    public string LinhaSetorLimite { get; init; } = string.Empty;
    public string LinhaBalancos { get; init; } = string.Empty;

    /// <summary>Construtor a partir de Empresa do domínio.</summary>
    public static EmpresaListItem De(Empresa e)
    {
        return new EmpresaListItem
        {
            Id = e.Id,
            RazaoSocial = e.RazaoSocial,
            Cnpj = FormatarCnpj(e.Cnpj),
            Rating = e.Rating,
            CorRating = CorDoRating(e.Rating),
            LinhaIdentificacao = MontarIdentificacao(e),
            LinhaSetorLimite = MontarSetorLimite(e),
            LinhaBalancos = MontarBalancos(e)
        };
    }

    private static string FormatarCnpj(string cnpj)
    {
        if (string.IsNullOrEmpty(cnpj) || cnpj.Length != 14) return cnpj;
        // 12.345.678/0001-99
        return $"{cnpj[..2]}.{cnpj[2..5]}.{cnpj[5..8]}/{cnpj[8..12]}-{cnpj[12..14]}";
    }

    private static string MontarIdentificacao(Empresa e)
    {
        var pedacos = new List<string> { $"CNPJ {FormatarCnpj(e.Cnpj)}" };
        if (!string.IsNullOrEmpty(e.UfAtuacao)) pedacos.Add(e.UfAtuacao);
        if (e.GrupoEconomico is not null) pedacos.Add($"Grupo: {e.GrupoEconomico.Nome}");
        return string.Join("• ", pedacos);
    }

    private static string MontarSetorLimite(Empresa e)
    {
        var pedacos = new List<string>();

        var setorPrincipal = e.Setores.FirstOrDefault();
        if (setorPrincipal is not null)
        {
            pedacos.Add($"Setor: {setorPrincipal.Nome}");
            if (e.Setores.Count > 1)
            {
                pedacos[0] += $"(+{e.Setores.Count - 1})";
            }
        }

        if (e.LimiteCredito.HasValue)
        {
            pedacos.Add($"Limite: R$ {e.LimiteCredito.Value:N0}");
        }

        return pedacos.Count > 0 ? string.Join("• ", pedacos) : "Sem setor/limite definido";
    }

    private static string MontarBalancos(Empresa e)
    {
        if (e.Balancos.Count == 0) return "Nenhum balanço planilhado";

        var anos = e.Balancos
            .Select(b => b.AnoExercicio)
            .Distinct()
            .OrderByDescending(a => a)
            .ToList();

        var anosTexto = string.Join(", ", anos);
        return $"Balanços: {anosTexto} ({e.Balancos.Count})";
    }

    private static Color CorDoRating(string? rating)
    {
        if (string.IsNullOrEmpty(rating)) return Colors.Gray;
        // Padrão S&P/Moody's simplificado: AAA/AA/A=verde, BBB/BB=amarelo, B/CCC ou pior=vermelho
        var primeiraLetra = rating.TrimStart('+', '-').ToUpper().FirstOrDefault();
        return primeiraLetra switch
        {
            'A' => Color.FromArgb("#16A34A"), // verde
            'B' when rating.StartsWith("BBB", StringComparison.OrdinalIgnoreCase)
                  || rating.StartsWith("BB",  StringComparison.OrdinalIgnoreCase) => Color.FromArgb("#F59E0B"), // amarelo
            'B' => Color.FromArgb("#DC2626"), // vermelho (B só)
            _   => Color.FromArgb("#DC2626")  // CCC, CC, C, D — todos vermelho
        };
    }
}

/// <summary>BindingContext da página, expõe o subtítulo dinamicamente.</summary>
public class EmpresasPageState
{
    // Reservado pra Fase 7 caso adicione MVVM. Mantido aqui pra não quebrar
    // referências futuras. Atualmente o code-behind manipula a UI diretamente.
}
