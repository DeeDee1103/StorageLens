const fs = require('fs');
const path = require('path');

const DEFAULT_BASE_URL = process.env.SCREENSHOT_BASE_URL || 'http://localhost:5001';
const DEFAULT_OUTPUT_DIR = process.argv[2] || path.resolve('docs', 'screenshots');

const routes = [
  { route: '/', file: 'home-index.png' },
  { route: '/GlobalView', file: 'global-view.png' },
  { route: '/StorageLocations', file: 'storage-locations.png' },
  { route: '/ScanJobs', file: 'scan-jobs.png' },
  { route: '/FileInventory', file: 'file-inventory.png' },
  { route: '/Duplicates', file: 'duplicates.png' },
  { route: '/Reports', file: 'reports.png' },
  { route: '/Insights', file: 'insights.png' },
  { route: '/FlowManager', file: 'flow-manager.png' },
  { route: '/Settings', file: 'settings.png' },
  { route: '/AppearanceSettings', file: 'appearance-settings.png' },
  { route: '/EnvironmentSettings', file: 'environment-settings.png' },
  { route: '/ClientManagement', file: 'client-management.png' },
  { route: '/Configuration', file: 'configuration.png' },
  { route: '/ScannerSettings', file: 'scanner-settings.png' },
  { route: '/NotificationSettings', file: 'notification-settings.png' },
  { route: '/Alerts', file: 'alerts.png' },
  { route: '/Logging', file: 'logging.png' },
  { route: '/PricingAdmin', file: 'pricing-admin.png' },
  { route: '/Landing', file: 'landing.png' },
  { route: '/About', file: 'about.png' },
  { route: '/Pricing', file: 'pricing.png' },
  { route: '/Profile', file: 'profile.png' },
  { route: '/Privacy', file: 'privacy.png' },
  { route: '/Error', file: 'error.png' }
];

async function captureAllPages() {
  const { chromium } = require('playwright');

  fs.mkdirSync(DEFAULT_OUTPUT_DIR, { recursive: true });

  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({
    viewport: { width: 1440, height: 1100 },
    deviceScaleFactor: 1
  });

  const manifest = {
    baseUrl: DEFAULT_BASE_URL,
    generatedAtUtc: new Date().toISOString(),
    outputDir: DEFAULT_OUTPUT_DIR,
    items: []
  };

  try {
    for (const item of routes) {
      const url = new URL(item.route, DEFAULT_BASE_URL).toString();
      const outputPath = path.join(DEFAULT_OUTPUT_DIR, item.file);

      try {
        const response = await page.goto(url, { waitUntil: 'networkidle', timeout: 90000 });
        await page.waitForTimeout(2500);
        await page.screenshot({ path: outputPath, fullPage: true });

        manifest.items.push({
          route: item.route,
          url,
          file: item.file,
          outputPath,
          status: response ? response.status() : null,
          ok: true,
          title: await page.title()
        });
      } catch (error) {
        manifest.items.push({
          route: item.route,
          url,
          file: item.file,
          outputPath,
          ok: false,
          error: error && error.message ? error.message : String(error)
        });
      }
    }
  } finally {
    await browser.close();
  }

  const manifestPath = path.join(DEFAULT_OUTPUT_DIR, 'capture-manifest.json');
  fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2));

  const failures = manifest.items.filter((x) => !x.ok);
  if (failures.length > 0) {
    console.error(`Capture completed with ${failures.length} failed route(s). See ${manifestPath}.`);
    process.exitCode = 2;
  } else {
    console.log(`Capture completed successfully for ${manifest.items.length} routes.`);
    console.log(`Manifest: ${manifestPath}`);
  }
}

captureAllPages().catch((error) => {
  const message = error && error.stack ? error.stack : String(error);
  fs.writeFileSync(path.resolve('runtime-capture-all-pages-error.log'), message);
  process.exit(1);
});
