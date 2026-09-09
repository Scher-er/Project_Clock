import re

with open('Services/CvmMockService.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('TipoEmpresa.SABerta', 'TipoEmpresa.Outro')
code = code.replace('ResultadoOperacao<AnaliseAutomaticaResultado>.Sucesso(res)', 'ResultadoOperacao<AnaliseAutomaticaResultado>.Ok(res)')

with open('Services/CvmMockService.cs', 'w', encoding='utf-8') as f:
    f.write(code)

with open('Views/PlanilhamentoPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('TipoEmpresa.SABerta', 'TipoEmpresa.Outro')

with open('Views/PlanilhamentoPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
