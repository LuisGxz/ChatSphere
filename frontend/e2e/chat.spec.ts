import { expect, test } from '@playwright/test';
import { loginAs } from './helpers';

test.describe('Chat', () => {
  test('send a message and see it appear in the channel', async ({ page }) => {
    await loginAs(page, 'Owner');
    // design-crit is open by default; wait for seeded history.
    await expect(page.getByText('Dropped v3 of the onboarding flow in Figma.')).toBeVisible({ timeout: 15_000 });

    const text = `E2E hello ${Date.now() % 100000}`;
    await page.locator('textarea').fill(text);
    await page.keyboard.press('Enter');

    // The message round-trips through SignalR and renders.
    await expect(page.getByText(text)).toBeVisible({ timeout: 10_000 });
  });

  test('switching channels loads that channel', async ({ page }) => {
    await loginAs(page, 'Owner');
    await page.getByRole('button', { name: /announcements/ }).click();
    await expect(page.getByText('Welcome to Driftwood Studio')).toBeVisible({ timeout: 10_000 });
  });
});
