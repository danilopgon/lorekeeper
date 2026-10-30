import { expect, test } from '@playwright/test';

test('shows the campaign selection entry point in Spanish', async ({ page }) => {
  await page.goto('/');

  await expect(page.locator('html')).toHaveAttribute('lang', 'es');
  await expect(page.getByRole('heading', { level: 1, name: 'Elige tu crónica' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Crear campaña' })).toBeVisible();
});
