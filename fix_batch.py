import re

with open('Views/PlanilhamentoPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# Replace the single file picker
replacement = '''    private async Task<IEnumerable<FileResult>> SelecionarPdfsAsync()
    {
        try
        {
            var resultados = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Selecionar PDFs de balanço",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.WinUI] = new[] { ".pdf" }
                })
            });
            return resultados ?? Enumerable.Empty<FileResult>();
        }
        catch
        {
            return Enumerable.Empty<FileResult>();
        }
    }'''

code = re.sub(r'private async Task<FileResult\?> SelecionarPdfAsync\(\).*?return null;\s*\}\s*\}', replacement, code, flags=re.DOTALL)

with open('Views/PlanilhamentoPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
