import { expect, test, type Page, type Response } from '@playwright/test';

interface CspViolation {
  directive: string;
  blockedURI: string;
}

async function expectNonceStyles(page: Page, response: Response | null): Promise<string> {
  await expect(page.locator('body')).toHaveCSS('background-color', 'rgb(244, 246, 243)');
  await expect(page.locator('.sidebar')).toHaveCSS('background-color', 'rgb(16, 47, 46)');
  await expect(page.locator('.sidebar')).toHaveCSS('display', 'flex');
  const styles = await page.evaluate(() => ({
    nonce: document.querySelector('app-root')?.getAttribute('ngCspNonce') ?? '',
    nonces: [...document.querySelectorAll('style')].map((style) => style.nonce),
    inlineScripts: document.querySelectorAll('script:not([src])').length,
  }));
  expect(styles.nonce.length).toBeGreaterThanOrEqual(32);
  expect(styles.nonce).not.toBe('__CSP_NONCE__');
  expect(styles.nonces.length).toBeGreaterThan(0);
  expect(styles.nonces.every((nonce) => nonce === styles.nonce)).toBe(true);
  expect(styles.inlineScripts).toBe(0);
  expect(response).not.toBeNull();
  const policy = response!.headers()['content-security-policy'];
  expect(policy).toContain(`'nonce-${styles.nonce}'`);
  expect(policy).not.toContain("'unsafe-inline'");
  return styles.nonce;
}

test('live local API: create, reload and read a persisted invoice', async ({ page }, testInfo) => {
  test.skip(
    process.env['E2E_LIVE'] !== '1',
    'Requires an explicitly running local API and SQL Server; no mocks.',
  );
  const violations: CspViolation[] = [];
  await page.exposeFunction('recordCspViolation', (violation: CspViolation) => {
    violations.push(violation);
  });
  await page.addInitScript(() => {
    document.addEventListener('securitypolicyviolation', (event) => {
      const reporter = window as typeof window & {
        recordCspViolation: (violation: CspViolation) => Promise<void>;
      };
      void reporter.recordCspViolation({
        directive: event.effectiveDirective,
        blockedURI: event.blockedURI,
      });
    });
  });
  const initialResponse = await page.goto('/facturas/nueva');
  await page.locator('#document').fill('E2E-DEMO');
  await page.locator('#firstName').fill('Prueba');
  await page.locator('#lastName').fill('Automatizada');
  await page.locator('#address').fill('Dirección ficticia');
  await page.locator('#phone').fill('3000000000');
  await page.getByLabel('Código del artículo 1').fill('1001');
  await page.getByLabel('Precio del artículo 1').fill('500000');
  await expect(page.getByText(/\$\s*565\.250,00/)).toBeVisible();
  await page.getByRole('button', { name: 'Grabar factura' }).click();
  await expect(page.getByRole('heading', { name: /^Factura #\d+$/ })).toBeVisible();
  const heading = await page.getByRole('heading', { name: /^Factura #\d+$/ }).textContent();
  const initialNonce = await expectNonceStyles(page, initialResponse);
  expect(violations).toEqual([]);
  const reloadedResponse = await page.reload();
  await expect(page.getByRole('heading', { name: heading!, exact: true })).toBeVisible();
  await expect(page.getByText('Prueba Automatizada')).toBeVisible();
  const reloadedNonce = await expectNonceStyles(page, reloadedResponse);
  expect(reloadedNonce).not.toBe(initialNonce);
  expect(violations).toEqual([]);
  await page.screenshot({ path: testInfo.outputPath('invoice-live.png'), fullPage: true });
});
