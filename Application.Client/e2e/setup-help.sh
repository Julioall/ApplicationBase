#!/bin/bash

# E2E Tests Helper Script
# Inicia todos os serviços necessários para E2E tests

echo "🚀 Iniciando serviços para E2E tests..."

# Cores para output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Verificar se está no diretório correto
if [ ! -f "playwright.config.ts" ]; then
    echo -e "${RED}❌ Erro: Execute este script a partir de Application.Client/e2e${NC}"
    exit 1
fi

# Informar próximas ações
echo -e "${GREEN}✓ Configuração verificada${NC}"
echo ""
echo -e "${YELLOW}📋 Próximas etapas (em terminais separados):${NC}"
echo ""
echo "1️⃣  Backend (OBRIGATÓRIO):"
echo "   cd c:/Users/Julio/Desktop/Repositorios/ApplicationBase"
echo "   dotnet run --project Application.Web"
echo ""
echo "2️⃣  Docker services (opcional, se houver dependências):"
echo "   docker compose up"
echo ""
echo "3️⃣  E2E Tests (após backend inicializar):"
echo "   npm test"
echo ""
echo -e "${GREEN}ℹ️  O frontend será iniciado automaticamente pelo Playwright${NC}"
echo ""
echo "🔗 BaseURL: https://localhost:4200"
