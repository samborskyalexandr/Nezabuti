import { expect, test, type Page } from '@playwright/test';
import { baseMemorial, MEMORIAL_ID } from './fixtures/billing-memorial';

function listItem(id: string, fullName: string, viewCount: number) {
  return {
    id,
    publicId: fullName.replace(/\s+/g, '').slice(0, 10).toUpperCase(),
    fullName,
    status: 'Published',
    privacy: 'Public',
    isDemo: fullName.includes('Демо'),
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-02T00:00:00Z',
    paymentState: 'Paid',
    paymentStateLabel: 'Оплачено',
    viewCount
  };
}

const items = [
  listItem('id-low', 'Низькі перегляди', 2),
  listItem('id-high', 'Високі перегляди', 40),
  listItem('id-demo', 'Демо сторінка', 15)
];

async function fulfill(route: { fulfill: (r: { status?: number; contentType?: string; body: string }) => Promise<void> }, body: unknown, status = 200) {
  await route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(body) });
}

async function mockAdminViews(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('nezabuti_admin_token', 'e2e-test-token');
  });

  await page.route('**/api/**', async (route) => {
    const req = route.request();
    const method = req.method();
    const url = new URL(req.url());
    const path = url.pathname;

    if (path === '/api/admin/memorials' && method === 'GET') {
      const sortBy = url.searchParams.get('sortBy');
      const sortDir = url.searchParams.get('sortDir') ?? 'desc';
      const sorted = [...items].sort((a, b) => {
        if (sortBy === 'viewCount') {
          return sortDir === 'asc' ? a.viewCount - b.viewCount : b.viewCount - a.viewCount;
        }
        return a.fullName.localeCompare(b.fullName);
      });
      return fulfill(route, { items: sorted, total: sorted.length, page: 1, pageSize: 20 });
    }
    if (path === '/api/admin/plans') {
      return fulfill(route, []);
    }
    if (path === `/api/admin/memorials/${MEMORIAL_ID}` && method === 'GET') {
      return fulfill(route, baseMemorial({ viewCount: 128, lastViewedAt: '2026-09-17T17:43:00Z' }));
    }
    if (path.includes(`/api/admin/memorials/${MEMORIAL_ID}/statistics`)) {
      return fulfill(route, { publicId: 'E2ETEST001', totalViews: 128, lastViewedAt: '2026-09-17T17:43:00Z', viewsPerDay: [] });
    }
    if (path.includes(`/api/admin/memorials/${MEMORIAL_ID}/payments`)) {
      return fulfill(route, []);
    }
    if (path.includes('/api/admin/settings')) {
      return fulfill(route, {
        phone: '',
        telegram: '',
        viber: '',
        additionalUpdatePrice: 100,
        qrSize50PriceDelta: 0,
        qrSize75PriceDelta: 100,
        qrSize100PriceDelta: 200,
        telegramNotifyEnabled: false,
        telegramBotTokenMasked: '',
        hasTelegramBotToken: false,
        shortTextMaxChars: 1000,
        textBlockMaxChars: 1000,
        quoteMaxChars: 1000,
        timelineDescriptionMaxChars: 1000,
        memoryTextMaxChars: 1000,
        serviceDescriptionMaxChars: 1000,
        awardDescriptionMaxChars: 1000,
        photoCaptionMaxChars: 1000
      });
    }
    return fulfill(route, { message: `unmocked ${method} ${path}` }, 404);
  });
}

test.describe('Memorial views', () => {
  test('list shows ViewCount and sorts by views', async ({ page }) => {
    await mockAdminViews(page);
    await page.goto('/manage-nz7k4p/memorials');
    await expect(page.getByRole('heading', { name: 'Меморіали' })).toBeVisible();
    const viewsSort = page.getByRole('button', { name: /Перегляди|Сортувати за переглядами/ });
    await viewsSort.scrollIntoViewIfNeeded();
    await expect(viewsSort).toBeVisible();
    await expect(page.getByRole('cell', { name: '40' })).toBeVisible();
    await expect(page.getByRole('cell', { name: '15' })).toBeVisible();

    await viewsSort.click();
    const firstName = page.locator('tbody tr').first().locator('td').nth(1);
    await expect(firstName).toContainText('Високі перегляди');
  });

  test('editor shows stats in header and has no bottom statistics card', async ({ page }) => {
    await mockAdminViews(page);
    await page.goto(`/manage-nz7k4p/memorials/${MEMORIAL_ID}`);
    await page.getByRole('heading', { name: 'Редагування меморіалу' }).waitFor();
    await expect(page.getByText(/Переглядів:\s*128/)).toBeVisible();
    await expect(page.getByText(/Останній перегляд:\s*\d{2}\.\d{2}\.2026/)).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Статистика' })).toHaveCount(0);
  });
});
