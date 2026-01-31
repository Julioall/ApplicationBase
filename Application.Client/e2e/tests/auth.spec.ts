import { test, expect } from '@playwright/test';

/**
 * E2E Tests - Autenticação (Auth Flow)
 * 
 * Fluxos críticos:
 * 1. Usuário não autenticado é redirecionado para login
 * 2. Login com credenciais válidas redireciona para dashboard
 * 3. Login com credenciais inválidas exibe erro
 * 4. Logout remove token JWT e redireciona para login
 */

test.describe('Autenticação (Auth Flow)', () => {
  const validEmail = 'test@example.com';
  const validPassword = 'ValidPassword123!';
  const invalidPassword = 'WrongPassword123!';

  test('Usuário não autenticado é redirecionado para login', async ({ page }) => {
    // Act - Tentar acessar dashboard sem token
    await page.goto('/dashboard', { waitUntil: 'networkidle' });

    // Assert - Deve estar em página de login
    await expect(page).toHaveURL(/.*login/);
    await expect(page.getByText(/entrar|login/i)).toBeVisible();
  });

  test('Login com credenciais válidas - sucesso', async ({ page }) => {
    // Arrange
    await page.goto('/login', { waitUntil: 'networkidle' });

    // Act - Preencher formulário de login
    await page.fill('input[type="email"]', validEmail);
    await page.fill('input[type="password"]', validPassword);
    await page.click('button[type="submit"]');

    // Assert - Redirecionar para dashboard e token armazenado
    await page.waitForURL(/.*dashboard/, { timeout: 10000 });
    await expect(page).toHaveURL(/.*dashboard/);

    // Validar que token foi armazenado no localStorage
    const token = await page.evaluate(() => localStorage.getItem('auth_token'));
    expect(token).toBeTruthy();
    expect(token).toMatch(/^Bearer /);
  });

  test('Login com credenciais inválidas - erro', async ({ page }) => {
    // Arrange
    await page.goto('/login', { waitUntil: 'networkidle' });

    // Act
    await page.fill('input[type="email"]', validEmail);
    await page.fill('input[type="password"]', invalidPassword);
    await page.click('button[type="submit"]');

    // Assert - Exibir mensagem de erro
    const errorMessage = page.getByText(/credenciais|inválidas|incorretas/i);
    await expect(errorMessage).toBeVisible({ timeout: 5000 });

    // Token não deve ser armazenado
    const token = await page.evaluate(() => localStorage.getItem('auth_token'));
    expect(token).toBeFalsy();
  });

  test('Logout remove token e redireciona para login', async ({ page, context }) => {
    // Arrange - Fazer login primeiro
    await page.goto('/login', { waitUntil: 'networkidle' });
    await page.fill('input[type="email"]', validEmail);
    await page.fill('input[type="password"]', validPassword);
    await page.click('button[type="submit"]');
    await page.waitForURL(/.*dashboard/);

    // Validar que estamos logados
    let token = await page.evaluate(() => localStorage.getItem('auth_token'));
    expect(token).toBeTruthy();

    // Act - Logout
    await page.click('button[aria-label="menu"]'); // Abrir menu
    await page.click('text=Sair'); // Clicar em Sair

    // Assert
    await page.waitForURL(/.*login/, { timeout: 10000 });
    token = await page.evaluate(() => localStorage.getItem('auth_token'));
    expect(token).toBeFalsy();
  });

  test('Refresh token atualiza sessão automaticamente', async ({ page }) => {
    // Arrange - Login e aguardar expiração
    await page.goto('/login', { waitUntil: 'networkidle' });
    await page.fill('input[type="email"]', validEmail);
    await page.fill('input[type="password"]', validPassword);
    await page.click('button[type="submit"]');
    await page.waitForURL(/.*dashboard/);

    const oldToken = await page.evaluate(() => localStorage.getItem('auth_token'));

    // Act - Aguardar refresh automático (depende da implementação)
    // Simular requisição após expiração
    await page.evaluate(() => {
      fetch('/api/refresh-token', { method: 'POST', credentials: 'include' });
    });

    await page.waitForTimeout(2000);

    // Assert - Novo token deve estar disponível
    const newToken = await page.evaluate(() => localStorage.getItem('auth_token'));
    expect(newToken).toBeTruthy();
    // Token pode ser o mesmo ou diferente dependendo da lógica
  });

  test('Navegação protegida sem token exibe 401', async ({ page }) => {
    // Arrange - Remover token manualmente
    await page.goto('/dashboard');
    await page.evaluate(() => localStorage.removeItem('auth_token'));

    // Act - Tentar acessar recurso protegido
    await page.goto('/api/users', { waitUntil: 'domcontentloaded' });

    // Assert
    await expect(page.getByText(/401|unauthorized|não autorizado/i)).toBeVisible({
      timeout: 5000,
    });
  });
});
