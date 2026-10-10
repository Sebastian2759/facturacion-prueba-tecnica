import { expect, test } from '@playwright/test';

test('live Novasoft: reads the previously authorized account without creating another', async ({
  page,
}, testInfo) => {
  test.skip(
    process.env['E2E_NOVASOFT'] !== '1',
    'Requires configured local Novasoft credentials and an existing test account.',
  );
  const code = process.env['E2E_ACCOUNT_CODE'];
  if (!code || !/^[A-Za-z0-9_-]{1,15}$/.test(code))
    throw new Error('Set E2E_ACCOUNT_CODE to the authorized existing account.');
  const writes: string[] = [];
  page.on('request', (request) => {
    if (request.method() === 'POST' && request.url().includes('/api/v1/accounts'))
      writes.push(request.url());
  });
  await page.goto('/cuentas');
  await expect(page.getByRole('heading', { name: 'Directorio de cuentas' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Consultar', exact: true })).toBeEnabled();
  await page.getByRole('textbox', { name: 'Código de cuenta', exact: true }).fill(code);
  await page.getByRole('button', { name: 'Consultar', exact: true }).click();
  await expect(page.getByRole('cell', { name: code, exact: true })).toBeVisible();
  await expect(page.getByText('Respuesta de Novasoft')).toBeVisible();
  await expect(page.getByText('1 resultados')).toBeVisible();
  expect(writes).toEqual([]);
  await page.screenshot({ path: testInfo.outputPath('accounts-live.png'), fullPage: true });
});
