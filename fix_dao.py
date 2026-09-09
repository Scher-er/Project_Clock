import re

with open('DAO/MySQL/MySqlMapeamentoDeParaDao.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('public class MySqlMapeamentoDeParaDao : IMapeamentoDeParaDao\n{',
'''public class MySqlMapeamentoDeParaDao : IMapeamentoDeParaDao
{
    public string Fonte => "MySQL.mapeamentos_de_para";''')

code = code.replace('public async Task InserirAsync', 'public async Task<int> InserirAsync')
code = code.replace('public async Task AtualizarAsync', 'public async Task<bool> AtualizarAsync')
code = code.replace('public async Task ExcluirAsync', 'public async Task<bool> ExcluirAsync')

# Fix return values
code = code.replace('''entidade.Id = await conexao.ExecuteScalarAsync<int>(sql, new {
            TextoOriginal = entidade.TextoOriginal.ToLowerInvariant().Trim(),
            entidade.ContaPadraoId,
            entidade.EmpresaId
        });
    }''', '''entidade.Id = await conexao.ExecuteScalarAsync<int>(sql, new {
            TextoOriginal = entidade.TextoOriginal.ToLowerInvariant().Trim(),
            entidade.ContaPadraoId,
            entidade.EmpresaId
        });
        return entidade.Id;
    }''')

code = code.replace('''WHERE Id = @Id;
        ";
        await conexao.ExecuteAsync(sql, new {
            entidade.Id,
            TextoOriginal = entidade.TextoOriginal.ToLowerInvariant().Trim(),
            entidade.ContaPadraoId,
            entidade.EmpresaId
        });
    }''', '''WHERE Id = @Id;
        ";
        var rows = await conexao.ExecuteAsync(sql, new {
            entidade.Id,
            TextoOriginal = entidade.TextoOriginal.ToLowerInvariant().Trim(),
            entidade.ContaPadraoId,
            entidade.EmpresaId
        });
        return rows > 0;
    }''')

code = code.replace('''public async Task<bool> ExcluirAsync(int id)
    {
        using var conexao = CreateConnection();
        await conexao.ExecuteAsync("DELETE FROM MapeamentosDePara WHERE Id = @Id", new { Id = id });
    }''', '''public async Task<bool> ExcluirAsync(int id)
    {
        using var conexao = CreateConnection();
        var rows = await conexao.ExecuteAsync("DELETE FROM MapeamentosDePara WHERE Id = @Id", new { Id = id });
        return rows > 0;
    }''')

code = code.replace('public Task<MapeamentoDePara?> ObterPorIdAsync(int id) => throw new NotImplementedException();',
'public Task<MapeamentoDePara?> BuscarPorIdAsync(int id) => throw new NotImplementedException();')

with open('DAO/MySQL/MySqlMapeamentoDeParaDao.cs', 'w', encoding='utf-8') as f:
    f.write(code)
