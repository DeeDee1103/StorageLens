const fs = require('fs');
const path = require('path');

async function main() {
  const { chromium } = require('playwright');

  const url = process.argv[2] || 'http://localhost:5001/Index?dashboardMode=interactive';
  const outputPath = process.argv[3] || 'runtime-dashboard-capture.png';
  const logPath = process.argv[4] || 'runtime-dashboard-capture.log';

  const browser = await chromium.launch({ headless: true });

  try {
    const page = await browser.newPage({
      viewport: { width: 1440, height: 1100 },
      deviceScaleFactor: 1
    });

    await page.goto(url, { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(3500);

    await page.screenshot({
      path: outputPath,
      fullPage: true
    });

    const details = {
      url,
      outputPath,
      title: await page.title(),
      bodyClass: await page.locator('body').evaluate((element) => element.className),
      hasInteractivePanel: await page.locator('[data-dashboard-mode-panel="interactive"]').count(),
      hasModeToggle: await page.locator('[data-dashboard-mode-btn="interactive"]').count(),
      bodyText: await page.locator('body').innerText()
    };

    fs.writeFileSync(logPath, JSON.stringify(details, null, 2));
  } finally {
    await browser.close();
  }
}

main().catch((error) => {
  const message = error && error.stack ? error.stack : String(error);
  fs.writeFileSync(path.resolve('runtime-dashboard-capture-error.log'), message);
  process.exit(1);
});