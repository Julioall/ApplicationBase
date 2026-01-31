@echo off
REM E2E Tests Helper Script for Windows
REM Inicia todos os serviços necessários para E2E tests

echo.
echo ===================================================
echo    E2E Tests - ApplicationBase Setup Helper
echo ===================================================
echo.

REM Verificar se está no diretório correto
if not exist "playwright.config.ts" (
    echo [ERRO] Execute este script a partir de Application.Client\e2e
    pause
    exit /b 1
)

echo [OK] Configuracao verificada
echo.
echo ================== PROXIMOS PASSOS ==================
echo.
echo 1. Backend (OBRIGATORIO - em terminal separado):
echo    cd c:\Users\Julio\Desktop\Repositorios\ApplicationBase
echo    dotnet run --project Application.Web
echo.
echo 2. Docker services (opcional - em terminal separado):
echo    docker compose up
echo.
echo 3. E2E Tests (apos backend inicializar):
echo    npm test
echo.
echo IMPORTANTE: O frontend sera iniciado automaticamente
echo             pelo Playwright quando rodar npm test
echo.
echo BaseURL: https://localhost:4200
echo.
echo =====================================================
echo.
pause
