using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Services;

public interface ICvmMockService
{
    Task<ResultadoOperacao<AnaliseAutomaticaResultado>> BuscarHistoricoB3Async(string ticker);
}

public class CvmMockService : ICvmMockService
{
    public async Task<ResultadoOperacao<AnaliseAutomaticaResultado>> BuscarHistoricoB3Async(string ticker)
    {
        await Task.Delay(1500); // mock network delay

        ticker = ticker.ToUpperInvariant();
        if (ticker != "PETR4" && ticker != "VALE3" && ticker != "ITUB4")
        {
            return ResultadoOperacao<AnaliseAutomaticaResultado>.Falha("Ticker não encontrado no mock. Tente PETR4, VALE3 ou ITUB4.");
        }

        var res = new AnaliseAutomaticaResultado
        {
            NomeArquivo = $"B3_{ticker}.json",
            RazaoSocial = ticker == "PETR4" ? "Petróleo Brasileiro S.A. - Petrobras" : 
                          ticker == "VALE3" ? "Vale S.A." : "Itaú Unibanco Holding S.A.",
            Cnpj = "00.000.000/0001-91",
            TipoEmpresa = TipoEmpresa.Outro,
            UfAtuacao = "RJ"
        };

        // Gerar 3 anos de histórico
        int anoAtual = DateTime.Now.Year - 1;
        for (int i = 2; i >= 0; i--)
        {
            var p = new PeriodoDetectado
            {
                Ano = anoAtual - i,
                Tipo = TipoBalanco.Consolidado,
                DreReceitaLiquida = 100000m + (i * 10000),
                DreLucroLiquido = 20000m + (i * 5000)
            };
            // Contas mockadas (ID da conta padrao -> valor)
            // 1: Caixa e equiv
            p.ContasMapeadas[1] = 50000m + (i * 2000);
            
            res.Periodos.Add(p);
        }

        return ResultadoOperacao<AnaliseAutomaticaResultado>.Ok(res);
    }
}
