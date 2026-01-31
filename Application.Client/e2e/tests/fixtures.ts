import { Page } from '@playwright/test';

/**
 * Fixture compartilhada para funcionalidades comuns
 */

export async function loginAs(page: Page, email: string, password: string) {
  await page.goto('/login', { waitUntil: 'networkidle' });
  await page.fill('input[type="email"]', email);
  await page.fill('input[type="password"]', password);
  await page.click('button[type="submit"]');
  await page.waitForURL(/.*dashboard/, { timeout: 10000 });
}

export async function logout(page: Page) {
  await page.click('button[aria-label="menu"]');
  await page.click('text=Sair');
  await page.waitForURL(/.*login/, { timeout: 10000 });
}

export async function getAuthToken(page: Page): Promise<string | null> {
  return await page.evaluate(() => localStorage.getItem('auth_token'));
}

export async function setAuthToken(page: Page, token: string) {
  await page.evaluate((t) => localStorage.setItem('auth_token', t), token);
}

export async function clearAuthToken(page: Page) {
  await page.evaluate(() => localStorage.removeItem('auth_token'));
}

export async function waitForApiResponse(
  page: Page,
  urlPattern: string | RegExp,
  timeout = 10000
) {
  return await page.waitForResponse(
    (response) => {
      const url = response.url();
      if (typeof urlPattern === 'string') {
        return url.includes(urlPattern);
      }
      return urlPattern.test(url);
    },
    { timeout }
  );
}
