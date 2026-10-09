import { expect, test } from '@playwright/test';
test('live local API: create, reload and read a persisted invoice', async ({ page }, testInfo) => {
  test.skip(
    process.env['E2E_LIVE'] !== '1',
    'Requires an explicitly running local API and SQL Server; no mocks.',
  );
  await page.goto('/facturas/nueva');
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
  await page.reload();
  await expect(page.getByRole('heading', { name: heading!, exact: true })).toBeVisible();
  await expect(page.getByText('Prueba Automatizada')).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('invoice-live.png'), fullPage: true });
});
