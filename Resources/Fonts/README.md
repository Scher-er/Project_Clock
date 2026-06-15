# Fontes customizadas

Coloque arquivos `.ttf` ou `.otf` aqui. Eles serão automaticamente incluídos
no build via `<MauiFont Include="Resources\Fonts\*" />` no csproj.

Pra usar uma fonte adicionada, registre no `MauiProgram.cs`:

```csharp
builder.ConfigureFonts(fonts =>
{
    fonts.AddFont("MinhaFonte-Regular.ttf", "MinhaFonteRegular");
});
```

E use no XAML:

```xml
<Label Text="..." FontFamily="MinhaFonteRegular" />
```
