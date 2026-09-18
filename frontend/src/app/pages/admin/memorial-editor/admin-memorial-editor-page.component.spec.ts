import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { convertToParamMap, provideRouter } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { AdminMemorialEditorPageComponent } from './admin-memorial-editor-page.component';

describe('AdminMemorialEditorPageComponent statistics header', () => {
  it('shows view stats in the header and has no bottom Statistics card', () => {
    TestBed.configureTestingModule({
      imports: [AdminMemorialEditorPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'mem-1' }) } }
        }
      ]
    });
    const fixture = TestBed.createComponent(AdminMemorialEditorPageComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    for (const req of http.match(() => true)) {
      if (req.request.url === '/api/admin/plans') {
        req.flush([]);
      } else if (req.request.url === '/api/admin/settings') {
        req.flush({
          phone: '',
          telegram: '',
          viber: '',
          additionalUpdatePrice: 0,
          qrSize50PriceDelta: 0,
          qrSize75PriceDelta: 0,
          qrSize100PriceDelta: 0,
          telegramNotifyEnabled: false,
          telegramBotTokenMasked: '',
          hasTelegramBotToken: false,
          shortTextMaxChars: 1,
          textBlockMaxChars: 1,
          quoteMaxChars: 1,
          timelineDescriptionMaxChars: 1,
          memoryTextMaxChars: 1,
          serviceDescriptionMaxChars: 1,
          awardDescriptionMaxChars: 1,
          photoCaptionMaxChars: 1
        });
      } else if (req.request.url === '/api/admin/memorials/mem-1') {
        req.flush({
          id: 'mem-1',
          publicId: 'PUB12345AB',
          fullName: 'Тест',
          status: 'Published',
          privacy: 'Public',
          isDemo: true,
          blocks: [],
          createdAt: '2026-01-01T00:00:00Z',
          updatedAt: '2026-01-02T00:00:00Z',
          usedUpdates: 0,
          qrPlateSize: 'Size50',
          paymentState: 'Unconfigured',
          paymentStateLabel: 'Не налаштовано',
          viewCount: 128,
          lastViewedAt: '2026-09-17T17:43:00Z'
        });
      } else {
        req.flush([]);
      }
    }

    fixture.detectChanges();
    const later = http.match(() => true);
    for (const req of later) {
      if (req.request.url.includes('/statistics')) {
        req.flush({ publicId: 'PUB12345AB', totalViews: 128, lastViewedAt: '2026-09-17T17:43:00Z', viewsPerDay: [] });
      } else if (req.request.url.includes('/payments')) {
        req.flush([]);
      } else {
        req.flush({});
      }
    }
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Переглядів: 128');
    expect(text).toContain('Останній перегляд:');
    expect(text).not.toMatch(/Останній перегляд:\s*—/);
    const headings = Array.from(fixture.nativeElement.querySelectorAll('h2') as NodeListOf<HTMLElement>).map((h) => h.textContent?.trim());
    expect(headings).not.toContain('Статистика');
  });
});
