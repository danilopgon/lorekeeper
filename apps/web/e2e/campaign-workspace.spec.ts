import { expect, test } from '@playwright/test';

test('creates a campaign and moves between its unavailable workspace sections', async ({
  page,
}) => {
  const campaignName = `La Corona de Ceniza ${crypto.randomUUID()}`;

  await page.goto('/campaigns');

  await page.getByLabel('Nombre de la campaña').fill(campaignName);
  const createCampaign = page.waitForResponse(
    (response) =>
      response.url().endsWith('/api/campaigns') && response.request().method() === 'POST',
  );
  await page.getByRole('button', { name: 'Crear campaña' }).click();
  await expect((await createCampaign).status()).toBe(201);

  const campaign = page.getByRole('listitem').filter({ hasText: campaignName });
  await expect(campaign).toBeVisible();

  await campaign.getByRole('link', { name: 'Chat' }).click();
  await expect(page).toHaveTitle('Chat — Lorekeeper');
  await expect(page.getByRole('heading', { level: 1, name: campaignName })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'El chat aún no está disponible' })).toBeVisible();
  await expect(page.getByRole('textbox')).toHaveCount(0);
  await expect(page.getByRole('button')).toHaveCount(0);

  const sections = page.getByRole('navigation', { name: 'Secciones de la campaña' });
  await sections.getByRole('link', { name: 'Fuentes' }).click();
  await expect(page).toHaveTitle('Fuentes — Lorekeeper');
  await expect(page.getByRole('heading', { level: 1, name: campaignName })).toBeVisible();
  await expect(
    page.getByRole('heading', { name: 'Las fuentes aún no están disponibles' }),
  ).toBeVisible();
  await expect(sections.getByRole('link', { name: 'Fuentes' })).toHaveAttribute(
    'aria-current',
    'page',
  );
  await expect(page.locator('input[type="file"]')).toHaveCount(0);
  await expect(page.getByRole('button')).toHaveCount(0);

  await page.getByRole('link', { name: 'Todas las campañas' }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Elige tu crónica' })).toBeVisible();
});
