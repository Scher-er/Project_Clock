import re

with open('Views/PlanilhamentoPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('Color.FromArgb(#', 'Color.FromArgb("#')
code = code.replace('60);', '60");')
code = code.replace('66);', '66");')
code = code.replace('2B);', '2B");')

# Remove duplicate usings at the top
code = re.sub(r'using Microsoft.*?/// </summary>\s*', '', code, count=1, flags=re.DOTALL)

with open('Views/PlanilhamentoPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
