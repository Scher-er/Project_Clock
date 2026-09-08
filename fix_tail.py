import re

with open('Views/AnalisesPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# Remove anything after the class closing brace
# But first let's just find private static Border EmbrulharCard(View conteudo)
match = re.search(r'private static Border EmbrulharCard\(View conteudo\).*?return card;\s*\}', code, flags=re.DOTALL)
if match:
    # We will slice right after EmbrulharCard
    end_idx = match.end()
    
    # And we'll append FormatarCurto inside the class, then close the class
    good_code = code[:end_idx] + """
    
    private string FormatarCurto(decimal valor)
    {
        var v = (double)valor;
        if (Math.Abs(v) >= 1_000_000_000) return $"R$ {v / 1_000_000_000:0.##}B";
        if (Math.Abs(v) >= 1_000_000) return $"R$ {v / 1_000_000:0.##}M";
        if (Math.Abs(v) >= 1_000) return $"R$ {v / 1_000:0.##}k";
        return $"R$ {v:0.##}";
    }
}
"""
    with open('Views/AnalisesPage.xaml.cs', 'w', encoding='utf-8') as f:
        f.write(good_code)
