import { expect, test } from '@playwright/test';

test('shows the campaign selection entry point', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByRole('heading', { name: 'Choose your campaign chronicle' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Create campaign' })).toBeVisible();
});
