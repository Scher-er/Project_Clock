import re

with open('MauiProgram.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('        services.AddTransient<ComparacaoPage>();\n    }\n}', '        services.AddTransient<ComparacaoPage>();\n        services.AddTransient<DashboardViewModel>();\n        services.AddTransient<DashboardPage>();\n    }\n}')

with open('MauiProgram.cs', 'w', encoding='utf-8') as f:
    f.write(code)
