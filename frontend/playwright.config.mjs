import { defineConfig } from '@playwright/test'

const webPort = process.env.E2E_WEB_PORT || '5173'
const apiUrl = process.env.E2E_API_URL || 'http://127.0.0.1:5077'

export default defineConfig({
  testDir: './e2e',
  testMatch: '**/*.e2e.mjs',
  fullyParallel: false,
  workers: 1,
  timeout: 60000,
  use: { baseURL: `http://127.0.0.1:${webPort}`, channel: process.env.E2E_BROWSER_CHANNEL, trace: 'retain-on-failure' },
  webServer: {
    command: `npm run dev -- --port ${webPort} --strictPort`,
    url: `http://127.0.0.1:${webPort}`,
    reuseExistingServer: false,
    env: { VITE_API_URL: apiUrl },
  },
})
