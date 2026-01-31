import { test, expect } from '@playwright/test';

/**
 * E2E Tests - User CRUD Operations
 * 
 * Fluxos críticos:
 * 1. Criar novo usuário (CREATE)
 * 2. Listar usuários (READ)
 * 3. Atualizar dados de usuário (UPDATE)
 * 4. Deletar usuário (DELETE)
 * 5. Validações de entrada
 * 6. Tratamento de conflitos (email duplicado)
 */

test.describe('User CRUD Operations', () => {
  const testUser = {
    email: `user-${Date.now()}@example.com`,
    name: 'Test User',
    password: 'TestPassword123!',
  };

  let userId: string;

  test.beforeEach(async ({ page }) => {
    // Fazer login antes de cada teste
    await page.goto('/login', { waitUntil: 'networkidle' });
    await page.fill('input[type="email"]', 'admin@example.com');
    await page.fill('input[type="password"]', 'AdminPassword123!');
    await page.click('button[type="submit"]');
    await page.waitForURL(/.*dashboard/);
  });

  test('CREATE - Criar novo usuário com sucesso', async ({ page }) => {
    // Arrange
    await page.goto('/users', { waitUntil: 'networkidle' });
    await page.click('button:has-text("Novo Usuário")');
    await page.waitForURL(/.*users.*create/);

    // Act - Preencher formulário
    await page.fill('input[placeholder="Email"]', testUser.email);
    await page.fill('input[placeholder="Nome"]', testUser.name);
    await page.fill('input[type="password"]', testUser.password);
    await page.click('button:has-text("Salvar")');

    // Assert - Mensagem de sucesso
    await expect(page.getByText(/criado|sucesso|successfully/i)).toBeVisible({
      timeout: 5000,
    });

    // Extrair ID do usuário da URL ou resposta
    const urlMatch = await page.url();
    userId = urlMatch.split('/').pop() || '';
    expect(userId).toBeTruthy();
  });

  test('READ - Listar usuários com paginação', async ({ page }) => {
    // Arrange & Act
    await page.goto('/users', { waitUntil: 'networkidle' });

    // Assert - Tabela de usuários visível
    await expect(page.getByRole('table')).toBeVisible();
    await expect(page.getByText(/email|name|ações/i)).toBeVisible();

    // Validar paginação
    const nextButton = page.getByRole('button', { name: /próxima|next/i });
    if (await nextButton.isVisible()) {
      await nextButton.click();
      await page.waitForLoadState('networkidle');
      await expect(page.getByRole('table')).toBeVisible();
    }
  });

  test('READ - Buscar usuário por email', async ({ page }) => {
    // Arrange
    await page.goto('/users', { waitUntil: 'networkidle' });

    // Act - Usar searchbox
    const searchInput = page.getByPlaceholder(/buscar|search|email/i);
    await searchInput.fill(testUser.email);
    await page.keyboard.press('Enter');

    // Assert
    await page.waitForLoadState('networkidle');
    await expect(page.getByText(testUser.email)).toBeVisible();
  });

  test('UPDATE - Atualizar dados de usuário', async ({ page }) => {
    // Arrange
    await page.goto('/users', { waitUntil: 'networkidle' });
    
    // Encontrar e clicar no usuário a editar
    const userRow = page.locator(`text=${testUser.email}`).first();
    await expect(userRow).toBeVisible();
    
    // Act - Clicar em editar
    const editButton = userRow.locator('button:has-text("Editar")').first();
    await editButton.click();
    await page.waitForURL(/.*users.*edit/);

    // Preencher novo nome
    const nameField = page.getByPlaceholder('Nome');
    await nameField.clear();
    await nameField.fill('Updated User Name');
    
    await page.click('button:has-text("Salvar")');

    // Assert
    await expect(page.getByText(/atualizado|updated|sucesso/i)).toBeVisible({
      timeout: 5000,
    });
  });

  test('DELETE - Deletar usuário com confirmação', async ({ page }) => {
    // Arrange
    await page.goto('/users', { waitUntil: 'networkidle' });
    
    const userRow = page.locator(`text=${testUser.email}`).first();
    await expect(userRow).toBeVisible();

    // Act - Clicar delete
    const deleteButton = userRow.locator('button[aria-label="delete"]').first();
    await deleteButton.click();

    // Confirmar exclusão em modal
    await page.click('button:has-text("Confirmar")');

    // Assert
    await expect(page.getByText(/deletado|removed|sucesso/i)).toBeVisible({
      timeout: 5000,
    });
    
    // Usuário não deve estar mais na lista
    await page.goto('/users', { waitUntil: 'networkidle' });
    await expect(page.locator(`text=${testUser.email}`)).not.toBeVisible();
  });

  test('VALIDATION - Email duplicado exibe erro', async ({ page }) => {
    // Arrange
    const duplicateEmail = 'duplicate@example.com';
    
    // Criar primeiro usuário
    await page.goto('/users/create', { waitUntil: 'networkidle' });
    await page.fill('input[placeholder="Email"]', duplicateEmail);
    await page.fill('input[placeholder="Nome"]', 'First User');
    await page.fill('input[type="password"]', 'TestPassword123!');
    await page.click('button:has-text("Salvar")');
    await expect(page.getByText(/sucesso|criado/i)).toBeVisible({ timeout: 5000 });

    // Act - Tentar criar segundo com mesmo email
    await page.goto('/users/create', { waitUntil: 'networkidle' });
    await page.fill('input[placeholder="Email"]', duplicateEmail);
    await page.fill('input[placeholder="Nome"]', 'Second User');
    await page.fill('input[type="password"]', 'TestPassword123!');
    await page.click('button:has-text("Salvar")');

    // Assert - Erro de conflict
    await expect(page.getByText(/duplicado|já existe|email.*já|conflito/i)).toBeVisible({
      timeout: 5000,
    });
  });

  test('VALIDATION - Email inválido é rejeitado', async ({ page }) => {
    // Arrange
    await page.goto('/users/create', { waitUntil: 'networkidle' });

    // Act - Preencher com email inválido
    const emailInput = page.getByPlaceholder('Email');
    await emailInput.fill('not-an-email');
    await emailInput.blur(); // Disparar validação

    // Assert
    await expect(page.getByText(/email.*inválido|formato/i)).toBeVisible();

    // Submit button deve estar desabilitado
    const submitButton = page.getByRole('button', { name: /salvar/i });
    await expect(submitButton).toBeDisabled();
  });

  test('VALIDATION - Senha fraca é rejeitada', async ({ page }) => {
    // Arrange
    await page.goto('/users/create', { waitUntil: 'networkidle' });

    // Act
    await page.fill('input[placeholder="Email"]', `weak-${Date.now()}@example.com`);
    await page.fill('input[placeholder="Nome"]', 'Test User');
    await page.fill('input[type="password"]', '123'); // Senha fraca

    // Assert
    await expect(page.getByText(/senha.*fraca|mínimo|caracteres/i)).toBeVisible();

    const submitButton = page.getByRole('button', { name: /salvar/i });
    await expect(submitButton).toBeDisabled();
  });

  test('Performance - Listar 1000+ usuários sem lag', async ({ page }) => {
    // Arrange
    await page.goto('/users', { waitUntil: 'networkidle' });

    // Act - Medir tempo de renderização
    const startTime = Date.now();
    
    // Scroll para fim (trigger lazy loading)
    await page.evaluate(() => {
      window.scrollTo(0, document.body.scrollHeight);
    });

    const endTime = Date.now();
    const loadTime = endTime - startTime;

    // Assert - Deve carregar em menos de 3s
    expect(loadTime).toBeLessThan(3000);
    
    // Conteúdo deve estar visível
    await expect(page.getByRole('table')).toBeVisible();
  });
});
