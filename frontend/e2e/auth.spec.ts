import { expect, test } from '@playwright/test';
import { loginAs } from './helpers';

test.describe('Auth', () => {
  test('owner signs in and sees the workspace', async ({ page }) => {
    await loginAs(page, 'Owner');
    await expect(page.getByText('design-crit').first()).toBeVisible();
    // Seeded history loaded into the default channel.
    await expect(page.getByText('Dropped v3 of the onboarding flow in Figma.')).toBeVisible({ timeout: 15_000 });
  });

  test('invalid credentials are rejected', async ({ page }) => {
    await page.addInitScript(() => localStorage.setItem('cs-tour-seen', '1'));
    await page.goto('/login');
    await page.getByPlaceholder('you@studio.com').fill('luis@chatsphere.app');
    await page.getByPlaceholder('••••••••').fill('wrong-password');
    await page.getByRole('button', { name: 'Sign in' }).click();
    await expect(page.getByText('Invalid email or password.')).toBeVisible();
  });
});
