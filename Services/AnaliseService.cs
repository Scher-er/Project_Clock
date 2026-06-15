using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Cálculos de análise financeira a partir dos balanços planilhados:
/// indicadores de liquidez/endividamento/estrutura, análise vertical e
/// horizontal, e uma avaliação de risco de crédito (score + parecer).
///
/// Soma das contas analíticas por prefixo de código (padrão CVM/CPC):
///   1.01 = Ativo Circulante      1.01.01 = Caixa/Equivalentes  1.01.04 = Estoques
///   1.02 = Ativo Não Circulante  1.02.01 = Realizável a LP
///   2.01 = Passivo Circulante    2.02 = Passivo Não Circulante  2.03 = PL
/// </summary>
public class AnaliseService : IAnaliseService
{
    private readonly IEmpresaDao _empresaDao;
    private readonly IBalancoDao _balancoDao;
    private readonly IDreDao _dreDao;
    private readonly ILogService _log;

    public string NomeServico => "AnaliseService";

    public AnaliseService(IEmpresaDao empresaDao, IBalancoDao balancoDao, IDreDao dreDao, ILogService log)
    {
        _empresaDao = empresaDao;
        _balancoDao = balancoDao;
        _dreDao = dreDao;
        _log = log;
    }

    public async Task<ResultadoOperacao<AnaliseEmpresa>> AnalisarAsync(int empresaId)
    {
        try
        {
            var empresa = await _empresaDao.BuscarCompletaAsync(empresaId);
            if (empresa is null)
                return ResultadoOperacao<AnaliseEmpresa>.Falha("Empresa não encontrada.");

            var resultado = new AnaliseEmpresa { Empresa = empresa };

            if (empresa.Balancos.Count == 0)
                return ResultadoOperacao<AnaliseEmpresa>.Ok(resultado, "Empresa sem balanços planilhados.");

            var balancosCompletos = new List<Balanco>();
            foreach (var resumo in empresa.Balancos.OrderBy(b => b.AnoExercicio))
            {
                var completo = await _balancoDao.BuscarCompletoAsync(resumo.Id);
                if (completo is not null) balancosCompletos.Add(completo);
            }

            // A análise não deve MISTURAR Individual e Consolidado (são visões
            // diferentes da mesma empresa). Escolhemos um tipo de referência: o do
            // balanço mais recente que tenha contas, e trabalhamos só com ele.
            var tipoReferencia = balancosCompletos
                .Where(b => b.Contas.Count > 0)
                .OrderByDescending(b => b.AnoExercicio)
                .Select(b => (TipoBalanco?)b.TipoBalanco)
                .FirstOrDefault()
                ?? balancosCompletos.OrderByDescending(b => b.AnoExercicio).First().TipoBalanco;

            balancosCompletos = balancosCompletos
                .Where(b => b.TipoBalanco == tipoReferencia)
                .OrderBy(b => b.AnoExercicio)
                .ToList();

            resultado.TipoAnalisado = tipoReferencia.ToString();

            // DREs ativas da empresa, indexadas por (ano, tipo)
            var dres = (await _dreDao.ListarPorEmpresaAsync(empresaId))
                .ToDictionary(d => (d.AnoExercicio, d.TipoBalanco), d => d);

            // 1) Evolução + indicadores por ano
            foreach (var b in balancosCompletos)
            {
                var ponto = CalculadoraIndicadores.ConstruirPonto(b);
                resultado.Evolucao.Add(ponto);
                dres.TryGetValue((b.AnoExercicio, b.TipoBalanco), out var dreDoAno);
                resultado.Indicadores.Add(CalculadoraIndicadores.CalcularIndicadores(ponto, dreDoAno));
            }

            var ultimo = resultado.Evolucao.LastOrDefault(p => p.AtivoTotal > 0) ?? resultado.Evolucao.LastOrDefault();
            if (ultimo is not null)
            {
                // 2) Composição (pizza)
                resultado.ComposicaoAtivo = new ComposicaoBalanco
                {
                    Titulo = $"Composição do Ativo ({ultimo.Ano})",
                    Fatias = new()
                    {
                        new("Ativo Circulante",     ultimo.AtivoCirculante),
                        new("Ativo Não Circulante", ultimo.AtivoNaoCirculante)
                    }
                };
                resultado.ComposicaoPassivoPL = new ComposicaoBalanco
                {
                    Titulo = $"Origem dos Recursos ({ultimo.Ano})",
                    Fatias = new()
                    {
                        new("Passivo Circulante",     ultimo.PassivoCirculante),
                        new("Passivo Não Circulante", ultimo.PassivoNaoCirculante),
                        new("Patrimônio Líquido",     ultimo.PatrimonioLiquido)
                    }
                };

                // 3) Análise vertical do último balanço
                resultado.AnaliseVertical = CalculadoraIndicadores.CalcularAnaliseVertical(ultimo);

                // 4) Análise horizontal (dois últimos balanços)
                if (resultado.Evolucao.Count >= 2)
                    resultado.AnaliseHorizontal = CalculadoraIndicadores.CalcularAnaliseHorizontal(
                        resultado.Evolucao[^2], ultimo);

                // 5) Avaliação de risco do último ano
                resultado.Risco = CalculadoraIndicadores.AvaliarRisco(ultimo, resultado.Indicadores.Last());
            }

            return ResultadoOperacao<AnaliseEmpresa>.Ok(resultado);
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("ANALISAR_EMPRESA", ex, "Empresa");
            return ResultadoOperacao<AnaliseEmpresa>.FalhaExcecao(ex);
        }
    }

}