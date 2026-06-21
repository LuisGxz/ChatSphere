import { expect, test } from '@playwright/test';
import { loginAs } from './helpers';

test.describe('Guided demo layer', () => {
  test('explore panel opens and starts the tour', async ({ page }) => {
    await loginAs(page, 'Owner');

    await page.getByRole('button', { name: 'How to explore' }).first().click();
    await expect(page.getByText(/real here/i)).toBeVisible();

    await page.getByRole('button', { name: 'Start guided tour' }).click();
    await expect(page.getByText('Welcome to ChatSphere')).toBeVisible();

    await page.getByRole('button', { name: 'Next' }).click();
    await expect(page.getByText('Channels & DMs')).toBeVisible();

    await page.getByRole('button', { name: 'Skip' }).click();
    await expect(page.getByText('Channels & DMs')).toHaveCount(0);
  });
});
