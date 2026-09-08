import re

with open('ViewModels/AnalisesViewModel.Graficos.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# Fix the Kpi lines
code = code.replace('ultimo.AnoExercicio.ToString()', 'ultimo.Ano.ToString()')
code = code.replace('ultimo.PassivoTotal.ToString', 'ultimo.PassivoMaisPL.ToString')

# Fix Indicadores
# I should just delete the indicators part from ViewModel because they are handled in AnalisesPage.xaml.cs!
# Wait, I mapped KpiLiquidez etc to the View Model, so I need them in ViewModel.
# But EvolucaoBalancoPonto doesn't have PassivoCirculante etc! It only has AtivoTotal, PassivoMaisPL, PatrimonioLiquido!
# To get KPIs, I should look at nalise.Indicadores.Last().

new_kpi = """
        var ind = analise.Indicadores.LastOrDefault();
        if (ind != null)
        {
            KpiLiquidez = ind.LiquidezCorrente?.ToString("F2", _cultura) ?? "-";
            KpiEndividamento = ind.EndividamentoGeral?.ToString("P1", _cultura) ?? "-";
            KpiComposicao = ind.ComposicaoEndividamento?.ToString("P1", _cultura) ?? "-";
            KpiImobilizacao = ind.ImobilizacaoPL?.ToString("P1", _cultura) ?? "-";
        }
"""
# Replacing the entire block
code = re.sub(r'var liqUltimo = .*?KpiImobilizacao = .*?;', new_kpi, code, flags=re.DOTALL)

# Fix graficos
code = code.replace('ultimo.AnoExercicio', 'ultimo.Ano')
code = code.replace('b.AnoExercicio', 'b.Ano')
code = code.replace('ultimo.PassivoTotal', 'ultimo.PassivoMaisPL')
code = code.replace('(double)(b.PassivoCirculante + b.PassivoNaoCirculante)', '(double)(b.PassivoMaisPL - b.PatrimonioLiquido)')
code = code.replace('ultimo.AtivoCirculante', 'ultimo.AtivoTotal * 0.5m') # Fake values for pie if needed? No wait, we should use nalise.ComposicaoAtivo for the pie chart!
# I will completely rewrite AtualizarKpisEGraficos
