import { expect, test, Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
const envelope = (data: unknown) => ({ isSuccess: true, statusCode: 200, message: '', data });
const invoice = {
  id: 'demo-id',
  number: 42,
  date: '2026-10-09T12:00:00',
  document: 'TEST-123',
  firstName: 'Cliente',
  lastName: 'Ficticio',
  address: 'Calle de ejemplo 1',
  phone: '3000000000',
  details: [{ code: 101, quantity: 2, unitPrice: 250000, lineTotal: 500000 }],
  gross: 500000,
  discount: 25000,
  tax: 90250,
  total: 565250,
};
async function mockLocal(page: Page) {
  await page.route('**/api/v1/session', (route) =>
    route.fulfill({
      json: envelope({
        mode: 'local',
        authenticated: true,
        canEvaluate: true,
        canUseRealAccounts: false,
        csrfToken: 'test-csrf',
      }),
    }),
  );
  await page.route('**/api/v1/demo/invoices', (route) =>
    route.fulfill({ json: envelope({ items: [invoice], page: 1, pageSize: 10, totalCount: 1 }) }),
  );
  await page.route('**/api/v1/invoices/preview', (route) =>
    route.fulfill({
      json: envelope({ gross: 500000, discount: 25000, tax: 90250, total: 565250 }),
    }),
  );
  await page.route('**/api/v1/invoices/42', (route) =>
    route.fulfill({ json: envelope({ invoice }) }),
  );
}
async function fillInvoice(page: Page) {
  await page.getByLabel('Documento', { exact: false }).fill('TEST-123');
  await page.getByLabel('Nombres', { exact: false }).fill('Cliente');
  await page.getByLabel('Apellidos', { exact: false }).fill('Ficticio');
  await page.getByLabel('Dirección', { exact: false }).fill('Calle de ejemplo 1');
  await page.getByLabel('Teléfono', { exact: false }).fill('3000000000');
  await page.getByLabel('Código del artículo 1').fill('101');
  await page.getByLabel('Cantidad del artículo 1').fill('2');
  await page.getByLabel('Precio del artículo 1').fill('250000');
  await expect(page.getByText('Actualizando importes…')).toBeHidden();
}
test.describe('UI with controlled API responses (not live SQL evidence)', () => {
  test('saves the server totals, sends CSRF and preserves retry identity after a timeout', async ({
    page,
  }) => {
    await mockLocal(page);
    const keys: string[] = [];
    let attempt = 0;
    await page.route('**/api/v1/invoices', async (route) => {
      keys.push(route.request().headers()['idempotency-key']);
      expect(route.request().headers()['x-csrf-token']).toBe('test-csrf');
      expect(route.request().postDataJSON()).not.toHaveProperty('total');
      if (++attempt === 1)
        await route.fulfill({
          status: 504,
          json: { title: 'Solicitud interrumpida. Puedes reintentar.' },
        });
      else await route.fulfill({ status: 201, json: envelope({ invoice }) });
    });
    await page.goto('/facturas/nueva');
    await fillInvoice(page);
    await expect(page.getByText(/\$\s*565\.250,00/)).toBeVisible();
    await page.getByRole('button', { name: 'Grabar factura' }).click();
    await expect(page.getByRole('alert')).toContainText('interrumpida');
    await page.getByRole('button', { name: 'Grabar factura' }).click();
    await expect(page.getByRole('heading', { name: 'Factura #42', exact: true })).toBeVisible();
    expect(keys).toHaveLength(2);
    expect(keys[0]).toBe(keys[1]);
  });
  test('displays validation errors without losing the entered customer', async ({ page }) => {
    await mockLocal(page);
    await page.route('**/api/v1/invoices', (route) =>
      route.fulfill({
        status: 400,
        json: {
          title: 'Revisa el documento.',
          fieldErrors: { document: ['Documento no admitido.'] },
          traceId: 'test-123',
        },
      }),
    );
    await page.goto('/facturas/nueva');
    await fillInvoice(page);
    await page.getByRole('button', { name: 'Grabar factura' }).click();
    await expect(page.getByText('Documento no admitido.')).toBeVisible();
    await expect(page.locator('#firstName')).toHaveValue('Cliente');
    await expect(page.locator('#document')).toHaveAttribute('aria-invalid', 'true');
  });
  test('mobile layout remains within the viewport and exposes navigation', async ({ page }) => {
    await mockLocal(page);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/facturas/nueva');
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
    await page.getByRole('button', { name: 'Abrir navegación' }).click();
    await expect(
      page
        .getByRole('navigation', { name: 'Navegación principal' })
        .getByRole('link', { name: 'Facturas', exact: true }),
    ).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(page.getByRole('button', { name: 'Abrir navegación' })).toHaveAttribute(
      'aria-expanded',
      'false',
    );
    await page.getByRole('button', { name: 'Abrir navegación' }).focus();
    await page.keyboard.press('Tab');
    expect(
      await page
        .locator('.sidebar')
        .evaluate((element) => element.contains(document.activeElement)),
    ).toBe(false);
  });
  test('shows unavailable local session honestly and prevents invoice navigation', async ({
    page,
  }) => {
    await page.route('**/api/v1/session', (route) =>
      route.fulfill({ status: 503, json: { title: 'No disponible' } }),
    );
    await page.goto('/facturas/nueva');
    await expect(page).toHaveURL(/\/acceso$/);
    await expect(page.getByText('Sin conexión verificada')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Grabar factura' })).toHaveCount(0);
  });
  test('ignores an obsolete preview when a newer amount is entered', async ({ page }) => {
    await mockLocal(page);
    let resolveOld: (() => void) | undefined;
    let oldSettled: (() => void) | undefined;
    const oldFinished = new Promise<void>((resolve) => {
      oldSettled = resolve;
    });
    await page.route('**/api/v1/invoices/preview', async (route) => {
      const price = route.request().postDataJSON().details[0].unitPrice;
      if (price === 250000)
        await new Promise<void>((resolve) => {
          resolveOld = resolve;
        });
      try {
        await route.fulfill({
          json: envelope({ gross: price, discount: 0, tax: 0, total: price }),
        });
      } catch {
        /* cancelled request */
      } finally {
        if (price === 250000) oldSettled?.();
      }
    });
    await page.goto('/facturas/nueva');
    await page.getByLabel('Código del artículo 1').fill('101');
    const firstRequest = page.waitForRequest('**/api/v1/invoices/preview');
    await page.getByLabel('Precio del artículo 1').fill('250000');
    await firstRequest;
    await page.getByLabel('Precio del artículo 1').fill('100');
    await expect(page.locator('.grand-total dd')).toHaveText(/\$\s*100,00/);
    resolveOld?.();
    await oldFinished;
    await expect(page.locator('.grand-total dd')).toHaveText(/\$\s*100,00/);
  });
  test('accounts accepts a simulated request and consults the actual response', async ({
    page,
  }, testInfo) => {
    await mockLocal(page);
    let created = false;
    await page.route('**/api/v1/accounts?**', (route) =>
      route.fulfill({
        json: envelope({
          mode: 'simulated',
          accounts: created
            ? [
                {
                  code: 'DEMO001',
                  name: 'Cuenta ficticia',
                  document: '123',
                  email: 'demo@example.test',
                  countryCode: '057',
                  cityCode: '11001',
                },
              ]
            : [],
        }),
      }),
    );
    await page.route('**/api/v1/accounts', (route) => {
      expect(route.request().postDataJSON().mode).toBe('simulated');
      expect(route.request().headers()['x-csrf-token']).toBe('test-csrf');
      created = true;
      return route.fulfill({
        json: envelope({ mode: 'simulated', code: 'DEMO001', accepted: true }),
      });
    });
    await page.goto('/cuentas');
    await expect(page.getByText('No encontramos cuentas')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Real', exact: true })).toHaveCount(0);
    await page.getByRole('button', { name: 'Crear cuenta', exact: true }).click();
    await page.locator('#account-code').fill('DEMO001');
    await page.locator('#identificationType').fill('06');
    await page.locator('#account-document').fill('123');
    await page.locator('#account-name').fill('Cuenta ficticia');
    await page.locator('#email').fill('demo@example.test');
    await page.locator('#countryCode').fill('057');
    await page.locator('#departmentCode').fill('11');
    await page.locator('#cityCode').fill('11001');
    expect(
      (await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa']).analyze())
        .violations,
    ).toEqual([]);
    await page.evaluate(() => window.scrollTo(0, 0));
    await page.screenshot({ path: testInfo.outputPath('accounts-desktop.png'), fullPage: true });
    await page.locator('form').getByRole('button', { name: 'Crear cuenta', exact: true }).click();
    await expect(page.getByText('Solicitud aceptada: DEMO001')).toBeVisible();
    await page.getByRole('button', { name: 'Consultar esta cuenta' }).click();
    await expect(page.getByRole('cell', { name: 'Cuenta ficticia', exact: false })).toBeVisible();
    await expect(page.getByText('Respuesta simulada')).toBeVisible();
  });
  test('invoice and demo screens pass WCAG AA automated checks', async ({ page }, testInfo) => {
    await mockLocal(page);
    await page.goto('/');
    await expect(
      page.getByRole('heading', { name: 'Así se ve una factura registrada' }),
    ).toBeVisible();
    expect(
      (await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa']).analyze())
        .violations,
    ).toEqual([]);
    await page.evaluate(() => window.scrollTo(0, 0));
    await page.screenshot({ path: testInfo.outputPath('demo-desktop.png'), fullPage: true });
    await page.goto('/facturas/nueva');
    await fillInvoice(page);
    expect(
      (await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa']).analyze())
        .violations,
    ).toEqual([]);
    await page.evaluate(() => window.scrollTo(0, 0));
    await page.screenshot({ path: testInfo.outputPath('invoice-desktop.png'), fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    expect(
      (await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa']).analyze())
        .violations,
    ).toEqual([]);
    await page.evaluate(() => window.scrollTo(0, 0));
    await page.screenshot({ path: testInfo.outputPath('invoice-mobile.png'), fullPage: true });
  });
});
