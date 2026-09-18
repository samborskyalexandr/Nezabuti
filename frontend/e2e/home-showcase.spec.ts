import { expect, test, type Page } from '@playwright/test';

const TINY_PNG = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==',
  'base64'
);

const emptyShowcase = {
  howItWorksEnabled: true,
  howItWorksSlides: [] as Array<Record<string, unknown>>,
  demoEnabled: false,
  demoTitle: '',
  demoDescription: '',
  demoUrl: '',
  demoPreviewImage: null as Record<string, unknown> | null
};

const photo = {
  photoId: 'home1',
  thumbUrl: '/uploads/settings/home/home1-thumb.webp',
  previewUrl: '/uploads/settings/home/home1-preview.webp',
  fullUrl: '/uploads/settings/home/home1-full.webp'
};

function adminSettingsPayload(showcase: typeof emptyShowcase) {
  return {
    phone: '',
    telegram: '',
    viber: '',
    additionalUpdatePrice: 250,
    qrSize50PriceDelta: 0,
    qrSize75PriceDelta: 100,
    qrSize100PriceDelta: 200,
    telegramNotifyEnabled: false,
    telegramBotTokenMasked: '',
    hasTelegramBotToken: false,
    telegramChatId: '',
    shortTextMaxChars: 2000,
    textBlockMaxChars: 20000,
    quoteMaxChars: 1000,
    timelineDescriptionMaxChars: 5000,
    memoryTextMaxChars: 10000,
    serviceDescriptionMaxChars: 10000,
    awardDescriptionMaxChars: 5000,
    photoCaptionMaxChars: 1000,
    homeShowcase: showcase
  };
}

function publicFromShowcase(showcase: typeof emptyShowcase) {
  const slides = showcase.howItWorksEnabled
    ? showcase.howItWorksSlides
        .filter((s) => s['enabled'] !== false && s['title'] && (s['desktopImage'] || s['image']))
        .sort((a, b) => Number(a['sortOrder'] ?? 0) - Number(b['sortOrder'] ?? 0))
        .map((s, index) => {
          const desktop = (s['desktopImage'] || s['image']) as Record<string, string> | undefined;
          const mobile = (s['mobileImage'] as Record<string, string> | undefined) || undefined;
          return {
            title: s['title'],
            description: s['description'] ?? '',
            altText: s['altText'] || s['title'],
            sortOrder: index,
            image: desktop,
            desktopImage: desktop,
            mobileImage: mobile ?? null,
            desktopImageUrl: desktop?.['previewUrl'] || desktop?.['fullUrl'],
            mobileImageUrl: mobile?.['previewUrl'] || desktop?.['previewUrl']
          };
        })
    : [];
  const demo =
    showcase.demoEnabled && showcase.demoUrl
      ? {
          title: showcase.demoTitle || 'Подивіться, як виглядає готова сторінка',
          description: showcase.demoDescription,
          url: showcase.demoUrl,
          buttonLabel: 'Відкрити демо-сторінку',
          previewImage: showcase.demoPreviewImage
        }
      : null;
  return {
    phone: '',
    telegram: '',
    viber: '',
    howItWorksEnabled: slides.length > 0,
    howItWorksSlides: slides,
    demo
  };
}

async function mockHomeAndAdminSettings(page: Page) {
  const showcase = structuredClone(emptyShowcase);

  await page.addInitScript(() => {
    localStorage.setItem('nezabuti_admin_token', 'e2e-test-token');
  });

  await page.route('**/api/**', async (route) => {
    const req = route.request();
    const method = req.method();
    const path = new URL(req.url()).pathname;

    const fulfill = (body: unknown, status = 200) =>
      route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(body) });

    if (path === '/api/public/settings' && method === 'GET') {
      return fulfill(publicFromShowcase(showcase));
    }
    if (path === '/api/admin/settings' && method === 'GET') {
      return fulfill(adminSettingsPayload(showcase));
    }
    if (path === '/api/admin/settings' && method === 'PUT') {
      const body = req.postDataJSON() as { homeShowcase?: typeof emptyShowcase };
      if (body.homeShowcase) {
        Object.assign(showcase, body.homeShowcase);
        showcase.howItWorksSlides = body.homeShowcase.howItWorksSlides ?? [];
      }
      return fulfill(adminSettingsPayload(showcase));
    }
    if (path === '/api/admin/settings/home-images' && method === 'POST') {
      return fulfill(photo);
    }
    if (path === '/api/admin/plans') {
      return fulfill([]);
    }
    return fulfill({ message: `unmocked ${method} ${path}` }, 404);
  });
}

const packageTitles = [
  'QR-код, що веде до пам’яті',
  'Сторінка пам’яті одним скануванням',
  'Життєва історія в хронології',
  'Світлини, що зберігають спогади',
  'Теплі слова, що залишаються поруч',
  'Nezabuti — жива історія пам’яті'
];

function sixPublicSlides() {
  return packageTitles.map((title, i) => ({
    title,
    description: `Опис ${i + 1}`,
    altText: title,
    sortOrder: i,
    desktopImage: {
      photoId: `d${i + 1}`,
      thumbUrl: `/uploads/settings/home/d${i + 1}-thumb.webp`,
      previewUrl: `/uploads/settings/home/d${i + 1}-preview.webp`,
      fullUrl: `/uploads/settings/home/d${i + 1}-full.webp`
    },
    mobileImage: {
      photoId: `m${i + 1}`,
      thumbUrl: `/uploads/settings/home/m${i + 1}-thumb.webp`,
      previewUrl: `/uploads/settings/home/m${i + 1}-preview.webp`,
      fullUrl: `/uploads/settings/home/m${i + 1}-full.webp`
    },
    desktopImageUrl: `/uploads/settings/home/d${i + 1}-preview.webp`,
    mobileImageUrl: `/uploads/settings/home/m${i + 1}-preview.webp`
  }));
}

test.describe('Home showcase', () => {
  test('admin can add a slide and it appears on home', async ({ page }) => {
    await mockHomeAndAdminSettings(page);
    await page.goto('/manage-nz7k4p/settings');
    await page.getByRole('button', { name: 'Головна сторінка' }).click();

    await page.getByRole('button', { name: 'Додати слайд' }).click();
    const slide = page.locator('article').filter({ hasText: 'Слайд 1' });
    await expect(slide).toBeVisible();
    await expect(slide.getByText('Зображення для десктопа')).toBeVisible();
    await expect(slide.getByText('Зображення для мобільних')).toBeVisible();
    await slide.getByLabel('Заголовок').fill('Створюємо сторінку пам’яті');
    await slide.getByLabel('Опис').fill('Адмін додає історію та світлини.');
    await slide.locator('input[type="file"]').first().setInputFiles({
      name: 'slide.png',
      mimeType: 'image/png',
      buffer: TINY_PNG
    });
    await expect(slide.locator('img')).toBeVisible();
    await page.getByRole('button', { name: 'Зберегти' }).click();
    await expect(page.getByText('Збережено')).toBeVisible();

    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'Як це працює' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Створюємо сторінку пам’яті' })).toBeVisible();
  });

  test('demo CTA is shown with the configured URL', async ({ page }) => {
    await mockHomeAndAdminSettings(page);
    await page.goto('/manage-nz7k4p/settings');
    await page.getByRole('button', { name: 'Головна сторінка' }).click();
    await page.getByRole('heading', { name: 'Демо-сторінка' }).waitFor();
    await page.locator('input[name="demoEnabled"]').check();
    await page.locator('input[name="demoTitle"]').fill('Подивіться, як виглядає готова сторінка');
    await page.locator('textarea[name="demoDescription"]').fill('Короткий приклад меморіальної сторінки.');
    await page.locator('input[name="demoUrl"]').fill('/m/DEMO1234AB');
    await page.getByRole('button', { name: 'Зберегти' }).click();
    await expect(page.getByText('Збережено')).toBeVisible();

    await page.goto('/');
    const cta = page.getByRole('link', { name: 'Відкрити демо-сторінку' });
    await expect(cta).toBeVisible();
    await expect(cta).toHaveAttribute('href', '/m/DEMO1234AB');
  });

  test('desktop carousel keeps one dominant active slide, next, lightbox and ESC', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 800 });
    await page.route('**/api/**', async (route) => {
      const path = new URL(route.request().url()).pathname;
      if (path === '/api/public/settings') {
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            phone: '',
            telegram: '',
            viber: '',
            howItWorksEnabled: true,
            howItWorksSlides: sixPublicSlides(),
            demo: {
              title: 'Подивіться, як виглядає готова сторінка',
              description: 'Ознайомтеся з прикладом оформлення меморіальної сторінки.',
              url: 'https://nezabuti.com.ua/m/YSQG27AFFT',
              buttonLabel: 'Відкрити демо-сторінку'
            }
          })
        });
      }
      return route.fulfill({ status: 404, contentType: 'application/json', body: '{}' });
    });

    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'Як це працює' })).toBeVisible();
    const articles = page.locator('app-how-it-works-carousel article');
    await expect(articles).toHaveCount(6);
    await expect(articles.first()).toHaveClass(/opacity-100/);
    await expect(articles.nth(1)).toHaveClass(/opacity-50/);
    await expect(page.getByRole('heading', { name: packageTitles[0] })).toBeVisible();
    await expect(page.getByRole('heading', { name: packageTitles[1] })).toHaveCount(0);

    await page.getByRole('button', { name: 'Наступний слайд' }).click();
    await expect(page.getByRole('button', { name: 'Слайд 2' })).toHaveAttribute('aria-current', 'true');
    await expect(page.getByRole('heading', { name: packageTitles[1] })).toBeVisible();

    await page.locator('app-how-it-works-carousel article.opacity-100 button').click();
    await expect(page.getByRole('dialog', { name: 'Перегляд зображення' })).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(page.getByRole('dialog', { name: 'Перегляд зображення' })).toHaveCount(0);

    await expect(page.getByRole('link', { name: 'Відкрити демо-сторінку' })).toHaveAttribute(
      'href',
      'https://nezabuti.com.ua/m/YSQG27AFFT'
    );
  });

  test('disabled slide stays in admin and is omitted from home', async ({ page }) => {
    await mockHomeAndAdminSettings(page);
    await page.goto('/manage-nz7k4p/settings');
    await page.getByRole('button', { name: 'Головна сторінка' }).click();
    await page.getByRole('button', { name: 'Додати слайд' }).click();
    await page.getByRole('button', { name: 'Додати слайд' }).click();
    const first = page.locator('article').filter({ hasText: 'Слайд 1' });
    const second = page.locator('article').filter({ hasText: 'Слайд 2' });
    await first.getByLabel('Заголовок').fill('Видимий слайд');
    await first.locator('input[type="file"]').first().setInputFiles({ name: 'a.png', mimeType: 'image/png', buffer: TINY_PNG });
    await second.getByLabel('Заголовок').fill('Прихований слайд');
    await second.locator('input[type="file"]').first().setInputFiles({ name: 'b.png', mimeType: 'image/png', buffer: TINY_PNG });
    await second.getByLabel('Увімкнено').uncheck();
    await page.getByRole('button', { name: 'Зберегти' }).click();
    await expect(page.getByText('Збережено')).toBeVisible();
    await expect(first).toBeVisible();
    await expect(second).toBeVisible();

    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'Видимий слайд' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Прихований слайд' })).toHaveCount(0);
  });

  test('mobile uses mobile image, swipe, lightbox tap and no overflow', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.route('**/api/public/settings', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          phone: '',
          telegram: '',
          viber: '',
          howItWorksEnabled: true,
          howItWorksSlides: sixPublicSlides(),
          demo: null
        })
      });
    });
    await page.goto('/');
    const img = page.locator('app-how-it-works-carousel img').first();
    await expect(img).toHaveClass(/object-contain/);
    await expect(img).toHaveClass(/aspect-\[9\/16\]/);
    const track = page.locator('app-how-it-works-carousel [role="region"] > div').first();
    await expect(track).toHaveClass(/touch-pan-y/);
    await expect(track).toHaveClass(/touch-pan-x/);
    const source = page.locator('app-how-it-works-carousel source').first();
    await expect(source).toHaveAttribute('srcset', /m1-preview/);
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 2);
    expect(overflow).toBeFalsy();

    await page.getByRole('button', { name: 'Наступний слайд' }).click();
    await expect(page.getByRole('heading', { name: packageTitles[1] })).toBeVisible();

    await page.locator('app-how-it-works-carousel article.opacity-100 button').click();
    await expect(page.getByRole('dialog', { name: 'Перегляд зображення' })).toBeVisible();
    await page.getByRole('button', { name: 'Закрити' }).click();
    await expect(page.getByRole('dialog', { name: 'Перегляд зображення' })).toHaveCount(0);
  });
});
