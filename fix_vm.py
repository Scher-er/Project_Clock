import re

with open('ViewModels/PlanilhamentoViewModel.cs', 'r', encoding='mbcs') as f:
    code = f.read()

code = code.replace('private void AtualizarSubtitulo()', 'public void AtualizarSubtitulo()')

new_method = """    public async Task RecarregarPlanoPreservandoValoresAsync()
    {
        var snapshot = new Dictionary<(int contaId, int perIdx), decimal>();
        foreach (var linha in Linhas.Where(l => l.EhEditavel))
            foreach (var kv in linha.Celulas)
                if (kv.Value.Valor != 0)
                    snapshot[(linha.Conta.Id, kv.Key)] = kv.Value.Valor;

        var rContas = await _controller.ListarPlanoDeContasAsync();
        if (!rContas.Sucesso || rContas.Dados is null) return;

        PlanoContas = rContas.Dados.ToList();
        Linhas = LinhaContaPlanilhamento.ConstruirHierarquia(PlanoContas);

        foreach (var ((contaId, perIdx), valor) in snapshot)
        {
            if (perIdx >= Periodos.Count) continue;
            var linha = Linhas.FirstOrDefault(l => l.Conta.Id == contaId);
            if (linha is not null && linha.EhEditavel)
                linha.ObterCelula(perIdx).Valor = valor;
        }

        ReconstruirTabelaAction?.Invoke();
    }
"""

code = re.sub(r'public async Task InicializarAsync\(\)\s*\{', new_method + '\n    public async Task InicializarAsync() {', code, count=1)

with open('ViewModels/PlanilhamentoViewModel.cs', 'w', encoding='utf-8') as f:
    f.write(code)
