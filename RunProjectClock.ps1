$ErrorActionPreference = 'Stop'

function Pause-Script {
    Write-Host ""
    Read-Host "Pressione ENTER para fechar esta janela"
}

try {
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host "     INICIALIZACAO DO PROJECT CLOCK (v2.0)        " -ForegroundColor Cyan
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host ""

    # 1. Verifica .NET SDK
    Write-Host "[1/4] Verificando ambiente .NET..." -ForegroundColor Yellow
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (!$dotnet) {
        Write-Host "ERRO CRITICO: .NET 8 SDK nao encontrado. Por favor, instale o .NET 8 MAUI." -ForegroundColor Red
        Pause-Script
        exit
    }
    $dotnetVersion = (dotnet --version)
    Write-Host "OK: .NET CLI encontrado (Versao: $dotnetVersion)" -ForegroundColor Green

    # 2. Restaurar pacotes e workloads
    Write-Host "
[2/4] Verificando dependencias e pacotes NuGet..." -ForegroundColor Yellow
    try {
        dotnet restore
        Write-Host "OK: Pacotes restaurados com sucesso." -ForegroundColor Green
    } catch {
        Write-Host "ERRO: Nao foi possivel restaurar pacotes. Verifique sua conexao com a internet." -ForegroundColor Red
        Pause-Script
        exit
    }

    # 3. Validar e Iniciar Conexoes
    Write-Host "
[3/4] Verificando servicos de Banco de Dados..." -ForegroundColor Yellow

    # Funcao auxiliar para iniciar servico
    function Ensure-ServiceRunning {
        param($ServiceNamePattern, $Port, $DisplayName)
        
        Write-Host "Testando $DisplayName (Porta $Port)... " -NoNewline
        $portTest = Test-NetConnection -ComputerName 127.0.0.1 -Port $Port -WarningAction SilentlyContinue
        
        if ($portTest.TcpTestSucceeded) {
            Write-Host "OK! (Ja esta rodando)" -ForegroundColor Green
            return $true
        } else {
            Write-Host "FALHOU!" -ForegroundColor Red
            Write-Host " >> Tentando ligar o servico automaticamente..." -ForegroundColor Cyan
            
            $service = Get-Service -Name $ServiceNamePattern -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($service) {
                if ($service.Status -ne 'Running') {
                    Write-Host " >> Servico '$($service.Name)' encontrado. Solicitando permissao de Administrador para iniciar..." -ForegroundColor Yellow
                    try {
                        Start-Process powershell -Verb RunAs -Wait -ArgumentList "-WindowStyle Hidden -Command Start-Service $($service.Name)"
                        
                        # Testa novamente a porta
                        Start-Sleep -Seconds 2
                        $portTest2 = Test-NetConnection -ComputerName 127.0.0.1 -Port $Port -WarningAction SilentlyContinue
                        if ($portTest2.TcpTestSucceeded) {
                            Write-Host " >> SUCESSO: $DisplayName foi iniciado com sucesso!" -ForegroundColor Green
                            return $true
                        } else {
                            Write-Host " >> ERRO: O servico parece ter iniciado, mas a porta continua fechada." -ForegroundColor Red
                        }
                    } catch {
                        Write-Host " >> ERRO: Falha ao tentar iniciar o servico (permissao negada ou erro interno)." -ForegroundColor Red
                    }
                }
            } else {
                Write-Host " >> ERRO: Nenhum servico com nome parecido com '$ServiceNamePattern' foi encontrado no Windows." -ForegroundColor Red
            }
            return $false
        }
    }

    # MySQL
    $mysqlOk = Ensure-ServiceRunning -ServiceNamePattern "MySQL*" -Port 3306 -DisplayName "MySQL Server"
    if (!$mysqlOk) {
        Write-Host " ! O aplicativo requer o MySQL ligado e os scripts da pasta Scripts\ importados." -ForegroundColor Yellow
    }

    # MongoDB
    $mongoOk = Ensure-ServiceRunning -ServiceNamePattern "MongoDB*" -Port 27017 -DisplayName "MongoDB"
    if (!$mongoOk) {
        Write-Host " ! O aplicativo funcionara, mas nenhum Log de Auditoria sera salvo no banco." -ForegroundColor Yellow
    }

    # 4. Executar
    Write-Host "
[4/4] Compilando e iniciando a aplicacao MAUI Windows..." -ForegroundColor Yellow
    Write-Host "Pode levar alguns instantes na primeira vez. Uma nova janela do aplicativo ira se abrir..." -ForegroundColor DarkGray

    try {
        # Roda o aplicativo. O terminal vai aguardar ate o app fechar.
        dotnet run -f net8.0-windows10.0.19041.0
    } catch {
        Write-Host "Ocorreu um erro critico ao compilar/executar a aplicacao." -ForegroundColor Red
        Pause-Script
        exit
    }

    Write-Host "
Project Clock encerrado normalmente." -ForegroundColor Cyan
    Start-Sleep -Seconds 3

} catch {
    Write-Host "
UM ERRO INESPERADO OCORREU NO SCRIPT:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Pause-Script
}
