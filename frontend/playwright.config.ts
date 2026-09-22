import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  timeout: 60000,
  expect: { timeout: 10000 },
  use: {
    baseURL: 'http://127.0.0.1:4174',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    reducedMotion: 'reduce',
  },
  projects: [{ name: 'mobile-chromium', use: { ...devices['iPhone 13'], defaultBrowserType: 'chromium' } }],
  webServer: [
    {
      command: 'dotnet run --project ../backend/CalisApi --no-launch-profile --urls http://127.0.0.1:5261',
      url: 'http://127.0.0.1:5261/api/health', timeout: 120000, reuseExistingServer: false,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ConnectionStrings__DefaultConnection: 'Server=(localdb)\\MSSQLLocalDB;Database=CalisAppE2E;Trusted_Connection=True;TrustServerCertificate=True',
        Serilog__MinimumLevel__Default: 'Warning',
      },
    },
    {
      command: 'npm run build && npm run preview -- --host 127.0.0.1 --port 4174 --strictPort',
      url: 'http://127.0.0.1:4174', timeout: 120000, reuseExistingServer: false,
      env: { CALIS_API_URL: 'http://127.0.0.1:5261' },
    },
  ],
})
