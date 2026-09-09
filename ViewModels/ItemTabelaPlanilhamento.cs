using BalancoPatrimonial.App.Views.Items;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BalancoPatrimonial.App.ViewModels;

public enum TipoItemTabela { LinhaConta, BotaoAdicionar }

public partial class ItemTabelaPlanilhamento : ObservableObject
{
    public TipoItemTabela Tipo { get; init; }

    public bool IsLinhaConta => Tipo == TipoItemTabela.LinhaConta;
    public bool IsBotaoAdicionar => Tipo == TipoItemTabela.BotaoAdicionar;

    /// <summary>Preenchido se Tipo == LinhaConta</summary>
    public LinhaContaPlanilhamento? Linha { get; init; }

    /// <summary>Preenchido se Tipo == BotaoAdicionar</summary>
    public int? ContaPaiIdParaAdicionar { get; init; }
}
