import re

with open('Views/PlanilhamentoPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

method = '''    private async void OnImportarB3Clicado(object sender, EventArgs e)
    {
        string ticker = await DisplayPromptAsync("Importação B3", "Digite o ticker da ação (ex: PETR4, VALE3, ITUB4):", "Buscar", "Cancelar", "PETR4");
        if (string.IsNullOrWhiteSpace(ticker)) return;

        _viewModel.IsBusy = true;
        lblSubtitulo.Text = $"Buscando histórico de {ticker} na B3...";

        try
        {
            var r = await _controller.ImportarTickerB3Async(ticker);
            if (!r.Sucesso || r.Dados is null)
            {
                await DisplayAlert("Erro", r.Mensagem, "OK");
                return;
            }

            var dados = r.Dados;
            
            if (_viewModel.EmpresaSelecionada == null && dados.TemEmpresa)
            {
                var busca = await _controller.BuscarOuCadastrarEmpresaAsync(
                    dados.RazaoSocial, 
                    dados.Cnpj, 
                    TipoEmpresa.SABerta, 
                    dados.UfAtuacao);
                    
                if (busca.Sucesso && busca.Dados != null)
                {
                    DefinirEmpresaSelecionada(busca.Dados);
                }
            }
            
            for (int i = 0; i < dados.Periodos.Count; i++)
            {
                var pDados = dados.Periodos[i];
                _viewModel.Periodos.Add(new PeriodoPlanilhado
                {
                    Ano = pDados.Ano,
                    Mes = pDados.Mes,
                    Tipo = pDados.Tipo
                });
                
                int perIdx = _viewModel.Periodos.Count - 1;
                
                foreach (var (contaId, valor) in pDados.ContasMapeadas)
                {
                    var linha = _viewModel.Linhas.FirstOrDefault(l => l.Conta.Id == contaId);
                    if (linha is not null && linha.EhEditavel)
                    {
                        linha.ObterCelula(perIdx).Valor = valor;
                    }
                }
            }
            
            await DisplayAlert("Importação Concluída", $"Histórico de {ticker} importado com sucesso da CVM/B3.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Falha ao buscar na B3: {ex.Message}", "OK");
        }
        finally
        {
            _viewModel.IsBusy = false;
            _viewModel.AtualizarSubtitulo();
            _viewModel.AtualizarItensTabela();
            AtualizarChips();
            AtualizarKPIs();
        }
    }
'''

# Find OnImportarPdfComIaClicado and add after it
code = code.replace('private async void OnImportarPdfComIaClicado(object? sender, EventArgs e)\n        => await ImportarComIaAutomaticoAsync();', 'private async void OnImportarPdfComIaClicado(object? sender, EventArgs e)\n        => await ImportarComIaAutomaticoAsync();\n\n' + method)

with open('Views/PlanilhamentoPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
