namespace BalancoPatrimonial.App.Views;

public partial class PlanilhamentoPage : ContentPage
{
    private readonly Style? _stylePrimary;
    private readonly Style? _styleSecondary;

    public PlanilhamentoPage()
    {
        InitializeComponent();

        // Cacheia os estilos globais (definidos em Resources/Styles/Styles.xaml)
        _stylePrimary = Application.Current?.Resources["PrimaryButton"] as Style;
        _styleSecondary = Application.Current?.Resources["SecondaryButton"] as Style;
    }

    private void OnTabAutoClicked(object? sender, EventArgs e)
    {
        painelAuto.IsVisible = true;
        painelManual.IsVisible = false;
        if (_stylePrimary is not null) btnTabAuto.Style = _stylePrimary;
        if (_styleSecondary is not null) btnTabManual.Style = _styleSecondary;
    }

    private void OnTabManualClicked(object? sender, EventArgs e)
    {
        painelAuto.IsVisible = false;
        painelManual.IsVisible = true;
        if (_styleSecondary is not null) btnTabAuto.Style = _styleSecondary;
        if (_stylePrimary is not null) btnTabManual.Style = _stylePrimary;
    }

    private async void OnSelecionarPdfClicked(object? sender, EventArgs e)
    {
        // Stub Fase 1 — Fase 5 implementa o parser
        await DisplayAlert(
            "Em construção",
            "A seleção e parsing de PDF será implementada na Fase 5.",
            "OK");
    }
}
