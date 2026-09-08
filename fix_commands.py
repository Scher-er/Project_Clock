with open('ViewModels/PlanilhamentoViewModel.Commands.cs', 'r', encoding='mbcs') as f:
    code = f.read()

code = code.replace('using BalancoPatrimonial.App.Models.Enums;', 'using BalancoPatrimonial.App.Models.Enums;\nusing BalancoPatrimonial.App.Views.Items;')

with open('ViewModels/PlanilhamentoViewModel.Commands.cs', 'w', encoding='utf-8') as f:
    f.write(code)
