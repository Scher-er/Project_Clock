import re

with open('AppShell.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

replacement = '''        var sp = App.Services;
        scDashboard.ContentTemplate     = new DataTemplate(() => sp.GetRequiredService<DashboardPage>());
        scPlanilhamento.ContentTemplate = new DataTemplate(() => sp.GetRequiredService<PlanilhamentoPage>());'''

code = code.replace('        var sp = App.Services;\n        scPlanilhamento.ContentTemplate = new DataTemplate(() => sp.GetRequiredService<PlanilhamentoPage>());', replacement)

with open('AppShell.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
