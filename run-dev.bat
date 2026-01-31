@echo off
REM Script para setup completo do desenvolvimento em um comando
REM Uso: run-dev.bat [backend|frontend|test|all]

setlocal enabledelayedexpansion

cd /d "%~dp0"

if "%1"=="" goto help
if /i "%1"=="backend" goto backend
if /i "%1"=="frontend" goto frontend
if /i "%1"=="test" goto test
if /i "%1"=="all" goto all
if /i "%1"=="docker" goto docker
goto help

:docker
echo [*] Iniciando infraestrutura Docker...
call docker-compose up -d
echo [+] Docker containers iniciados
echo.
echo Aguarde alguns segundos para os containers estabilizarem...
timeout /t 5 /nobreak
goto end

:backend
echo [*] Verificando .env...
if not exist ".env" (
    if exist ".env.local" (
        echo [+] Copiando .env.local para .env
        copy .env.local .env > nul
    ) else (
        echo [!] Arquivo .env.local não encontrado
        goto help
    )
)

echo [*] Iniciando Docker (se não estiver rodando)...
docker-compose ps | findstr "Up" > nul
if errorlevel 1 (
    echo [+] Docker não está rodando. Iniciando...
    call docker-compose up -d
    timeout /t 3 /nobreak
)

echo [*] Iniciando Backend ASP.NET Core...
cd "%~dp0"
call dotnet run --project Application.Web
goto end

:frontend
echo [*] Verificando Node.js...
call node --version
if errorlevel 1 (
    echo [!] Node.js não instalado
    goto help
)

echo [*] Instalando dependências...
cd "%~dp0Application.Client"
call npm install

echo [*] Iniciando Frontend Angular...
call npm start
goto end

:test
echo [*] Rodando Testes E2E com Playwright...
cd "%~dp0Application.Client\e2e"
call npm install --save-dev @playwright/test

echo [*] Executando testes...
call npm test
goto end

:all
echo [*] Setup Completo - Desenvolvimento Full-Stack
echo.
echo Este script vai:
echo 1. Iniciar Docker (Redis, PostgreSQL, RavenDB, MailHog)
echo 2. Iniciar Backend ASP.NET Core
echo 3. Iniciar Frontend Angular
echo.
echo [*] Fase 1: Docker
call docker-compose up -d
timeout /t 5 /nobreak

echo.
echo [*] Fase 2: Backend (em novo terminal)
echo.
echo Para iniciar o backend em outro terminal, execute:
echo.
echo   cd "%~dp0"
echo   dotnet run --project Application.Web
echo.

echo [*] Fase 3: Frontend
cd "%~dp0Application.Client"
echo [+] Instalando dependências Angular...
call npm install > nul

echo [+] Iniciando Frontend em http://localhost:4200
call npm start
goto end

:help
echo.
echo Usage: run-dev.bat [comando]
echo.
echo Comandos disponíveis:
echo.
echo   all          - Setup completo (Docker + Backend + Frontend)
echo   docker       - Iniciar apenas Docker (Redis, PostgreSQL, RavenDB, MailHog)
echo   backend      - Iniciar Backend ASP.NET Core
echo   frontend     - Iniciar Frontend Angular
echo   test         - Rodar testes E2E com Playwright
echo.
echo Exemplos:
echo.
echo   run-dev.bat docker         ^(Inicia infraestrutura^)
echo   run-dev.bat backend        ^(Inicia API em https://localhost:5001^)
echo   run-dev.bat frontend       ^(Inicia SPA em http://localhost:4200^)
echo   run-dev.bat test           ^(Roda testes Playwright^)
echo.
echo Setup Recomendado:
echo.
echo   1. Terminal 1: run-dev.bat docker
echo   2. Terminal 2: run-dev.bat backend
echo   3. Terminal 3: run-dev.bat frontend
echo   4. Terminal 4: run-dev.bat test
echo.

:end
endlocal
