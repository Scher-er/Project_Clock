using Microsoft.UI.Xaml;

// Pra deixar o codigo abaixo enxuto. Mais info:
// https://learn.microsoft.com/dotnet/maui/platform-integration/platform-specific?pivots=windows

namespace BalancoPatrimonial.App.WinUI;

/// <summary>
/// Application root específica do Windows (WinUI).
/// Esta classe herda <see cref="MauiWinUIApplication"/> que sabe como bootar
/// o app MAUI dentro do WinUI Composition. Aqui é onde o <c>Main</c> vive —
/// sem este arquivo, o linker reclama de CS5001.
/// </summary>
public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
