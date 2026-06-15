using System.Globalization;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Views.Items;

/// <summary>
/// Item de exibição de um balanço na máscara de visualização da empresa.
/// Formata os dados do <see cref="Balanco"/> pra leitura rápida.
/// </summary>
public class BalancoResumoItem
{
    private static readonly CultureInfo _ptBR = new("pt-BR");
    private readonly Balanco _balanco;

    public BalancoResumoItem(Balanco balanco) => _balanco = balanco;

    public int Id => _balanco.Id;

    /// <summary>Ex: "2024 · Consolidado".</summary>
    public string Titulo
    {
        get
        {
            var tipo = _balanco.TipoBalanco == TipoBalanco.Consolidado ? "Consolidado" : "Individual";
            return $"{_balanco.AnoExercicio} · {tipo}";
        }
    }

    public string DataReferencia => $"Ref.: {_balanco.DataReferencia:dd/MM/yyyy}";

    public string DataPlanilhamento => $"Planilhado em {_balanco.DataPlanilhamento:dd/MM/yyyy HH:mm}";

    public string Origem => string.IsNullOrWhiteSpace(_balanco.Origem) ? "Manual" : _balanco.Origem!;

    public string LinhaAtivo => $"Ativo: R$ {_balanco.AtivoTotal.ToString("N2", _ptBR)}";

    public string LinhaPassivo => $"Passivo + PL: R$ {_balanco.PassivoTotal.ToString("N2", _ptBR)}";

    public string LinhaContas => $"{_balanco.Contas.Count} contas";

    /// <summary>Cor do chip de tipo (consolidado = azul forte, individual = cinza).</summary>
    public Color CorTipo => _balanco.TipoBalanco == TipoBalanco.Consolidado
        ? Color.FromArgb("#1F3A60")
        : Color.FromArgb("#5A6478");

    public string TipoChip => _balanco.TipoBalanco == TipoBalanco.Consolidado ? "C" : "I";

    /// <summary>Diferença Ativo - (Passivo+PL). Verde se fecha, vermelho se não.</summary>
    public bool Fecha => Math.Abs(_balanco.AtivoTotal - _balanco.PassivoTotal) <= 1.00m;

    public string StatusBalanceamento => Fecha ? "Fechado" : "Não fecha";

    /// <summary>Texto do selo com símbolo (✓ fecha / ⚠ não fecha).</summary>
    public string SeloStatus => Fecha ? "✓ Fecha" : "⚠ Não fecha";

    public Color CorStatus => Fecha ? Color.FromArgb("#0E7C66") : Color.FromArgb("#C0392B");
}
