using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Lógica PURA de análise financeira (sem dependências de banco/IA), extraída
/// do AnaliseService para ser testável por testes unitários. Todos os métodos
/// são funções puras: mesma entrada → mesma saída.
/// </summary>
public static class CalculadoraIndicadores
{
    public static EvolucaoBalancoPonto ConstruirPonto(Balanco b)
    {
        decimal SomarPrefixo(string prefixo, GrupoContaPrincipal grupo) => b.Contas
            .Where(c => c.ContaPadrao is not null
                     && c.ContaPadrao.GrupoPrincipal == grupo
                     && !c.ContaPadrao.EhTotalizadora
                     && c.ContaPadrao.Codigo.StartsWith(prefixo, StringComparison.Ordinal))
            .Sum(c => c.Valor);

        return new EvolucaoBalancoPonto
        {
            Ano = b.AnoExercicio,
            TipoBalanco = b.TipoBalanco.ToString(),
            AtivoCirculante      = SomarPrefixo("1.01", GrupoContaPrincipal.Ativo),
            AtivoNaoCirculante   = SomarPrefixo("1.02", GrupoContaPrincipal.Ativo),
            PassivoCirculante    = SomarPrefixo("2.01", GrupoContaPrincipal.Passivo),
            PassivoNaoCirculante = SomarPrefixo("2.02", GrupoContaPrincipal.Passivo),
            PatrimonioLiquido    = SomarPrefixo("2.03", GrupoContaPrincipal.PatrimonioLiquido),
            Disponibilidades     = SomarPrefixo("1.01.01", GrupoContaPrincipal.Ativo),
            Estoques             = SomarPrefixo("1.01.04", GrupoContaPrincipal.Ativo),
            RealizavelLongoPrazo = SomarPrefixo("1.02.01", GrupoContaPrincipal.Ativo)
        };
    }

    public static IndicadoresBalanco CalcularIndicadores(EvolucaoBalancoPonto p, Dre? dre = null)
    {
        decimal? Div(decimal num, decimal den) => den != 0 ? num / den : null;

        var ind = new IndicadoresBalanco
        {
            Ano = p.Ano,
            LiquidezCorrente  = Div(p.AtivoCirculante, p.PassivoCirculante),
            LiquidezSeca      = Div(p.AtivoCirculante - p.Estoques, p.PassivoCirculante),
            LiquidezImediata  = Div(p.Disponibilidades, p.PassivoCirculante),
            LiquidezGeral     = Div(p.AtivoCirculante + p.RealizavelLongoPrazo,
                                    p.PassivoCirculante + p.PassivoNaoCirculante),
            CapitalCirculanteLiquido = p.CapitalCirculanteLiquido,

            EndividamentoGeral      = Div(p.PassivoTotal, p.PassivoMaisPL),
            ComposicaoEndividamento = Div(p.PassivoCirculante, p.PassivoTotal),
            ParticipacaoCapitalTerceiros = Div(p.PassivoTotal, p.PatrimonioLiquido),
            ImobilizacaoPL          = Div(p.AtivoNaoCirculante, p.PatrimonioLiquido),
            ImobilizacaoRecursosNaoCorrentes = Div(p.AtivoNaoCirculante,
                                    p.PatrimonioLiquido + p.PassivoNaoCirculante)
        };

        // Rentabilidade (só quando há DRE do ano)
        if (dre is not null && dre.TemDados)
        {
            ind.TemDre = true;
            ind.MargemBruta       = Div(dre.LucroBruto, dre.ReceitaLiquida);
            ind.MargemOperacional = Div(dre.ResultadoOperacional, dre.ReceitaLiquida);
            ind.MargemLiquida     = Div(dre.LucroLiquido, dre.ReceitaLiquida);
            ind.RetornoSobrePL    = Div(dre.LucroLiquido, p.PatrimonioLiquido);
            ind.RetornoSobreAtivo = Div(dre.LucroLiquido, p.AtivoTotal);
            ind.GiroAtivo         = Div(dre.ReceitaLiquida, p.AtivoTotal);
            ind.CoberturaJuros    = Div(dre.ResultadoOperacional, dre.DespesasFinanceiras);
        }

        return ind;
    }

    public static List<AnaliseVerticalItem> CalcularAnaliseVertical(EvolucaoBalancoPonto p)
    {
        decimal? Pct(decimal v, decimal total) => total != 0 ? v / total * 100m : null;
        var ativo = p.AtivoTotal;
        var passPl = p.PassivoMaisPL;

        return new List<AnaliseVerticalItem>
        {
            new("Ativo Circulante",      p.AtivoCirculante,      Pct(p.AtivoCirculante, ativo)),
            new("Ativo Não Circulante",  p.AtivoNaoCirculante,   Pct(p.AtivoNaoCirculante, ativo)),
            new("Passivo Circulante",    p.PassivoCirculante,    Pct(p.PassivoCirculante, passPl)),
            new("Passivo Não Circulante",p.PassivoNaoCirculante, Pct(p.PassivoNaoCirculante, passPl)),
            new("Patrimônio Líquido",    p.PatrimonioLiquido,    Pct(p.PatrimonioLiquido, passPl))
        };
    }

    public static List<AnaliseHorizontalItem> CalcularAnaliseHorizontal(
        EvolucaoBalancoPonto ant, EvolucaoBalancoPonto atu)
    {
        decimal? Var(decimal a, decimal b) => a != 0 ? (b - a) / Math.Abs(a) * 100m : null;

        AnaliseHorizontalItem Item(string r, Func<EvolucaoBalancoPonto, decimal> sel)
            => new(r, sel(ant), sel(atu), Var(sel(ant), sel(atu)));

        return new List<AnaliseHorizontalItem>
        {
            Item("Ativo Circulante",       x => x.AtivoCirculante),
            Item("Ativo Não Circulante",   x => x.AtivoNaoCirculante),
            Item("Ativo Total",            x => x.AtivoTotal),
            Item("Passivo Circulante",     x => x.PassivoCirculante),
            Item("Passivo Não Circulante", x => x.PassivoNaoCirculante),
            Item("Patrimônio Líquido",     x => x.PatrimonioLiquido)
        };
    }

    /// <summary>
    /// Avaliação de risco de crédito baseada em regras sobre os indicadores.
    /// Determinística (não depende de IA). Score 0-100, classe A-D.
    /// </summary>
    public static AvaliacaoRisco AvaliarRisco(EvolucaoBalancoPonto p, IndicadoresBalanco ind)
    {
        var fortes = new List<string>();
        var atencao = new List<string>();
        int score = 0;

        // PL negativo é o sinal mais grave (passivo a descoberto)
        if (p.PatrimonioLiquido < 0)
            atencao.Add("Patrimônio Líquido NEGATIVO (passivo a descoberto) — situação crítica.");
        else
            score += 15;

        // Liquidez Corrente (peso 25)
        if (ind.LiquidezCorrente is decimal lc)
        {
            if (lc >= 1.5m) { score += 25; fortes.Add($"Liquidez corrente forte ({lc:N2})."); }
            else if (lc >= 1.0m) { score += 18; fortes.Add($"Liquidez corrente adequada ({lc:N2})."); }
            else if (lc >= 0.7m) { score += 8; atencao.Add($"Liquidez corrente apertada ({lc:N2})."); }
            else atencao.Add($"Liquidez corrente baixa ({lc:N2}) — dificuldade de pagar dívidas de curto prazo.");
        }

        // Liquidez Geral (peso 20)
        if (ind.LiquidezGeral is decimal lg)
        {
            if (lg >= 1.0m) { score += 20; fortes.Add($"Liquidez geral saudável ({lg:N2})."); }
            else if (lg >= 0.7m) { score += 10; atencao.Add($"Liquidez geral abaixo do ideal ({lg:N2})."); }
            else atencao.Add($"Liquidez geral baixa ({lg:N2}) — solvência de longo prazo comprometida.");
        }

        // Endividamento Geral (peso 25) — menor é melhor
        if (ind.EndividamentoGeral is decimal eg)
        {
            var egp = eg * 100m;
            if (eg <= 0.5m) { score += 25; fortes.Add($"Endividamento baixo ({egp:N0}% de capital de terceiros)."); }
            else if (eg <= 0.7m) { score += 16; fortes.Add($"Endividamento moderado ({egp:N0}%)."); }
            else if (eg <= 0.9m) { score += 7; atencao.Add($"Endividamento elevado ({egp:N0}%)."); }
            else atencao.Add($"Endividamento muito alto ({egp:N0}%) — forte dependência de terceiros.");
        }

        // Capital Circulante Líquido (peso 15)
        if (p.CapitalCirculanteLiquido > 0)
        {
            score += 15;
            fortes.Add("Capital circulante líquido positivo (folga financeira de curto prazo).");
        }
        else
        {
            atencao.Add("Capital circulante líquido negativo (passivo circulante maior que ativo circulante).");
        }

        // Rentabilidade (qualitativa, quando há DRE) — enriquece o parecer
        if (ind.TemDre)
        {
            if (ind.MargemLiquida is decimal ml)
            {
                var mlp = ml * 100m;
                if (ml > 0.10m) fortes.Add($"Margem líquida saudável ({mlp:N1}%).");
                else if (ml > 0) atencao.Add($"Margem líquida baixa ({mlp:N1}%).");
                else atencao.Add($"Resultado líquido NEGATIVO (prejuízo no exercício, margem {mlp:N1}%).");
            }
            if (ind.RetornoSobrePL is decimal roe)
            {
                var roep = roe * 100m;
                if (roe > 0.15m) fortes.Add($"ROE forte ({roep:N1}%) — bom retorno ao acionista.");
                else if (roe > 0) fortes.Add($"ROE positivo ({roep:N1}%).");
                else atencao.Add($"ROE negativo ({roep:N1}%).");
            }
            if (ind.CoberturaJuros is decimal cj && cj < 1.5m && cj > 0)
                atencao.Add($"Cobertura de juros baixa ({cj:N2}x) — o resultado operacional cobre pouco as despesas financeiras.");
        }

        score = Math.Clamp(score, 0, 100);

        var (classe, nivel) = score switch
        {
            >= 80 => ("A", "Risco baixo"),
            >= 60 => ("B", "Risco médio-baixo"),
            >= 40 => ("C", "Risco médio-alto"),
            _     => ("D", "Risco alto")
        };

        var parecer =
            $"Com base no balanço de {p.Ano}, a empresa apresenta classificação {classe} " +
            $"({nivel.ToLower()}), com pontuação {score}/100. " +
            (fortes.Count > 0 ? "Destacam-se: " + string.Join(" ", fortes) + " " : "") +
            (atencao.Count > 0 ? "Pontos de atenção: " + string.Join(" ", atencao) : "Sem pontos de atenção relevantes.");

        return new AvaliacaoRisco
        {
            Score = score,
            Classe = classe,
            NivelRisco = nivel,
            Parecer = parecer,
            PontosFortes = fortes,
            PontosAtencao = atencao
        };
    }
}
