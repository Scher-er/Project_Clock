import re

with open('Services/ListagensService.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('private readonly IContaPadraoDao _contaDao;',
                    'private readonly IContaPadraoDao _contaDao;\n    private readonly IMapeamentoDeParaDao _deParaDao;')

code = code.replace('IContaPadraoDao contaDao)',
                    'IContaPadraoDao contaDao,\n        IMapeamentoDeParaDao deParaDao)')

code = code.replace('_contaDao = contaDao;',
                    '_contaDao = contaDao;\n        _deParaDao = deParaDao;')

# Add the new methods at the end before the last closing brace
methods = """
    public Task<MapeamentoDePara?> BuscarMapeamentoDeParaAsync(string textoOriginal, int? empresaId)
    {
        return _deParaDao.BuscarMapeamentoAsync(textoOriginal, empresaId);
    }

    public async Task<ResultadoOperacao> SalvarMapeamentoDeParaAsync(MapeamentoDePara mapeamento)
    {
        try
        {
            var existente = await _deParaDao.BuscarMapeamentoAsync(mapeamento.TextoOriginal, mapeamento.EmpresaId);
            if (existente != null)
            {
                existente.ContaPadraoId = mapeamento.ContaPadraoId;
                await _deParaDao.AtualizarAsync(existente);
            }
            else
            {
                await _deParaDao.InserirAsync(mapeamento);
            }
            return ResultadoOperacao.Ok();
        }
        catch (Exception ex)
        {
            return ResultadoOperacao.FalhaExcecao(ex);
        }
    }
}
"""
code = code.replace('}\n', methods)
# Just replacing the last one
idx = code.rfind('}')
if idx != -1:
    code = code[:idx] + methods

with open('Services/ListagensService.cs', 'w', encoding='utf-8') as f:
    f.write(code)
