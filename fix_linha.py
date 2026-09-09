import re

with open('Views/Items/LinhaContaPlanilhamento.cs', 'r', encoding='utf-8') as f:
    code = f.read()

replacement = '''    /// <summary>Células por índice de período. Lazy: cria sob demanda.</summary>
    public Dictionary<int, CelulaPlanilhamento> Celulas { get; } = new();

    public System.Collections.ObjectModel.ObservableCollection<CelulaPlanilhamento> CelulasVisiveis { get; } = new();

    public void AtualizarCelulasVisiveis(int totalPeriodos)
    {
        CelulasVisiveis.Clear();
        for (int i = 0; i < totalPeriodos; i++)
        {
            CelulasVisiveis.Add(ObterCelula(i));
        }
    }'''

code = code.replace('''    /// <summary>CAclulas por A-ndice de perA-odo. Lazy: cria sob demanda.</summary>
    public Dictionary<int, CelulaPlanilhamento> Celulas { get; } = new();''', replacement)

with open('Views/Items/LinhaContaPlanilhamento.cs', 'w', encoding='utf-8') as f:
    f.write(code)
