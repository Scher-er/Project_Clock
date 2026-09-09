import re

with open('ViewModels/PlanilhamentoViewModel.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# Add the ObservableCollection
replacement = '''    public ObservableCollection<PeriodoPlanilhado> Periodos { get; } = new();
    public List<LinhaContaPlanilhamento> Linhas { get; private set; } = new();
    public ObservableCollection<ItemTabelaPlanilhamento> ItensTabela { get; } = new();
    public List<ContaPadrao> PlanoContas { get; private set; } = new();'''

code = code.replace('''    public ObservableCollection<PeriodoPlanilhado> Periodos { get; } = new();
    public List<LinhaContaPlanilhamento> Linhas { get; private set; } = new();
    public List<ContaPadrao> PlanoContas { get; private set; } = new();''', replacement)

# Instead of relying on Action events to call ReconstruirTabela, the VM itself can update ItensTabela
update_logic = '''
    public void AtualizarItensTabela()
    {
        ItensTabela.Clear();
        for (int idx = 0; idx < Linhas.Count; idx++)
        {
            var l = Linhas[idx];
            l.AtualizarCelulasVisiveis(Periodos.Count); // ensure the row has cells
            ItensTabela.Add(new ItemTabelaPlanilhamento { Tipo = TipoItemTabela.LinhaConta, Linha = l });
            
            if (l.EhEditavel && l.Pai is not null)
            {
                bool ultimaDoPai = (idx == Linhas.Count - 1) || !ReferenceEquals(Linhas[idx + 1].Pai, l.Pai);
                if (ultimaDoPai)
                {
                    ItensTabela.Add(new ItemTabelaPlanilhamento { Tipo = TipoItemTabela.BotaoAdicionar, ContaPaiIdParaAdicionar = l.Pai.Conta.Id });
                }
            }
        }
    }
'''

# We will inject AtualizarItensTabela() before the last brace
idx = code.rfind('}')
if idx != -1:
    code = code[:idx] + update_logic + '\n}'

with open('ViewModels/PlanilhamentoViewModel.cs', 'w', encoding='utf-8') as f:
    f.write(code)
