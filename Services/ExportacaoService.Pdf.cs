using System.Globalization;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
// Aliases para resolver conflito entre Microsoft.Maui.Graphics.Colors e QuestPDF.Helpers.Colors.
// Sem isso o compilador reclama de "ambiguous reference".
using QuestColors = QuestPDF.Helpers.Colors;
using QuestFonts = QuestPDF.Helpers.Fonts;
using QuestPageSizes = QuestPDF.Helpers.PageSizes;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Gerador da exportação PDF — relatório executivo em A4 retrato.
///
/// Layout:
///   - Cabeçalho azul corporativo com razão social + CNPJ
///   - Bloco "Identificação": tipo, rating, limite, UF, grupo, setor
///   - Bloco "Resumo Executivo": Ativo Total, Passivo+PL, Diferença, PL
///   - Tabela hierárquica com todas as contas
///   - Rodapé com info de planilhamento e numeração de páginas
///
/// Padrão de código defensivo:
///   - Bold() nunca recebe argumento (não tem overload em QuestPDF)
///   - Text(action) retorna void, então estilos vão DENTRO da action via Span
///   - DefaultTextStyle dentro de Text(action) aplica estilo padrão aos spans
/// </summary>
public partial class ExportacaoService
{
    private static void GerarPdf(Balanco b, Empresa e, List<ContaPadrao> planoContas, string caminho)
    {
        var azul = QuestColors.Blue.Darken4;
        var azulClaro = QuestColors.Blue.Lighten4;
        var cinzaClaro = QuestColors.Grey.Lighten4;

        var cultura = new CultureInfo("pt-BR");
        var contasOrdenadas = MontarHierarquiaParaExport(b, planoContas);

        // Calcula totais pro resumo executivo
        var ativoTotal = b.Contas
            .Where(c => c.ContaPadrao is not null
                     && c.ContaPadrao.GrupoPrincipal == GrupoContaPrincipal.Ativo
                     && !c.ContaPadrao.EhTotalizadora)
            .Sum(c => c.Valor);

        var passivoTotal = b.Contas
            .Where(c => c.ContaPadrao is not null
                     && c.ContaPadrao.GrupoPrincipal == GrupoContaPrincipal.Passivo
                     && !c.ContaPadrao.EhTotalizadora)
            .Sum(c => c.Valor);

        var patrimonioLiquido = b.Contas
            .Where(c => c.ContaPadrao is not null
                     && c.ContaPadrao.GrupoPrincipal == GrupoContaPrincipal.PatrimonioLiquido
                     && !c.ContaPadrao.EhTotalizadora)
            .Sum(c => c.Valor);

        var diferenca = ativoTotal - (passivoTotal + patrimonioLiquido);
        var balanceado = Math.Abs(diferenca) <= 1.00m;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(10).FontFamily(QuestFonts.Calibri));

                // ═════ HEADER ═════
                page.Header().Background(azul).Padding(12).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("BALANÇO PATRIMONIAL").FontSize(18).Bold().FontColor(QuestColors.White);
                        c.Item().Text(e.RazaoSocial).FontSize(13).FontColor(QuestColors.White);
                        c.Item().Text($"CNPJ {FormatarCnpj(e.Cnpj)}").FontSize(10).FontColor(QuestColors.Grey.Lighten3);
                    });

                    row.ConstantItem(140).AlignRight().Column(c =>
                    {
                        c.Item().Text($"Exercício {b.AnoExercicio}").FontSize(14).Bold().FontColor(QuestColors.White);
                        c.Item().Text(b.TipoBalanco.ToString()).FontSize(11).FontColor(QuestColors.Grey.Lighten3);
                        c.Item().Text(b.DataReferencia.ToString("dd/MM/yyyy")).FontSize(10).FontColor(QuestColors.Grey.Lighten3);
                    });
                });

                // ═════ CONTENT ═════
                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(14);

                    // Bloco: Identificação (fundo cinza-claro)
                    col.Item().Background(cinzaClaro).Padding(10).Column(cc =>
                    {
                        cc.Item().Text("Identificação").Bold().FontSize(11).FontColor(azul);
                        cc.Item().PaddingTop(6).Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                            });

                            LinhaInfo(t, "Tipo da empresa:", e.TipoEmpresa.ToString());
                            LinhaInfo(t, "Rating:",         e.Rating ?? "—");
                            LinhaInfo(t, "Limite crédito:", e.LimiteCredito?.ToString("C2", cultura) ?? "—");
                            LinhaInfo(t, "UF de atuação:",  e.UfAtuacao ?? "—");

                            if (e.GrupoEconomico is not null)
                                LinhaInfo(t, "Grupo econômico:", e.GrupoEconomico.Nome);

                            if (e.Setores.Count > 0)
                                LinhaInfo(t, "Setor principal:", e.Setores[0].Nome);
                        });
                    });

                    // Bloco: Resumo Executivo
                    col.Item().Border(1).BorderColor(azulClaro).Padding(10).Column(cc =>
                    {
                        cc.Item().Text("Resumo Executivo").Bold().FontSize(11).FontColor(azul);
                        cc.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("ATIVO TOTAL").FontSize(9).FontColor(QuestColors.Grey.Darken1);
                                c.Item().Text("R$ " + ativoTotal.ToString("N2", cultura)).FontSize(13).Bold();
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("PASSIVO + PL").FontSize(9).FontColor(QuestColors.Grey.Darken1);
                                c.Item().Text("R$ " + (passivoTotal + patrimonioLiquido).ToString("N2", cultura)).FontSize(13).Bold();
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("DIFERENÇA").FontSize(9).FontColor(QuestColors.Grey.Darken1);
                                c.Item().Text("R$ " + diferenca.ToString("N2", cultura))
                                    .FontSize(13).Bold()
                                    .FontColor(balanceado ? QuestColors.Green.Darken2 : QuestColors.Red.Darken2);
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("PATRIMÔNIO LÍQUIDO").FontSize(9).FontColor(QuestColors.Grey.Darken1);
                                c.Item().Text("R$ " + patrimonioLiquido.ToString("N2", cultura)).FontSize(13).Bold();
                            });
                        });

                        if (!balanceado)
                        {
                            cc.Item().PaddingTop(6)
                              .Text("Balanço não fecha — Ativo Total deve ser igual a Passivo + PL.")
                              .FontSize(9).Italic().FontColor(QuestColors.Red.Darken2);
                        }
                    });

                    // Bloco: Tabela do Balanço
                    col.Item().Text("Detalhamento por Conta").Bold().FontSize(11).FontColor(azul);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(70);
                            c.RelativeColumn();
                            c.ConstantColumn(100);
                        });

                        // Cabeçalho da tabela
                        table.Header(h =>
                        {
                            h.Cell().Background(azul).Padding(5).Text("Código")
                                .FontColor(QuestColors.White).Bold().FontSize(9);
                            h.Cell().Background(azul).Padding(5).Text("Descrição")
                                .FontColor(QuestColors.White).Bold().FontSize(9);
                            h.Cell().Background(azul).Padding(5).AlignRight().Text("Valor (R$)")
                                .FontColor(QuestColors.White).Bold().FontSize(9);
                        });

                        // Linhas — usa Text(lambda) com Span pra suportar Bold condicional
                        // (Bold() não aceita argumento; chamamos só se for totalizadora)
                        bool zebra = false;
                        foreach (var c in contasOrdenadas)
                        {
                            var fundo = c.EhTotalizadora ? azulClaro : (zebra ? cinzaClaro : QuestColors.White);
                            var fonteBold = c.EhTotalizadora;

                            // Célula: Código
                            table.Cell().Background(fundo).Padding(4).Text(t =>
                            {
                                var s = t.Span(c.Codigo).FontSize(9);
                                if (fonteBold) s.Bold();
                            });

                            // Célula: Descrição (com indentação por nível)
                            table.Cell().Background(fundo).Padding(4)
                                .PaddingLeft((c.Nivel - 1) * 12)
                                .Text(t =>
                                {
                                    var s = t.Span(c.Descricao).FontSize(9);
                                    if (fonteBold) s.Bold();
                                });

                            // Célula: Valor
                            table.Cell().Background(fundo).Padding(4).AlignRight().Text(t =>
                            {
                                var s = t.Span(c.Valor.ToString("N2", cultura)).FontSize(9);
                                if (fonteBold) s.Bold();
                            });

                            zebra = !zebra;
                        }
                    });
                });

                // ═════ FOOTER ═════
                // Importante: como Text(lambda) retorna void, todo estilo é aplicado
                // DENTRO da lambda via DefaultTextStyle ou em cada Span.
                page.Footer().BorderTop(1).BorderColor(QuestColors.Grey.Lighten2)
                    .PaddingTop(6).Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(8).FontColor(QuestColors.Grey.Darken1));
                        t.Span("Planilhado em ");
                        t.Span(b.DataPlanilhamento.ToString("dd/MM/yyyy HH:mm")).Bold();
                        t.Span("— origem: ");
                        t.Span(b.Origem ?? "Manual").Bold();
                    });

                    r.AutoItem().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(8).FontColor(QuestColors.Grey.Darken1));
                        t.Span("Página ");
                        t.CurrentPageNumber();
                        t.Span("de ");
                        t.TotalPages();
                    });
                });
            });
        }).GeneratePdf(caminho);
    }

    /// <summary>
    /// Wrapper para adicionar label+valor numa tabela de 4 colunas.
    /// </summary>
    private static void LinhaInfo(TableDescriptor t, string label, string valor)
    {
        t.Cell().Padding(2).Text(label).FontSize(9).FontColor(QuestColors.Grey.Darken2);
        t.Cell().Padding(2).Text(valor).FontSize(9).Bold();
    }
}
