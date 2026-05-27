using System.Text.RegularExpressions;
using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Services;

public class EmpresaService : IEmpresaService
{
    private readonly IEmpresaDao _empresaDao;
    private readonly ILogService _log;
    public string NomeServico => "EmpresaService";

    public EmpresaService(IEmpresaDao empresaDao, ILogService log)
    {
        _empresaDao = empresaDao;
        _log = log;
    }

    public async Task<ResultadoOperacao<int>> CadastrarAsync(
        Empresa empresa, IEnumerable<int>? setoresIds = null)
    {
        // Validações
        var erros = ValidarCampos(empresa);
        if (erros.Count > 0)
            return ResultadoOperacao<int>.Falha("Há erros de validação.", erros);

        empresa.Cnpj = LimparCnpj(empresa.Cnpj);

        // Unicidade do CNPJ
        var existente = await _empresaDao.BuscarPorCnpjAsync(empresa.Cnpj);
        if (existente is not null)
        {
            return ResultadoOperacao<int>.Falha(
                $"Já existe uma empresa cadastrada com este CNPJ ({empresa.RazaoSocial}).");
        }

        try
        {
            var id = await _empresaDao.InserirAsync(empresa);

            // Vincula setores (N:N) — o primeiro vira o principal
            if (setoresIds is not null)
            {
                bool primeiro = true;
                foreach (var setorId in setoresIds)
                {
                    await _empresaDao.VincularSetorAsync(id, setorId, primeiro);
                    primeiro = false;
                }
            }

            await _log.RegistrarAsync(
                TipoEventoLog.Inclusao,
                "INCLUIU_EMPRESA",
                $"Empresa '{empresa.RazaoSocial}' (CNPJ {empresa.Cnpj}) cadastrada.",
                "Empresa", id.ToString());

            return ResultadoOperacao<int>.Ok(id, "Empresa cadastrada com sucesso.");
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("INCLUIR_EMPRESA", ex, "Empresa");
            return ResultadoOperacao<int>.FalhaExcecao(ex);
        }
    }

    public async Task<ResultadoOperacao<bool>> AtualizarAsync(Empresa empresa)
    {
        var erros = ValidarCampos(empresa);
        if (erros.Count > 0)
            return ResultadoOperacao<bool>.Falha("Há erros de validação.", erros);

        empresa.Cnpj = LimparCnpj(empresa.Cnpj);

        try
        {
            var sucesso = await _empresaDao.AtualizarAsync(empresa);
            if (sucesso)
            {
                await _log.RegistrarAsync(
                    TipoEventoLog.Alteracao,
                    "ALTEROU_EMPRESA",
                    $"Empresa '{empresa.RazaoSocial}' (id={empresa.Id}) atualizada.",
                    "Empresa", empresa.Id.ToString());
            }
            return ResultadoOperacao<bool>.Ok(sucesso);
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("ATUALIZAR_EMPRESA", ex, "Empresa");
            return ResultadoOperacao<bool>.FalhaExcecao(ex);
        }
    }

    public async Task<ResultadoOperacao<bool>> ExcluirAsync(int empresaId)
    {
        try
        {
            var empresa = await _empresaDao.BuscarPorIdAsync(empresaId);
            var sucesso = await _empresaDao.ExcluirAsync(empresaId);
            if (sucesso)
            {
                await _log.RegistrarAsync(
                    TipoEventoLog.Exclusao,
                    "EXCLUIU_EMPRESA",
                    $"Empresa '{empresa?.RazaoSocial ?? "?"}' (id={empresaId}) excluída.",
                    "Empresa", empresaId.ToString());
            }
            return ResultadoOperacao<bool>.Ok(sucesso);
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("EXCLUIR_EMPRESA", ex, "Empresa");
            return ResultadoOperacao<bool>.FalhaExcecao(ex);
        }
    }

    public async Task<ResultadoOperacao<Empresa>> BuscarCompletaAsync(int empresaId)
    {
        try
        {
            var empresa = await _empresaDao.BuscarCompletaAsync(empresaId);
            return empresa is null
                ? ResultadoOperacao<Empresa>.Falha("Empresa não encontrada.")
                : ResultadoOperacao<Empresa>.Ok(empresa);
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("BUSCAR_EMPRESA", ex, "Empresa");
            return ResultadoOperacao<Empresa>.FalhaExcecao(ex);
        }
    }

    public async Task<ResultadoOperacao<IEnumerable<Empresa>>> ListarAsync()
    {
        try
        {
            return ResultadoOperacao<IEnumerable<Empresa>>.Ok(
                await _empresaDao.ListarComResumoAsync());
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("LISTAR_EMPRESAS", ex, "Empresa");
            return ResultadoOperacao<IEnumerable<Empresa>>.FalhaExcecao(ex);
        }
    }

    public async Task<ResultadoOperacao<IEnumerable<Empresa>>> PesquisarAsync(string termo)
    {
        try
        {
            return ResultadoOperacao<IEnumerable<Empresa>>.Ok(
                await _empresaDao.PesquisarAsync(termo));
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("PESQUISAR_EMPRESAS", ex, "Empresa");
            return ResultadoOperacao<IEnumerable<Empresa>>.FalhaExcecao(ex);
        }
    }

    public bool ValidarCnpj(string cnpj)
    {
        var limpo = LimparCnpj(cnpj);
        return limpo.Length == 14 && limpo.All(char.IsDigit);
    }

    public string LimparCnpj(string cnpj)
        => string.IsNullOrWhiteSpace(cnpj) ? string.Empty : Regex.Replace(cnpj, @"[^\d]", string.Empty);

    // ───── helpers ─────

    private List<string> ValidarCampos(Empresa e)
    {
        var erros = new List<string>();

        if (string.IsNullOrWhiteSpace(e.RazaoSocial))
            erros.Add("Razão social é obrigatória.");
        else if (e.RazaoSocial.Length > 200)
            erros.Add("Razão social pode ter no máximo 200 caracteres.");

        if (!ValidarCnpj(e.Cnpj))
            erros.Add("CNPJ inválido. Deve conter 14 dígitos.");

        if (e.LimiteCredito is < 0)
            erros.Add("Limite de crédito não pode ser negativo.");

        if (!string.IsNullOrEmpty(e.UfAtuacao) && e.UfAtuacao.Length != 2)
            erros.Add("UF deve ter exatamente 2 caracteres.");

        return erros;
    }
}
