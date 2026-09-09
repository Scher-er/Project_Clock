import re

with open('Views/PlanilhamentoPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

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
                tasks.Add(ProcessarArquivoIaAsync(arq));
            }
            
            var resultados = await Task.WhenAll(tasks);
            
            int arquivosSucesso = 0;
            foreach (var r in resultados)
            {
                if (r.Sucesso && r.Dados is not null)
                {
                    arquivosSucesso++;
                    var dados = r.Dados;
                    
                    if (_viewModel.EmpresaSelecionada == null && dados.TemEmpresa)
                    {
                        var busca = await _controller.BuscarOuCadastrarEmpresaAsync(
                            dados.RazaoSocial, 
                            dados.Cnpj, 
                            TipoEmpresa.Outro, 
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
                }
            }
            
            await DisplayAlert("Importação em Lote", $"{arquivosSucesso} de {arquivosList.Count} arquivos processados com sucesso.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Falha ao processar PDFs: {ex.Message}", "OK");
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

    private async Task<ResultadoOperacao<AnaliseAutomaticaResultado>> ProcessarArquivoIaAsync(FileResult arquivo)
    {
        await using var stream = await arquivo.OpenReadAsync();
        using var mem = new MemoryStream();
        await stream.CopyToAsync(mem);
        mem.Position = 0;
        return await _controller.ImportarPdfAutomaticoAsync(mem, arquivo.FileName);
    }
'''

# Find the start of old ImportarComIaAutomaticoAsync and end of ExecutarImportacaoIaAsync.
# Wait, let's just regex replace the method bodies.
pattern = r'private async Task ImportarComIaAutomaticoAsync\(\).*?private async Task ExecutarImportacaoIaAsync\(\).*?(?=private async Task<FileResult\?> SelecionarPdfAsync\(\))'
code = re.sub(pattern, import_ai + '\n\n    ', code, flags=re.DOTALL)

# Let's also change SelecionarPdfAsync to SelecionarPdfsAsync! BUT heuristic still uses single picker.
# So I'll ADD SelecionarPdfsAsync instead of replacing.
picker_multiple = '''
    private async Task<IEnumerable<FileResult>> SelecionarPdfsAsync()
    {
        try
        {
            var r = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Selecionar PDFs",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.WinUI] = new[] { ".pdf" }
                })
            });
            return r ?? Enumerable.Empty<FileResult>();
        }
        catch { return Enumerable.Empty<FileResult>(); }
    }
'''
code = code.replace('private async Task<FileResult?> SelecionarPdfAsync()', picker_multiple + '\n    private async Task<FileResult?> SelecionarPdfAsync()')

with open('Views/PlanilhamentoPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
