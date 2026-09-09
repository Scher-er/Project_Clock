import re

# IListagensService.cs
with open('Services/IListagensService.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('Task<ResultadoOperacao> SalvarMapeamentoDeParaAsync', 'Task<ResultadoOperacao<bool>> SalvarMapeamentoDeParaAsync')

with open('Services/IListagensService.cs', 'w', encoding='utf-8') as f:
    f.write(code)

# ListagensService.cs
with open('Services/ListagensService.cs', 'r', encoding='utf-8') as f:
    code2 = f.read()

code2 = code2.replace('public async Task<ResultadoOperacao> SalvarMapeamentoDeParaAsync', 'public async Task<ResultadoOperacao<bool>> SalvarMapeamentoDeParaAsync')
code2 = code2.replace('return ResultadoOperacao.Ok();', 'return ResultadoOperacao<bool>.Ok(true);')
code2 = code2.replace('return ResultadoOperacao.FalhaExcecao(ex);', 'return ResultadoOperacao<bool>.FalhaExcecao(ex);')

with open('Services/ListagensService.cs', 'w', encoding='utf-8') as f:
    f.write(code2)
