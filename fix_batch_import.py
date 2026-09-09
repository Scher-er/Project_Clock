import re

with open('Views/PlanilhamentoPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# Replace ImportarComIaAutomaticoAsync
import_ai = '''    private async Task ImportarComIaAutomaticoAsync()
    {
        if (!await _controller.IaConfiguradaAsync())
        {
            await DisplayAlert("Gemini não configurado",
                "Configure a API key do Google na 'Área de Testes' antes de usar a importação por IA.",
                "OK");
            return;
        }

        var arquivos = await SelecionarPdfsAsync();
        if (arquivos is null || !arquivos.Any()) return;

        var arquivosList = arquivos.ToList();
        
        lblSubtitulo.Text = $"Analisando {arquivosList.Count} arquivos via Gemini em lote...";
        _viewModel.IsBusy = true;
        
        try
        {
            var tasks = new List<Task<ResultadoOperacao<AnaliseAutomaticaResultado>>>();
            
            foreach (var arq in arquivosList)
            {
                // We must copy to MemoryStream because OpenReadAsync stream cannot be reused easily 
                // and we need to pass a valid stream to AnalisarAutomaticoAsync
                tasks.Add(ProcessarArquivoIaAsync(arq));
            }
            
            var resultados = await Task.WhenAll(tasks);
            
            int arquivosSucesso = 0;
            foreach (var r in resultados)
            {
                if (r.Sucesso && r.Dados is not null)
                {
                    arquivosSucesso++;
                    // Para cada arquivo, extrai a empresa e os dados (igual ao ExecutarImportacaoIaAsync)
                    var dados = r.Dados;
                    
                    // Se empresa não existe, tenta cadastrar. 
                    // (Simplificado: assumindo que o 1º balanço define a empresa na tela)
                    if (_viewModel.EmpresaSelecionada == null && dados.EmpresaEncontrada != null)
                    {
                        var e = dados.EmpresaEncontrada;
                        var busca = await _controller.BuscarOuCadastrarEmpresaAsync(e.RazaoSocial, e.Cnpj, TipoEmpresa.NaoListada, e.Uf);
                        if (busca.Sucesso && busca.Dados != null)
                        {
                            DefinirEmpresaSelecionada(busca.Dados);
                        }
                    }
                    
                    // Adiciona os períodos e preenche as células
                    for (int i = 0; i < dados.Periodos.Count; i++)
                    {
                        var pDados = dados.Periodos[i];
                        _viewModel.AdicionarPeriodo(pDados.Ano, pDados.MesReferencia, pDados.Tipo);
                        
                        // O período adicionado fica no final da lista
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
                }
            }
            
            await DisplayAlert("Importação em Lote Concluída", $"{arquivosSucesso} de {arquivosList.Count} arquivos foram importados com sucesso.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Falha ao processar PDFs: {ex.Message}", "OK");
        }
        finally
        {
            _viewModel.IsBusy = false;
            _viewModel.AtualizarSubtitulo();
            _viewModel.AtualizarItensTabela(); // refresh UI collection
        }
    }

    private async Task<ResultadoOperacao<AnaliseAutomaticaResultado>> ProcessarArquivoIaAsync(FileResult arquivo)
    {
        await using var stream = await arquivo.OpenReadAsync();
        using var mem = new MemoryStream();
        await stream.CopyToAsync(mem);
        mem.Position = 0;
        
        var iaService = App.Services.GetRequiredService<IPdfAiAnalyzerService>();
        return await iaService.AnalisarAutomaticoAsync(mem, arquivo.FileName);
    }
'''

# We need to replace the old ImportarComIaAutomaticoAsync and ExecutarImportacaoIaAsync.
# Let's regex it
pattern = r'private async Task ImportarComIaAutomaticoAsync\(\).*?private async Task<IEnumerable<FileResult>> SelecionarPdfsAsync\(\)'
code = re.sub(pattern, import_ai + '\n\n    private async Task<IEnumerable<FileResult>> SelecionarPdfsAsync()', code, flags=re.DOTALL)

with open('Views/PlanilhamentoPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
