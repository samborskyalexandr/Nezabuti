import { Page, Route } from '@playwright/test';
import { MEMORIAL_ID, baseMemorial, renewedMemorial, renewalPayment } from '../fixtures/billing-memorial';

async function json(route: Route, body: unknown, status = 200) {
  await route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(body)
  });
}

export async function mockAdminApis(
  page: Page,
  options: {
    initialStatus?: string;
    afterRenewStatus?: string;
  } = {}
) {
  const initialStatus = options.initialStatus ?? 'Published';
  const afterRenewStatus = options.afterRenewStatus ?? initialStatus;
  let memorial = baseMemorial({ status: initialStatus });
  let payments: unknown[] = [];

  await page.route('**/api/**', async (route) => {
    const req = route.request();
    const method = req.method();
    const url = new URL(req.url());
    const path = url.pathname;

    if (path === `/api/admin/memorials/${MEMORIAL_ID}` && method === 'GET') {
      return json(route, memorial);
    }
    if (path === `/api/admin/memorials/${MEMORIAL_ID}/billing/confirm-renewal` && method === 'POST') {
      memorial = renewedMemorial(afterRenewStatus);
      payments = [renewalPayment];
      return json(route, memorial);
    }
    if (path === `/api/admin/memorials/${MEMORIAL_ID}/payments` && method === 'GET') {
      return json(route, payments);
    }
    if (path.includes(`/api/admin/memorials/${MEMORIAL_ID}/statistics`)) {
      return json(route, { publicId: 'E2ETEST001', totalViews: 0, lastViewedAt: null, viewsPerDay: [] });
    }
    if (path === '/api/admin/plans' || path.endsWith('/api/admin/plans')) {
      return json(route, [
        {
          id: '507f1f77bcf86cd799439099',
          code: 'memory',
          name: "Пам'ять",
          price: 700,
          initialPrice: 700,
          renewalPrice: 300,
          isActive: true,
          isCustom: false
        }
      ]);
    }
    if (path.includes('/api/admin/settings')) {
      return json(route, {
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
        telegramChatId: ''
      });
    }
    if (path.includes('/api/admin/customers')) {
      return json(route, { items: [], total: 0, page: 1, pageSize: 10 });
    }

    return json(route, { message: `unmocked ${method} ${path}` }, 404);
  });
}

export async function openBillingEditor(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem('nezabuti_admin_token', 'e2e-test-token');
  });
  await page.goto(`/manage-nz7k4p/memorials/${MEMORIAL_ID}`);
  await page.getByRole('heading', { name: 'Редагування меморіалу' }).waitFor();
  await page.getByRole('button', { name: 'Оплата та продовження' }).click();
  await page.getByRole('button', { name: 'Підтвердити продовження на 1 рік' }).waitFor();
}
