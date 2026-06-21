import { Page, expect } from '@playwright/test';

export type DemoRole = 'Owner' | 'Admin' | 'Member';

/** Sign in via a demo-account button; suppress the auto-tour for deterministic runs. */
export async function loginAs(page: Page, role: DemoRole): Promise<void> {
  await page.addInitScript(() => {
    try {
      localStorage.setItem('cs-tour-seen', '1');
      localStorage.setItem('cs-lang', 'en');
    } catch {
      /* ignore */
    }
  });
  await page.goto('/login');
  await page.getByText(role, { exact: true }).first().click();
  await page.waitForURL('**/app', { timeout: 20_000 });
  // The channels sidebar header is the server name once loaded.
  await expect(page.getByText('Driftwood Studio')).toBeVisible({ timeout: 15_000 });
}
