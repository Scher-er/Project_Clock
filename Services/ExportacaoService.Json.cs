using System.Text.Json;
using System.Text.Json.Serialization;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Gerador da exportação JSON — formato pensado pra ser consumido por outros
/// sistemas do banco (engines de risk, dashboards, data lake).
///
/// Estrutura plana, sem referências circulares, com tudo já formatado/calculado.
/// </summary>
public partial class ExportacaoService
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static async Task GerarJsonAsync(Balanco b, Empresa e, string caminho)
    {
        // Calcula totais pra incluir no JSON (poupa o consumidor de fazer o sum)
        var ativoTotal = b.Contas
            .Where(c => c.ContaPadrao?.GrupoPrincipal == GrupoContaPrincipal.Ativo
                     && !c.ContaPadrao.EhTotalizadora)
            .Sum(c => c.Valor);

        var passivoTotal = b.Contas
            .Where(c => c.ContaPadrao?.GrupoPrincipal == GrupoContaPrincipal.Passivo
                     && !c.ContaPadrao.EhTotalizadora)
            .Sum(c => c.Valor);

        var pl = b.Contas
            .Where(c => c.ContaPadrao?.GrupoPrincipal == GrupoContaPrincipal.PatrimonioLiquido
                     && !c.ContaPadrao.EhTotalizadora)
            .Sum(c => c.Valor);

        var diferenca = ativoTotal - (passivoTotal + pl);

        // Monta DTO
        var dto = new BalancoExportDto
        {
            VersaoSchema = "1.0",
            GeradoEm = DateTime.Now,
            Empresa = new EmpresaExportDto
            {
                Id = e.Id,
                Cnpj = e.Cnpj,
                RazaoSocial = e.RazaoSocial,
                NomeFantasia = e.NomeFantasia,
                TipoEmpresa = e.TipoEmpresa.ToString(),
                Rating = e.Rating,
                LimiteCredito = e.LimiteCredito,
                UfAtuacao = e.UfAtuacao,
                LocalAtuacao = e.LocalAtuacao,
                GrupoEconomico = e.GrupoEconomico?.Nome,
                Setores = e.Setores.Select(s => new SetorExportDto
                {
                    Codigo = s.Codigo,
                    Nome = s.Nome
                }).ToList()
            },
            Balanco = new BalancoDadosExportDto
            {
                Id = b.Id,
                AnoExercicio = b.AnoExercicio,
                DataReferencia = b.DataReferencia,
                Tipo = b.TipoBalanco.ToString(),
                Moeda = b.Moeda,
                MultiplicadorOriginal = b.MultiplicadorValores,
                Origem = b.Origem ?? "Manual",
                HashOrigemPdf = b.HashOrigemPdf,
                DataPlanilhamento = b.DataPlanilhamento,
                Observacoes = b.Observacoes
            },
            Resumo = new ResumoExportDto
            {
                AtivoTotal = ativoTotal,
                PassivoTotal = passivoTotal,
                PatrimonioLiquido = pl,
                PassivoMaisPl = passivoTotal + pl,
                Diferenca = diferenca,
                Balanceado = Math.Abs(diferenca) <= 1.00m,
                QtdContas = b.Contas.Count
            },
            Contas = b.Contas
                .Where(c => c.ContaPadrao is not null)
                .OrderBy(c => c.ContaPadrao!.GrupoPrincipal)
                .ThenBy(c => c.ContaPadrao!.Codigo)
                .Select(c => new ContaExportDto
                {
                    Codigo = c.ContaPadrao!.Codigo,
                    Descricao = c.ContaPadrao.Descricao,
                    Grupo = c.ContaPadrao.GrupoPrincipal.ToString(),
                    Nivel = c.ContaPadrao.Nivel,
                    EhTotalizadora = c.ContaPadrao.EhTotalizadora,
                    Valor = c.Valor,
                    DescricaoOriginal = c.DescricaoOriginal
                })
                .ToList()
        };

        var json = JsonSerializer.Serialize(dto, _jsonOpts);
        await File.WriteAllTextAsync(caminho, json);
    }

    // ───── DTOs (estrutura externa, estável; isola mudanças internas) ─────

    private class BalancoExportDto
    {
        public string VersaoSchema { get; set; } = "1.0";
        public DateTime GeradoEm { get; set; }
        public EmpresaExportDto Empresa { get; set; } = null!;
        public BalancoDadosExportDto Balanco { get; set; } = null!;
        public ResumoExportDto Resumo { get; set; } = null!;
        public List<ContaExportDto> Contas { get; set; } = new();
    }

    private class EmpresaExportDto
    {
        public int Id { get; set; }
        public string Cnpj { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;
        public string? NomeFantasia { get; set; }
        public string TipoEmpresa { get; set; } = string.Empty;
        public string? Rating { get; set; }
        public decimal? LimiteCredito { get; set; }
        public string? UfAtuacao { get; set; }
        public string? LocalAtuacao { get; set; }
        public string? GrupoEconomico { get; set; }
        public List<SetorExportDto> Setores { get; set; } = new();
    }

    private class SetorExportDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
    }

    private class BalancoDadosExportDto
    {
        public int Id { get; set; }
        public int AnoExercicio { get; set; }
        public DateTime DataReferencia { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Moeda { get; set; } = string.Empty;
        public int MultiplicadorOriginal { get; set; }
        public string Origem { get; set; } = string.Empty;
        public string? HashOrigemPdf { get; set; }
        public DateTime DataPlanilhamento { get; set; }
        public string? Observacoes { get; set; }
    }

    private class ResumoExportDto
    {
        public decimal AtivoTotal { get; set; }
        public decimal PassivoTotal { get; set; }
        public decimal PatrimonioLiquido { get; set; }
        public decimal PassivoMaisPl { get; set; }
        public decimal Diferenca { get; set; }
        public bool Balanceado { get; set; }
        public int QtdContas { get; set; }
    }

    private class ContaExportDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Grupo { get; set; } = string.Empty;
        public int Nivel { get; set; }
        public bool EhTotalizadora { get; set; }
        public decimal Valor { get; set; }
        public string? DescricaoOriginal { get; set; }
    }
}
