import { expect, test } from '@playwright/test';

test('creates a campaign and opens its unavailable workspace shells', async ({ page }) => {
  const campaignName = `Ash Crown ${crypto.randomUUID()}`;

  await page.goto('/campaigns');

  await page.getByLabel('Campaign name').fill(campaignName);
  const createCampaign = page.waitForResponse(
    (response) =>
      response.url().endsWith('/api/campaigns') && response.request().method() === 'POST',
  );
  await page.getByRole('button', { name: 'Create campaign' }).click();
  await expect((await createCampaign).status()).toBe(201);

  const campaign = page.getByRole('listitem').filter({ hasText: campaignName });
  await expect(campaign).toBeVisible();

  await campaign.getByRole('link', { name: 'Open Chat' }).click();
  await expect(page.getByRole('heading', { name: 'Chat is not available yet' })).toBeVisible();
  await expect(
    page.getByText(
      'AI chat, source-backed querying, retrieval, citations, conversation history, streaming, and draft persistence are unavailable in this slice.',
    ),
  ).toBeVisible();
  await expect(page.getByRole('textbox')).toHaveCount(0);
  await expect(page.getByRole('button')).toHaveCount(0);

  await page.getByRole('link', { name: 'Back to campaigns' }).click();
  await campaign.getByRole('link', { name: 'Open Sources' }).click();
  await expect(page.getByRole('heading', { name: 'Sources are not available yet' })).toBeVisible();
  await expect(
    page.getByText(
      'Ingestion, upload, paste, Notion import, source updates, removal, indexing, retry, progress, and source lifecycle actions are unavailable in this slice.',
    ),
  ).toBeVisible();
  await expect(page.locator('input[type="file"]')).toHaveCount(0);
  await expect(page.getByRole('button')).toHaveCount(0);
});
