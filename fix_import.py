# -*- coding: utf-8 -*-
import re

with open('ViewModels/PlanilhamentoViewModel.Import.cs', 'r', encoding='utf-8') as f:
    code = f.read()

new_salvar = """    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (EmpresaSelecionada == null)
        {
            MostrarMensagemAction?.Invoke("Selecione uma empresa.");
            return;
        }
        if (_sessao.UsuarioAtual == null)
        {
            MostrarMensagemAction?.Invoke("Sessão expirou. Faça login novamente.");
            return;
        }
        if (Periodos.Count == 0)
        {
            MostrarMensagemAction?.Invoke("Adicione pelo menos um período.");
            return;
        }

        var jaExistem = new List<string>();
        foreach (var per in Periodos)
        {
            if (await _controller.ExisteBalancoAsync(EmpresaSelecionada.Id, per.Ano, per.Tipo))
                jaExistem.Add(per.LabelCompleto);
        }

        bool substituir = false;
        if (jaExistem.Count > 0)
        {
            if (MostrarConfirmacaoAction != null)
            {
                substituir = await MostrarConfirmacaoAction.Invoke(
                    "Balanço já existe",
                    $"Já existe(m) balanço(s) salvo(s) para:\\n• {string.Join("\\n• ", jaExistem)}\\n\\nDeseja SUBSTITUIR?",
                    "Substituir", "Cancelar");
            }
            if (!substituir) return;
        }

        int salvos = 0;
        var erros = new List<string>();

        IsBusy = true;
        try
        {
            for (int p = 0; p < Periodos.Count; p++)
            {
                var per = Periodos[p];
                var contas = Linhas
                    .Where(l => l.EhEditavel && l.ObterCelula(p).Valor != 0)
                    .Select(l => new ContaBalanco
                    {
                        ContaPadraoId = l.Conta.Id,
                        Valor = l.ObterCelula(p).Valor
                    })
                    .ToList();

                if (contas.Count == 0)
                {
                    erros.Add($"{per.LabelCompleto}: nenhuma conta preenchida (todos os valores estão zerados).");
                    continue;
                }

                var balanco = new Balanco
                {
                    EmpresaId = EmpresaSelecionada.Id,
                    AnoExercicio = per.Ano,
                    DataReferencia = per.DataReferencia,
                    TipoBalanco = per.Tipo,
                    UsuarioId = _sessao.UsuarioAtual.Id,
                    Origem = "Manual",
                    HashOrigemPdf = null,
                    Contas = contas
                };

                var r = await _controller.SalvarAsync(balanco, substituir);
                if (r.Sucesso)
                {
                    salvos++;
                    if (DresImportadas.TryGetValue((per.Ano, per.Tipo), out var dre))
                    {
                        dre.EmpresaId = EmpresaSelecionada.Id;
                        dre.UsuarioId = _sessao.UsuarioAtual.Id;
                        await _controller.SalvarDreAsync(dre);
                    }
                }
                else
                {
                    erros.Add($"{per.LabelCompleto}: {r.Mensagem}");
                }
            }

            if (erros.Count > 0)
            {
                MostrarMensagemAction?.Invoke("Erros ao salvar:\\n" + string.Join("\\n", erros));
            }
            if (salvos > 0)
            {
                MostrarMensagemAction?.Invoke($"Salvo(s) com sucesso {salvos} balanço(s).");
                await LimparAsync();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }"""

code = re.sub(r'\[RelayCommand\]\s*private async Task SalvarAsync\(\)\s*\{.*?\}\s*\}\s*$', new_salvar + '\n}\n', code, flags=re.DOTALL)

with open('ViewModels/PlanilhamentoViewModel.Import.cs', 'w', encoding='utf-8') as f:
    f.write(code)
