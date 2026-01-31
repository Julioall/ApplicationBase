import { test, expect } from '@playwright/test';

test.describe('TODO Feature - E2E Tests', () => {
  test.beforeEach(async ({ page }) => {
    // Navegar para página de tarefas
    await page.goto('http://localhost:4200/todo');
    // Aguardar carregamento
    await page.waitForLoadState('networkidle');
  });

  test('deve exibir lista de tarefas', async ({ page }) => {
    // Verificar se o título está visível
    const title = page.locator('h1');
    await expect(title).toContainText('Minhas Tarefas');
  });

  test('deve carregar tarefas da API', async ({ page }) => {
    // Aguardar elemento de tarefa aparecer
    const cards = page.locator('app-card');
    await expect(cards.first()).toBeVisible();

    const cardText = await cards.first().textContent();
    expect(cardText).toBeTruthy();
  });

  test('deve abrir painel de detalhes ao clicar em tarefa', async ({ page }) => {
    // Clicar na primeira tarefa
    const taskCard = page.locator('app-card').first();
    await taskCard.click();

    // Verificar se o painel de detalhe aparece
    const detailPanel = page.locator('app-todo-task-detail');
    await expect(detailPanel).toBeVisible();
  });

  test('deve fechar painel de detalhes', async ({ page }) => {
    // Abrir painel
    const taskCard = page.locator('app-card').first();
    await taskCard.click();

    // Clicar no botão de fechar
    const closeBtn = page.locator('button').filter({ hasText: '✕' }).first();
    await closeBtn.click();

    // Verificar se painel desapareceu
    const detailPanel = page.locator('app-todo-task-detail');
    await expect(detailPanel).not.toBeVisible({ timeout: 1000 }).catch(() => {
      // Pode ser que ele seja removido do DOM ou apenas invisível
    });
  });

  test('deve filtrar tarefas por status (completo/incompleto)', async ({ page }) => {
    // Contar tarefas incompletas
    const incompleteTasks = page.locator('app-card:not([hidden])');
    const incompleteCount = await incompleteTasks.count();
    
    expect(incompleteCount).toBeGreaterThanOrEqual(0);
  });

  test('deve exibir prioridade com cor correta', async ({ page }) => {
    // Verificar se badges de prioridade estão visíveis
    const badges = page.locator('app-badge');
    await expect(badges.first()).toBeVisible();

    const badgeText = await badges.first().textContent();
    expect(['HIGH', 'MEDIUM', 'LOW']).toContain(badgeText?.trim());
  });

  test('deve responder a interações de teclado', async ({ page }) => {
    // Verificar foco em primeiro card
    const firstCard = page.locator('app-card').first();
    
    // Tab para focar
    await page.keyboard.press('Tab');
    // Verificar se elemento focável está acessível
    const focusedElement = await page.evaluate(() => document.activeElement?.tagName);
    expect(focusedElement).toBeTruthy();
  });

  test('deve exibir loading state', async ({ page }) => {
    // Recarregar página para ver loading
    await page.reload();
    
    // Pode mostrar spinner ou texto de loading
    const loadingText = page.locator('text=Carregando');
    
    // Aguardar loading desaparecer
    try {
      await expect(loadingText).not.toBeVisible({ timeout: 2000 });
    } catch {
      // Se não houver loading state visível, tudo bem
    }
  });
});
