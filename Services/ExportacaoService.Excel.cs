using System.Globalization;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using ClosedXML.Excel;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Gerador da exportação Excel. Em arquivo separado pra deixar
/// o ExportacaoService mais legível.
/// </summary>
public partial class ExportacaoService
{
    /// <summary>
    /// Constrói um .xlsx com 2 abas:
    ///   - "Balanço": cabeçalho da empresa + tabela hierárquica
    ///   - "Metadados": info de planilhamento, origem, hash do PDF (se houver)
    /// </summary>
    private static void GerarExcel(Balanco b, Empresa e, List<ContaPadrao> planoContas, string caminho)
    {
        using var workbook = new XLWorkbook();

        // ───── Aba 1: Balanço ─────
        var ws = workbook.Worksheets.Add("Balanço");

        var azul = XLColor.FromHtml("#1F3A60");
        var azulClaro = XLColor.FromHtml("#EEF2F7");
        var cinzaClaro = XLColor.FromHtml("#F8FAFC");

        // Cabeçalho da empresa
        ws.Cell("A1").Value = "BALANÇO PATRIMONIAL";
        ws.Cell("A1").Style.Font.SetFontSize(18).Font.SetBold().Font.SetFontColor(azul);
        ws.Range("A1:D1").Merge();

        ws.Cell("A3").Value = "Empresa:";
        ws.Cell("B3").Value = e.RazaoSocial;
        ws.Cell("A4").Value = "CNPJ:";
        ws.Cell("B4").Value = FormatarCnpj(e.Cnpj);
        ws.Cell("A5").Value = "Tipo:";
        ws.Cell("B5").Value = b.TipoBalanco.ToString();
        ws.Cell("A6").Value = "Exercício:";
        ws.Cell("B6").Value = b.AnoExercicio;
        ws.Cell("A7").Value = "Data de referência:";
        ws.Cell("B7").Value = b.DataReferencia.ToString("dd/MM/yyyy");
        ws.Cell("A8").Value = "Moeda:";
        ws.Cell("B8").Value = b.Moeda;

        if (e.GrupoEconomico is not null)
        {
            ws.Cell("A9").Value = "Grupo econômico:";
            ws.Cell("B9").Value = e.GrupoEconomico.Nome;
        }

        // Estiliza os labels da esquerda (negrito)
        ws.Range("A3:A9").Style.Font.SetBold();

        // ───── Tabela hierárquica ─────
        int linhaCabecalho = 11;
        ws.Cell(linhaCabecalho, 1).Value = "Código";
        ws.Cell(linhaCabecalho, 2).Value = "Descrição";
        ws.Cell(linhaCabecalho, 3).Value = "Valor (R$)";

        var rangeCabecalho = ws.Range(linhaCabecalho, 1, linhaCabecalho, 3);
        rangeCabecalho.Style.Fill.SetBackgroundColor(azul);
        rangeCabecalho.Style.Font.SetFontColor(XLColor.White).Font.SetBold();
        rangeCabecalho.Style.Border.SetBottomBorder(XLBorderStyleValues.Medium);

        // Organiza as contas em ordem de exibição (incluindo totalizadoras
        // pra mostrar a hierarquia completa)
        var contasOrdenadas = MontarHierarquiaParaExport(b, planoContas);

        int linha = linhaCabecalho + 1;
        foreach (var c in contasOrdenadas)
        {
            ws.Cell(linha, 1).Value = c.Codigo;
            ws.Cell(linha, 2).Value = new string(' ', (c.Nivel - 1) * 3) + c.Descricao;
            ws.Cell(linha, 3).Value = c.Valor;
            ws.Cell(linha, 3).Style.NumberFormat.Format = "#,##0.00;(#,##0.00)";

            if (c.EhTotalizadora)
            {
                var rg = ws.Range(linha, 1, linha, 3);
                rg.Style.Fill.SetBackgroundColor(azulClaro);
                rg.Style.Font.SetBold();
            }
            else
            {
                ws.Range(linha, 1, linha, 3).Style.Fill.SetBackgroundColor(
                    linha % 2 == 0 ? cinzaClaro : XLColor.White);
            }
            linha++;
        }

        // Bordas finais
        var tabela = ws.Range(linhaCabecalho, 1, linha - 1, 3);
        tabela.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

        // Auto-fit colunas
        ws.Columns().AdjustToContents();
        ws.Column(2).Width = Math.Max(ws.Column(2).Width, 50);
        ws.Column(3).Width = Math.Max(ws.Column(3).Width, 20);

        // ───── Aba 2: Metadados ─────
        var meta = workbook.Worksheets.Add("Metadados");
        meta.Cell("A1").Value = "Metadados do Balanço";
        meta.Cell("A1").Style.Font.SetFontSize(14).Font.SetBold().Font.SetFontColor(azul);
        meta.Range("A1:B1").Merge();

        var infosMeta = new (string Label, object Valor)[]
        {
            ("Balanço ID",            b.Id),
            ("Empresa ID",            e.Id),
            ("Razão social",          e.RazaoSocial),
            ("CNPJ",                  FormatarCnpj(e.Cnpj)),
            ("Tipo da empresa",       e.TipoEmpresa.ToString()),
            ("Rating",                e.Rating ?? "—"),
            ("Limite de crédito",     e.LimiteCredito?.ToString("C2", new CultureInfo("pt-BR")) ?? "—"),
            ("UF de atuação",         e.UfAtuacao ?? "—"),
            ("Ano de exercício",      b.AnoExercicio),
            ("Data de referência",    b.DataReferencia.ToString("dd/MM/yyyy")),
            ("Tipo do balanço",       b.TipoBalanco.ToString()),
            ("Moeda",                 b.Moeda),
            ("Multiplicador",         b.MultiplicadorValores),
            ("Origem",                b.Origem ?? "Manual"),
            ("Hash PDF (SHA-256)",    b.HashOrigemPdf ?? "—"),
            ("Data de planilhamento", b.DataPlanilhamento.ToString("dd/MM/yyyy HH:mm")),
            ("Usuário ID",            b.UsuarioId),
            ("Observações",           b.Observacoes ?? "—"),
            ("Total de contas",       b.Contas.Count)
        };

        int rowMeta = 3;
        foreach (var (label, valor) in infosMeta)
        {
            meta.Cell(rowMeta, 1).Value = label;
            meta.Cell(rowMeta, 1).Style.Font.SetBold();
            meta.Cell(rowMeta, 2).Value = valor.ToString();
            rowMeta++;
        }
        meta.Columns().AdjustToContents();

        // Save
        workbook.SaveAs(caminho);
    }

    /// <summary>
    /// Reconstrói a hierarquia: pega TODAS as contas do plano (analíticas +
    /// totalizadoras), preenche os valores das analíticas com os do balanço,
    /// e calcula as totalizadoras como soma das filhas (bottom-up).
    /// </summary>
    private static List<ContaExport> MontarHierarquiaParaExport(Balanco b, List<ContaPadrao> planoContas)
    {
        // 1) Mapeia plano por id pra navegação por ContaPaiId
        var planoPorId = planoContas.ToDictionary(c => c.Id);

        // 2) Cria ContaExport pra cada conta do plano (zerada)
        var porId = planoContas.ToDictionary(
            c => c.Id,
            c => ContaExport.De(c, 0));

        // 3) Preenche valores das analíticas a partir do balanço
        foreach (var cb in b.Contas)
        {
            if (cb.ContaPadrao is null) continue;
            if (porId.TryGetValue(cb.ContaPadrao.Id, out var exp))
            {
                exp.Valor = cb.Valor;
            }
        }

        // 4) Soma bottom-up: pra cada analítica, propaga seu valor pelos ancestrais
        foreach (var cb in b.Contas)
        {
            if (cb.ContaPadrao is null) continue;
            var paiId = cb.ContaPadrao.ContaPaiId;
            while (paiId.HasValue)
            {
                if (porId.TryGetValue(paiId.Value, out var paiExp))
                {
                    paiExp.Valor += cb.Valor;
                }
                paiId = planoPorId.TryGetValue(paiId.Value, out var paiPlano)
                    ? paiPlano.ContaPaiId
                    : null;
            }
        }

        // 5) Ordena: grupo principal, depois pelo código (que é hierárquico natural)
        return porId.Values
            .OrderBy(c => c.GrupoPrincipal)
            .ThenBy(c => c.Codigo, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>POCO interno só pra montar a hierarquia da exportação.</summary>
    private class ContaExport
    {
        public string Codigo { get; init; } = string.Empty;
        public string Descricao { get; init; } = string.Empty;
        public int Nivel { get; init; }
        public bool EhTotalizadora { get; init; }
        public GrupoContaPrincipal GrupoPrincipal { get; init; }
        public decimal Valor { get; set; }

        public static ContaExport De(ContaPadrao c, decimal valor) => new()
        {
            Codigo = c.Codigo,
            Descricao = c.Descricao,
            Nivel = c.Nivel,
            EhTotalizadora = c.EhTotalizadora,
            GrupoPrincipal = c.GrupoPrincipal,
            Valor = valor
        };
    }

    private static string FormatarCnpj(string cnpj)
    {
        if (string.IsNullOrEmpty(cnpj) || cnpj.Length != 14) return cnpj;
        return $"{cnpj[..2]}.{cnpj[2..5]}.{cnpj[5..8]}/{cnpj[8..12]}-{cnpj[12..14]}";
    }
}
