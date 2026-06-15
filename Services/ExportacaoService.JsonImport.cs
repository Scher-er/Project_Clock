using System.Text.Json;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Importação de um balanço a partir de um arquivo .json (formato gerado pela
/// própria exportação). Faz desserialização, VALIDAÇÃO dos dados e gravação no
/// MySQL, com tratamento de erros em cada etapa.
/// </summary>
public partial class ExportacaoService
{
    public async Task<ResultadoOperacao<string>> ImportarJsonAsync(string caminhoArquivo, int usuarioId)
    {
        // 1) Arquivo
        if (string.IsNullOrWhiteSpace(caminhoArquivo) || !File.Exists(caminhoArquivo))
            return ResultadoOperacao<string>.Falha("Arquivo não encontrado.");

        // 2) Leitura + desserialização (com tratamento de JSON malformado)
        BalancoExportDto? dto;
        try
        {
            var json = await File.ReadAllTextAsync(caminhoArquivo);
            dto = JsonSerializer.Deserialize<BalancoExportDto>(json, _jsonOpts);
        }
        catch (JsonException jx)
        {
            return ResultadoOperacao<string>.Falha($"JSON inválido: {jx.Message}");
        }
        catch (Exception ex)
        {
            return ResultadoOperacao<string>.FalhaExcecao(ex);
        }

        // 3) Validação dos dados importados
        var erros = ValidarImportacao(dto);
        if (erros.Count > 0)
            return ResultadoOperacao<string>.Falha("Dados inválidos no arquivo.", erros);

        try
        {
            // 4) Empresa: reaproveita se o CNPJ já existir; senão cria
            var empresa = await _empresaDao.BuscarPorCnpjAsync(dto!.Empresa.Cnpj);
            if (empresa is null)
            {
                empresa = new Empresa
                {
                    Cnpj = dto.Empresa.Cnpj,
                    RazaoSocial = dto.Empresa.RazaoSocial,
                    NomeFantasia = dto.Empresa.NomeFantasia,
                    TipoEmpresa = Enum.TryParse<TipoEmpresa>(dto.Empresa.TipoEmpresa, out var te) ? te : TipoEmpresa.Outro,
                    Rating = dto.Empresa.Rating,
                    LimiteCredito = dto.Empresa.LimiteCredito,
                    UfAtuacao = dto.Empresa.UfAtuacao,
                    LocalAtuacao = dto.Empresa.LocalAtuacao
                };
                empresa.Id = await _empresaDao.InserirAsync(empresa);
            }

            // 5) Mapa código -> ContaPadrao (pra resolver as contas do JSON)
            var plano = (await _contaPadraoDao.ListarHierarquiaAsync())
                .ToDictionary(c => c.Codigo, c => c);

            // 6) Monta o balanço (ignora totalizadoras — são recalculadas)
            var tipo = Enum.TryParse<TipoBalanco>(dto.Balanco.Tipo, out var tb) ? tb : TipoBalanco.Individual;
            var balanco = new Balanco
            {
                EmpresaId = empresa.Id,
                AnoExercicio = dto.Balanco.AnoExercicio,
                DataReferencia = dto.Balanco.DataReferencia == default
                    ? new DateTime(dto.Balanco.AnoExercicio, 12, 31)
                    : dto.Balanco.DataReferencia,
                TipoBalanco = tipo,
                Moeda = string.IsNullOrWhiteSpace(dto.Balanco.Moeda) ? "BRL" : dto.Balanco.Moeda,
                MultiplicadorValores = dto.Balanco.MultiplicadorOriginal == 0 ? 1 : dto.Balanco.MultiplicadorOriginal,
                Origem = "Importação JSON",
                UsuarioId = usuarioId,
                Observacoes = dto.Balanco.Observacoes
            };

            int contasIgnoradas = 0;
            foreach (var c in dto.Contas)
            {
                if (c.EhTotalizadora) continue;
                if (plano.TryGetValue(c.Codigo, out var contaPadrao) && !contaPadrao.EhTotalizadora)
                    balanco.Contas.Add(new ContaBalanco { ContaPadraoId = contaPadrao.Id, Valor = c.Valor });
                else
                    contasIgnoradas++;
            }

            if (balanco.Contas.Count == 0)
                return ResultadoOperacao<string>.Falha(
                    "Nenhuma conta do arquivo foi reconhecida no plano de contas atual.");

            // 7) Grava (insere balanço + contas numa transação)
            var id = await _balancoDao.InserirComContasAsync(balanco);

            await _log.RegistrarAsync(TipoEventoLog.Importacao, "IMPORTOU_JSON",
                $"Balanço {balanco.AnoExercicio} da empresa '{empresa.RazaoSocial}' importado de JSON (id {id}).",
                "Balanco", id.ToString());

            var aviso = contasIgnoradas > 0 ? $" ({contasIgnoradas} conta(s) não reconhecida(s) foram ignoradas)" : "";
            return ResultadoOperacao<string>.Ok(
                $"Importado: {empresa.RazaoSocial} — balanço {balanco.AnoExercicio} com {balanco.Contas.Count} contas{aviso}.",
                "Importação concluída.");
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("IMPORTAR_JSON", ex, "Balanco");
            return ResultadoOperacao<string>.FalhaExcecao(ex);
        }
    }

    private static List<string> ValidarImportacao(BalancoExportDto? dto)
    {
        var erros = new List<string>();
        if (dto is null) { erros.Add("Arquivo vazio ou em formato desconhecido."); return erros; }
        if (dto.Empresa is null) erros.Add("Bloco 'empresa' ausente.");
        else
        {
            if (string.IsNullOrWhiteSpace(dto.Empresa.Cnpj)) erros.Add("CNPJ da empresa ausente.");
            if (string.IsNullOrWhiteSpace(dto.Empresa.RazaoSocial)) erros.Add("Razão social ausente.");
        }
        if (dto.Balanco is null) erros.Add("Bloco 'balanco' ausente.");
        else if (dto.Balanco.AnoExercicio < 1900 || dto.Balanco.AnoExercicio > 2200)
            erros.Add($"Ano de exercício inválido ({dto.Balanco.AnoExercicio}).");
        if (dto.Contas is null || dto.Contas.Count == 0) erros.Add("Nenhuma conta no arquivo.");
        return erros;
    }
}
