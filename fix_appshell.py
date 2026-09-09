import re

with open('AppShell.xaml', 'r', encoding='utf-8') as f:
    code = f.read()

replacement = '''    <!-- Dashboard -->
    <FlyoutItem Title="Dashboard" Route="dashboard">
        <ShellContent x:Name="scDashboard" />
    </FlyoutItem>

    <FlyoutItem Title="Planilhamento" Route="planilhamento">'''

code = code.replace('    <FlyoutItem Title="Planilhamento" Route="planilhamento">', replacement)

with open('AppShell.xaml', 'w', encoding='utf-8') as f:
    f.write(code)
