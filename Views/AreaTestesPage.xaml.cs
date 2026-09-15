using System.Globalization;
using System.Text;
using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.DAO;
using BalancoPatrimonial.App.DAO.Mongo;
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
    private readonly IAiSettingsService _aiSettings;
    private readonly IPdfAiAnalyzerService _aiAnalyzer;
    private readonly IConfiguracaoLocalService _config;
    private readonly StringBuilder _saida = new();
    private bool _temaIniciandoCarregamento;

    public AreaTestesPage(
        IMySqlConnectionFactory mysql,
        ISqliteConnectionFactory sqlite,
        IMongoConnectionFactory mongo,
        ILogController logController,
        IBalancoController balancoController,
        IAiSettingsService aiSettings,
        IPdfAiAnalyzerService aiAnalyzer,
        IConfiguracaoLocalService config)
    {
        InitializeComponent();
        _mysql = mysql;
        _sqlite = sqlite;
        _mongo = mongo;
        _logController = logController;
        _balancoController = balancoController;
        _aiSettings = aiSettings;
        _aiAnalyzer = aiAnalyzer;
        _config = config;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await CarregarConfiguracoesIaAsync();
            await CarregarTemaAsync();
        }
        catch { }
    }

    private async Task CarregarTemaAsync()
    {
        _temaIniciandoCarregamento = true;
        var tema = await _config.ObterTemaAsync();
        swTemaEscuro.IsToggled = tema == "escuro";
        _temaIniciandoCarregamento = false;
    }

    private async Task CarregarConfiguracoesIaAsync()
    {
        var settings = await _aiSettings.ObterAsync();

        // Não mostra a chave inteira (segurança visual) — só placeholder se já configurada
        txtApiKey.Text = string.Empty;
        txtApiKey.Placeholder = settings.Configurado
            ? "API key configurada (cole nova pra substituir)"
            : "Cole sua API key do Google AI Studio aqui";

        // Seleciona o modelo salvo (Items são estáticos no XAML). Default: o
        // primeiro da lista (gemini-3.5-flash).
        var idx = pkModeloIa.Items.IndexOf(settings.Modelo);
        pkModeloIa.SelectedIndex = idx >= 0 ? idx : 0;

        AtualizarStatusIa(settings.Configurado);
    }

    private void AtualizarStatusIa(bool configurado)
    {
        if (configurado)
        {
            lblStatusIa.Text = "● Configurado";
            lblStatusIa.TextColor = Color.FromArgb("#16A34A");
        }
        else
        {
            lblStatusIa.Text = "○ Não configurado";
            lblStatusIa.TextColor = Color.FromArgb("#9CA3AF");
        }
    }

    // ───── Testes de conexão ─────

    private async void OnTestarMySqlClicado(object? sender, EventArgs e)
    {
        Escrever("Testando MySQL...");
        try
        {
            var ok = await _mysql.TestarConexaoAsync();
            Escrever(ok
                ? "MySQL conectado com sucesso."
                : "MySQL não respondeu. Verifique se está rodando na porta 3306.");
        }
        catch (Exception ex)
        {
            Escrever($"MySQL: {ex.Message}");
        }
    }

    private async void OnTestarSqliteClicado(object? sender, EventArgs e)
    {
        Escrever("Testando SQLite...");
        try
        {
            var ok = await _sqlite.TestarConexaoAsync();
            Escrever(ok
                ? $"SQLite operacional. Arquivo em {FileSystem.AppDataDirectory}."
                : "SQLite não pôde abrir/criar o arquivo.");
        }
        catch (Exception ex)
        {
            Escrever($"SQLite: {ex.Message}");
        }
    }

    private async void OnTestarMongoClicado(object? sender, EventArgs e)
    {
        Escrever("Testando MongoDB...");
        try
        {
            var ok = await _mongo.TestarConexaoAsync();
            Escrever(ok
                ? "MongoDB conectado com sucesso."
                : "MongoDB não respondeu. Verifique se está rodando na porta 27017.");
        }
        catch (Exception ex)
        {
            Escrever($"MongoDB: {ex.Message}");
        }
    }

    // ───── Logs de demonstração ─────

    private async void OnGerarLogsClicado(object? sender, EventArgs e)
    {
        Escrever("Gerando 5 logs de teste...");

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
            Escrever($"5 logs registrados. Verifique na página 'Logs' (data: hoje).");
        }
        catch (Exception ex)
        {
            Escrever($"Falha ao gerar logs: {ex.Message}");
        }
    }

    // ───── Testes de balanceamento ─────

    private void OnTestarBalanceamentoFechado(object? sender, EventArgs e)
    {
        Escrever("Construindo balanço FECHADO (deve balancear)...");

        var b = ConstruirBalancoExemplo(
            ativoCirculante: 1_000_000m,
            ativoNaoCirculante: 500_000m,
            passivoCirculante: 400_000m,
            passivoNaoCirculante: 600_000m,
            patrimonioLiquido: 500_000m);

        var r = _balancoController.ValidarBalanceamento(b);
        Escrever($"Ativo Total: R$ {r.AtivoTotal:N2}");
        Escrever($"Passivo + PL: R$ {r.PassivoMaisPL:N2}");
        Escrever($"Diferença: R$ {r.Diferenca:N2}");
        Escrever(r.Balanceado ? $"{r.Mensagem}" : $"️ {r.Mensagem}");
    }

    private void OnTestarBalanceamentoAberto(object? sender, EventArgs e)
    {
        Escrever("Construindo balanço ABERTO (não deve balancear)...");

        var b = ConstruirBalancoExemplo(
            ativoCirculante: 1_000_000m,
            ativoNaoCirculante: 500_000m,
            passivoCirculante: 400_000m,
            passivoNaoCirculante: 600_000m,
            patrimonioLiquido: 200_000m);  // ← faltam 300 mil pra fechar

        var r = _balancoController.ValidarBalanceamento(b);
        Escrever($"Ativo Total: R$ {r.AtivoTotal:N2}");
        Escrever($"Passivo + PL: R$ {r.PassivoMaisPL:N2}");
        Escrever($"Diferença: R$ {r.Diferenca:N2}");
        Escrever(r.Balanceado ? $"{r.Mensagem}" : $"️ {r.Mensagem}");
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

    // ───── Configuração da Gemini ─────

    private async void OnSalvarIaClicado(object? sender, EventArgs e)
    {
        var modelo = pkModeloIa.SelectedItem as string ?? "gemini-3.5-flash";

        // Se a textbox está vazia e já tinha config, mantém a key existente
        var atual = await _aiSettings.ObterAsync();
        var novaKey = string.IsNullOrWhiteSpace(txtApiKey.Text) ? atual.ApiKey : txtApiKey.Text.Trim();

        if (string.IsNullOrWhiteSpace(novaKey))
        {
            await DisplayAlert("Atenção",
                "Cole sua API key do Google AI Studio antes de salvar.", "OK");
            return;
        }

        await _aiSettings.SalvarAsync(new AiSettings
        {
            ApiKey = novaKey,
            Modelo = modelo
        });

        txtApiKey.Text = string.Empty;
        await CarregarConfiguracoesIaAsync();
        Escrever($"Configuração do Gemini salva. Modelo: {modelo}");
    }

    private async void OnTestarIaClicado(object? sender, EventArgs e)
    {
        Escrever("Testando conexão com Google Gemini...");
        var r = await _aiAnalyzer.TestarConexaoAsync();
        if (r.Sucesso)
        {
            Escrever($"{r.Dados ?? r.Mensagem}");
        }
        else
        {
            Escrever($"{r.Mensagem}");
        }
    }

    private async void OnLimparIaClicado(object? sender, EventArgs e)
    {
        var confirma = await DisplayAlert(
            "Limpar API key",
            "Tem certeza? A chave criptografada será removida do dispositivo.",
            "Remover", "Cancelar");
        if (!confirma) return;

        await _aiSettings.LimparApiKeyAsync();
        await CarregarConfiguracoesIaAsync();
        Escrever("️ API key removida do SecureStorage.");
    }

    // ───── Simulação de planilhamento ─────

    private async void OnSimularPlanilhamentoClicado(object? sender, EventArgs e)
    {
        _saida.Clear();
        Escrever("Iniciando simulação de planilhamento...");
        Escrever("");

        // Etapa 1: monta plano de contas mock
        Escrever("ETAPA 1: Plano de contas (8 contas analíticas)");
        var planoMock = new[]
        {
            ("1.01.01", "Caixa e Equivalentes",       GrupoContaPrincipal.Ativo,             900_000m),
            ("1.01.02", "Contas a Receber",           GrupoContaPrincipal.Ativo,             400_000m),
            ("1.02.01", "Imobilizado",                GrupoContaPrincipal.Ativo,             800_000m),
            ("1.02.02", "Investimentos",              GrupoContaPrincipal.Ativo,             100_000m),
            ("2.01.01", "Fornecedores",               GrupoContaPrincipal.Passivo,           300_000m),
            ("2.02.01", "Empréstimos LP",             GrupoContaPrincipal.Passivo,           600_000m),
            ("2.03.01", "Capital Social",             GrupoContaPrincipal.PatrimonioLiquido, 800_000m),
            ("2.03.02", "Lucros Acumulados",          GrupoContaPrincipal.PatrimonioLiquido, 500_000m),
        };

        foreach (var (cod, desc, _, _) in planoMock)
        {
            Escrever($"• [{cod}] {desc}");
        }

        await Task.Delay(400);  // delay artificial pra dar sensação de execução

        // Etapa 2: preenche valores
        Escrever("");
        Escrever("️ ETAPA 2: Usuário preenche valores");
        var cultura = new CultureInfo("pt-BR");
        foreach (var (cod, desc, _, valor) in planoMock)
        {
            Escrever($"{cod} {desc.PadRight(25)} R$ {valor.ToString("N2", cultura).PadLeft(15)}");
        }

        await Task.Delay(400);

        // Etapa 3: totalizadoras calculadas
        Escrever("");
        Escrever("ETAPA 3: Cálculo de totalizadoras (bottom-up)");
        var ativoCirc = planoMock.Where(c => c.Item1.StartsWith("1.01")).Sum(c => c.Item4);
        var ativoNaoCirc = planoMock.Where(c => c.Item1.StartsWith("1.02")).Sum(c => c.Item4);
        var passivoCirc = planoMock.Where(c => c.Item1.StartsWith("2.01")).Sum(c => c.Item4);
        var passivoNaoCirc = planoMock.Where(c => c.Item1.StartsWith("2.02")).Sum(c => c.Item4);
        var pl = planoMock.Where(c => c.Item1.StartsWith("2.03")).Sum(c => c.Item4);
        var ativoTotal = ativoCirc + ativoNaoCirc;
        var passivoTotal = passivoCirc + passivoNaoCirc;
        var passivoMaisPl = passivoTotal + pl;

        Escrever($"1.01 Ativo Circulante R$ {ativoCirc.ToString("N2", cultura).PadLeft(15)}");
        Escrever($"1.02 Ativo Não Circulante R$ {ativoNaoCirc.ToString("N2", cultura).PadLeft(15)}");
        Escrever($"1 ATIVO TOTAL R$ {ativoTotal.ToString("N2", cultura).PadLeft(15)}");
        Escrever($"2.01 Passivo Circulante R$ {passivoCirc.ToString("N2", cultura).PadLeft(15)}");
        Escrever($"2.02 Passivo Não Circul. R$ {passivoNaoCirc.ToString("N2", cultura).PadLeft(15)}");
        Escrever($"2.03 Patrimônio Líquido R$ {pl.ToString("N2", cultura).PadLeft(15)}");
        Escrever($"2 PASSIVO + PL R$ {passivoMaisPl.ToString("N2", cultura).PadLeft(15)}");

        await Task.Delay(400);

        // Etapa 4: validação
        Escrever("");
        Escrever("ETAPA 4: Validação Ativo = Passivo + PL");
        var diferenca = ativoTotal - passivoMaisPl;
        if (Math.Abs(diferenca) <= 1m)
        {
            Escrever($"Balanço FECHA — diferença R$ {diferenca:N2}");
        }
        else
        {
            Escrever($"Balanço NÃO FECHA — diferença R$ {diferenca:N2}");
        }

        await Task.Delay(400);

        // Etapa 5: indicadores
        Escrever("");
        Escrever("ETAPA 5: Indicadores financeiros");
        var liq = passivoCirc > 0 ? ativoCirc / passivoCirc : 0;
        var endiv = passivoMaisPl > 0 ? passivoTotal / passivoMaisPl : 0;
        var compEndiv = passivoTotal > 0 ? passivoCirc / passivoTotal : 0;
        var imobPL = pl > 0 ? ativoNaoCirc / pl : 0;

        Escrever($"Liquidez Corrente: {liq:F2} (>= 1,5 = saudável)");
        Escrever($"Endividamento Geral: {endiv:P1}");
        Escrever($"Composição Endividamento: {compEndiv:P1} ← % de dívida no curto prazo");
        Escrever($"Imobilização do PL: {imobPL:P1}");

        // Etapa 6: fluxo MVC
        Escrever("");
        Escrever("️ ETAPA 6: Fluxo MVC que aconteceria no app real:");
        Escrever("View (PlanilhamentoPage)");
        Escrever("→ IPlanilhamentoController.SalvarAsync(balanco)");
        Escrever("→ IBalancoService.SalvarAsync(balanco)");
        Escrever("→ IBalancoDao.InserirComContasAsync(balanco) [transacional]");
        Escrever("→ MySQL: INSERT INTO balanco + N x conta_balanco");
        Escrever("→ ILogService.RegistrarAsync(Inclusao)");
        Escrever("→ MongoDB + TXT diário (logs/YYYY-MM-DD.txt)");

        Escrever("");
        Escrever("Simulação concluída — nenhum dado foi persistido.");
    }

    // ───── Tema escuro ─────

    private async void OnTemaToggled(object? sender, ToggledEventArgs e)
    {
        if (_temaIniciandoCarregamento) return;

        var novoTema = e.Value ? "escuro" : "claro";
        await _config.DefinirTemaAsync(novoTema);

        // Aplica imediatamente
        if (Application.Current is not null)
        {
            Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
        }

        Escrever($"Tema alterado para: {novoTema}");
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
