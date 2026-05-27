using System.Text;
using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.DAO;
using BalancoPatrimonial.App.DAO.MongoDB;
using BalancoPatrimonial.App.DAO.MySQL;
using BalancoPatrimonial.App.DAO.SQLite;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using BalancoPatrimonial.App.Services;

namespace BalancoPatrimonial.App.Views;

/// <summary>
/// Página de diagnóstico. Permite verificar se o ambiente está configurado:
///   - Conexão com cada um dos 3 bancos
///   - Logs sendo escritos corretamente (Mongo + TXT)
///   - Lógica de balanceamento do BalancoService funcionando
///
/// Útil pro avaliador do trabalho rodar testes rápidos sem precisar
/// preencher a tela inteira de planilhamento.
/// </summary>
public partial class AreaTestesPage : ContentPage
{
    private readonly IMySqlConnectionFactory _mysql;
    private readonly ISqliteConnectionFactory _sqlite;
    private readonly IMongoConnectionFactory _mongo;
    private readonly ILogController _logController;
    private readonly IBalancoController _balancoController;
    private readonly StringBuilder _saida = new();

    public AreaTestesPage(
        IMySqlConnectionFactory mysql,
        ISqliteConnectionFactory sqlite,
        IMongoConnectionFactory mongo,
        ILogController logController,
        IBalancoController balancoController)
    {
        InitializeComponent();
        _mysql = mysql;
        _sqlite = sqlite;
        _mongo = mongo;
        _logController = logController;
        _balancoController = balancoController;
    }

    // ───── Testes de conexão ─────

    private async void OnTestarMySqlClicado(object? sender, EventArgs e)
    {
        Escrever("🔌 Testando MySQL...");
        try
        {
            var ok = await _mysql.TestarConexaoAsync();
            Escrever(ok
                ? "✅ MySQL conectado com sucesso."
                : "❌ MySQL não respondeu. Verifique se está rodando na porta 3306.");
        }
        catch (Exception ex)
        {
            Escrever($"❌ MySQL: {ex.Message}");
        }
    }

    private async void OnTestarSqliteClicado(object? sender, EventArgs e)
    {
        Escrever("🔌 Testando SQLite...");
        try
        {
            var ok = await _sqlite.TestarConexaoAsync();
            Escrever(ok
                ? $"✅ SQLite operacional. Arquivo em {FileSystem.AppDataDirectory}."
                : "❌ SQLite não pôde abrir/criar o arquivo.");
        }
        catch (Exception ex)
        {
            Escrever($"❌ SQLite: {ex.Message}");
        }
    }

    private async void OnTestarMongoClicado(object? sender, EventArgs e)
    {
        Escrever("🔌 Testando MongoDB...");
        try
        {
            var ok = await _mongo.TestarConexaoAsync();
            Escrever(ok
                ? "✅ MongoDB conectado com sucesso."
                : "❌ MongoDB não respondeu. Verifique se está rodando na porta 27017.");
        }
        catch (Exception ex)
        {
            Escrever($"❌ MongoDB: {ex.Message}");
        }
    }

    // ───── Logs de demonstração ─────

    private async void OnGerarLogsClicado(object? sender, EventArgs e)
    {
        Escrever("📋 Gerando 5 logs de teste...");

        var eventos = new (TipoEventoLog tipo, string acao, string desc)[]
        {
            (TipoEventoLog.Info,      "TESTE_INFO",       "Geração de log informativo a partir da Área de Testes."),
            (TipoEventoLog.Aviso,     "TESTE_AVISO",      "Exemplo de aviso (warning) — não impede operação."),
            (TipoEventoLog.Inclusao,  "TESTE_INCLUSAO",   "Simulação de inclusão de entidade."),
            (TipoEventoLog.Alteracao, "TESTE_ALTERACAO",  "Simulação de alteração de dados."),
            (TipoEventoLog.Erro,      "TESTE_ERRO",       "Simulação de erro recuperável.")
        };

        try
        {
            foreach (var (tipo, acao, desc) in eventos)
            {
                await _logController.RegistrarAsync(tipo, acao, desc);
            }
            Escrever($"✅ 5 logs registrados. Verifique na página 'Logs' (data: hoje).");
        }
        catch (Exception ex)
        {
            Escrever($"❌ Falha ao gerar logs: {ex.Message}");
        }
    }

    // ───── Testes de balanceamento ─────

    private void OnTestarBalanceamentoFechado(object? sender, EventArgs e)
    {
        Escrever("🧮 Construindo balanço FECHADO (deve balancear)...");

        var b = ConstruirBalancoExemplo(
            ativoCirculante: 1_000_000m,
            ativoNaoCirculante: 500_000m,
            passivoCirculante: 400_000m,
            passivoNaoCirculante: 600_000m,
            patrimonioLiquido: 500_000m);

        var r = _balancoController.ValidarBalanceamento(b);
        Escrever($"   Ativo Total:    R$ {r.AtivoTotal:N2}");
        Escrever($"   Passivo + PL:   R$ {r.PassivoMaisPL:N2}");
        Escrever($"   Diferença:      R$ {r.Diferenca:N2}");
        Escrever(r.Balanceado ? $"✅ {r.Mensagem}" : $"⚠️ {r.Mensagem}");
    }

    private void OnTestarBalanceamentoAberto(object? sender, EventArgs e)
    {
        Escrever("🧮 Construindo balanço ABERTO (não deve balancear)...");

        var b = ConstruirBalancoExemplo(
            ativoCirculante: 1_000_000m,
            ativoNaoCirculante: 500_000m,
            passivoCirculante: 400_000m,
            passivoNaoCirculante: 600_000m,
            patrimonioLiquido: 200_000m);  // ← faltam 300 mil pra fechar

        var r = _balancoController.ValidarBalanceamento(b);
        Escrever($"   Ativo Total:    R$ {r.AtivoTotal:N2}");
        Escrever($"   Passivo + PL:   R$ {r.PassivoMaisPL:N2}");
        Escrever($"   Diferença:      R$ {r.Diferenca:N2}");
        Escrever(r.Balanceado ? $"✅ {r.Mensagem}" : $"⚠️ {r.Mensagem}");
    }

    private static Balanco ConstruirBalancoExemplo(
        decimal ativoCirculante,
        decimal ativoNaoCirculante,
        decimal passivoCirculante,
        decimal passivoNaoCirculante,
        decimal patrimonioLiquido)
    {
        // Construímos um balanço mockado pra testar SÓ o cálculo —
        // sem persistência. Cria contas analíticas (não totalizadoras) com
        // ContaPadrao mockada apontando pros 3 grupos principais.
        var balanco = new Balanco { Moeda = "BRL" };

        balanco.Contas.Add(new ContaBalanco
        {
            Valor = ativoCirculante,
            ContaPadrao = new ContaPadrao
            {
                GrupoPrincipal = GrupoContaPrincipal.Ativo,
                EhTotalizadora = false,
                Codigo = "1.01"
            }
        });
        balanco.Contas.Add(new ContaBalanco
        {
            Valor = ativoNaoCirculante,
            ContaPadrao = new ContaPadrao
            {
                GrupoPrincipal = GrupoContaPrincipal.Ativo,
                EhTotalizadora = false,
                Codigo = "1.02"
            }
        });
        balanco.Contas.Add(new ContaBalanco
        {
            Valor = passivoCirculante,
            ContaPadrao = new ContaPadrao
            {
                GrupoPrincipal = GrupoContaPrincipal.Passivo,
                EhTotalizadora = false,
                Codigo = "2.01"
            }
        });
        balanco.Contas.Add(new ContaBalanco
        {
            Valor = passivoNaoCirculante,
            ContaPadrao = new ContaPadrao
            {
                GrupoPrincipal = GrupoContaPrincipal.Passivo,
                EhTotalizadora = false,
                Codigo = "2.02"
            }
        });
        balanco.Contas.Add(new ContaBalanco
        {
            Valor = patrimonioLiquido,
            ContaPadrao = new ContaPadrao
            {
                GrupoPrincipal = GrupoContaPrincipal.PatrimonioLiquido,
                EhTotalizadora = false,
                Codigo = "2.03"
            }
        });

        return balanco;
    }

    // ───── helpers de output ─────

    private void Escrever(string mensagem)
    {
        var hora = DateTime.Now.ToString("HH:mm:ss");
        _saida.AppendLine($"[{hora}] {mensagem}");
        lblSaida.Text = _saida.ToString();
    }

    private void OnLimparClicado(object? sender, EventArgs e)
    {
        _saida.Clear();
        lblSaida.Text = "Aguardando teste...";
    }
}
