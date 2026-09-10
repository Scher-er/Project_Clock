$ErrorActionPreference = 'Stop'

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "     INICIALIZACAO DO PROJECT CLOCK (v2.0)        " -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verifica .NET SDK
Write-Host "[1/4] Verificando ambiente .NET..." -ForegroundColor Yellow
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (!$dotnet) {
    Write-Host "ERRO CRITICO: .NET 8 SDK nao encontrado. Por favor, instale o .NET 8 MAUI." -ForegroundColor Red
    Read-Host "Pressione ENTER para sair"
    exit
}
$dotnetVersion = dotnet --version
Write-Host "OK: .NET CLI encontrado (Versao: $dotnetVersion)" -ForegroundColor Green

# 2. Restaurar pacotes e workloads
Write-Host "
[2/4] Verificando dependencias e pacotes NuGet..." -ForegroundColor Yellow
try {
    dotnet restore
    Write-Host "OK: Pacotes restaurados com sucesso." -ForegroundColor Green
} catch {
    Write-Host "ERRO: Nao foi possivel restaurar pacotes. Verifique sua conexao com a internet." -ForegroundColor Red
    Read-Host "Pressione ENTER para sair"
    exit
}

# 3. Validar Conexoes
Write-Host "
[3/4] Verificando servicos de Banco de Dados..." -ForegroundColor Yellow

# MySQL
Write-Host "Testando MySQL (Porta 3306)... " -NoNewline
$mysqlPort = Test-NetConnection -ComputerName 127.0.0.1 -Port 3306 -WarningAction SilentlyContinue
if ($mysqlPort.TcpTestSucceeded) {
    Write-Host "OK!" -ForegroundColor Green
} else {
    Write-Host "FALHOU!" -ForegroundColor Red
    Write-Host " >> AVISO: O servico do MySQL nao esta rodando localmente (Porta 3306)." -ForegroundColor Yellow
    Write-Host " >> O sistema requer o MySQL ligado e os scripts da pasta Scripts\ importados." -ForegroundColor Yellow
    Write-Host " >> Verifique se o MySQL Server (XAMPP, MySQL Workbench, etc) esta online." -ForegroundColor Yellow
    Write-Host " >> O aplicativo pode fechar ou falhar no login." -ForegroundColor Yellow
}

# MongoDB
Write-Host "Testando MongoDB (Porta 27017)... " -NoNewline
$mongoPort = Test-NetConnection -ComputerName 127.0.0.1 -Port 27017 -WarningAction SilentlyContinue
if ($mongoPort.TcpTestSucceeded) {
    Write-Host "OK!" -ForegroundColor Green
} else {
    Write-Host "FALHOU!" -ForegroundColor Red
    Write-Host " >> AVISO: O MongoDB nao esta rodando na porta 27017." -ForegroundColor Yellow
    Write-Host " >> O sistema funcionara normalmente, mas nenhum Log de Auditoria sera salvo no banco." -ForegroundColor Yellow
}

# 4. Executar
Write-Host "
[4/4] Compilando e iniciando a aplicacao MAUI Windows..." -ForegroundColor Yellow
Write-Host "Pode levar alguns instantes na primeira vez..." -ForegroundColor DarkGray

try {
    dotnet run -f net8.0-windows10.0.19041.0
} catch {
    Write-Host "Ocorreu um erro ao compilar/executar a aplicacao." -ForegroundColor Red
}

Write-Host "
Project Clock encerrado." -ForegroundColor Cyan
