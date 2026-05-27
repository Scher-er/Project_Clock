using System.Xml.Linq;
using BalancoPatrimonial.App.Models.Enums;
using BalancoPatrimonial.App.Models.Log;
using BalancoPatrimonial.App.Services;

namespace BalancoPatrimonial.App.Controllers;

public class LogController : ILogController
{
    private readonly ILogService _logService;
    public string NomeRecurso => "Log";

    public LogController(ILogService logService)
    {
        _logService = logService;
    }

    public Task<IEnumerable<LogSistema>> ListarPorDataAsync(DateTime dia)
        => _logService.ListarPorDataAsync(dia);

    public Task RegistrarAsync(TipoEventoLog tipo, string acao, string descricao)
        => _logService.RegistrarAsync(tipo, acao, descricao);

    public async Task<string> ExportarXmlAsync(DateTime dia)
    {
        var logs = (await _logService.ListarPorDataAsync(dia)).ToList();

        // Constrói XML usando LINQ to XML (mais legível que XmlWriter direto)
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement("logs",
                new XAttribute("data", dia.ToString("yyyy-MM-dd")),
                new XAttribute("total", logs.Count),
                new XAttribute("geradoEm", DateTime.Now.ToString("o")),
                logs.Select(l => new XElement("log",
                    new XAttribute("id", l.Id ?? string.Empty),
                    new XAttribute("dataHora", l.DataHora.ToString("o")),
                    new XAttribute("tipo", l.TipoEvento.ToString()),
                    new XElement("usuario", l.Usuario),
                    new XElement("acao", l.Acao),
                    new XElement("descricao", l.Descricao),
                    l.Entidade is null ? null : new XElement("entidade",
                        new XAttribute("id", l.EntidadeId ?? string.Empty),
                        l.Entidade),
                    l.Detalhes is null ? null : new XElement("detalhes", l.Detalhes)
                ))
            )
        );

        var pasta = Path.Combine(FileSystem.AppDataDirectory, "exportacoes");
        if (!Directory.Exists(pasta)) Directory.CreateDirectory(pasta);

        var caminho = Path.Combine(pasta, $"logs_{dia:yyyy-MM-dd}.xml");
        await using var stream = File.Create(caminho);
        await doc.SaveAsync(stream, SaveOptions.None, CancellationToken.None);

        await _logService.RegistrarAsync(
            TipoEventoLog.Info,
            "EXPORTOU_LOGS_XML",
            $"{logs.Count} logs do dia {dia:yyyy-MM-dd} exportados para {caminho}");

        return caminho;
    }
}
