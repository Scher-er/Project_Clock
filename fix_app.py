import re

with open('App.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

replacement = '''        // Aplica tema salvo (fire-and-forget)
        _ = AplicarTemaSalvoAsync();
        
        // Aquecimento da IA para reduzir latência do 1º uso
        _ = WarmUpIaAsync();'''

code = code.replace('        // Aplica tema salvo (fire-and-forget ?" nAo bloqueia startup)\n        _ = AplicarTemaSalvoAsync();', replacement)

method = '''
    private async Task WarmUpIaAsync()
    {
        try
        {
            var iaService = Services.GetService<IPdfAiAnalyzerService>();
            if (iaService != null && await iaService.ConfiguradoAsync())
            {
                await iaService.TestarConexaoAsync();
            }
        }
        catch { }
    }
}'''

code = code.replace('}\n', method + '\n')

with open('App.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
