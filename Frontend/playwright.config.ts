import { defineConfig, devices } from '@playwright/test';
export default defineConfig({
  testDir: './e2e', fullyParallel: false, retries: process.env['CI'] ? 1 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: { baseURL: process.env['E2E_BASE_URL'] ?? 'http://127.0.0.1:4200', trace: 'retain-on-failure', screenshot: 'only-on-failure' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'], channel: process.env['PLAYWRIGHT_CHANNEL'] } }],
  webServer: process.env['E2E_BASE_URL'] ? undefined : { command: 'npm run start', url: 'http://127.0.0.1:4200', reuseExistingServer: !process.env['CI'], timeout: 90000 },
});
