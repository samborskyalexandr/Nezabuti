import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AdminSettingsPageComponent } from './admin-settings-page.component';

describe('AdminSettingsPageComponent home showcase', () => {
  const baseSettings = {
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
    homeShowcase: {
      howItWorksEnabled: true,
      howItWorksSlides: [],
      demoEnabled: false,
      demoTitle: '',
      demoDescription: '',
      demoUrl: '',
      demoPreviewImage: null
    }
  };

  it('adds, edits, deletes a slide and saves demo settings', () => {
    TestBed.configureTestingModule({
      imports: [AdminSettingsPageComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    const fixture = TestBed.createComponent(AdminSettingsPageComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/admin/settings').flush(baseSettings);
    fixture.detectChanges();

    const cmp = fixture.componentInstance;
    cmp.addSlide();
    fixture.detectChanges();
    expect(cmp.showcase.howItWorksSlides.length).toBe(1);

    cmp.showcase.howItWorksSlides[0].title = 'Створюємо сторінку пам’яті';
    cmp.showcase.howItWorksSlides[0].description = 'Опис';
    cmp.showcase.howItWorksSlides[0].shortCaption = 'Коротко';
    cmp.addSlide();
    cmp.showcase.howItWorksSlides[1].title = 'Другий';
    cmp.removeSlide(1);
    expect(cmp.showcase.howItWorksSlides.length).toBe(1);

    cmp.showcase.demoEnabled = true;
    cmp.showcase.demoTitle = 'Подивіться, як виглядає готова сторінка';
    cmp.showcase.demoUrl = '/m/DEMO1234AB';
    cmp.save();

    const put = http.expectOne((r) => r.url === '/api/admin/settings' && r.method === 'PUT');
    const body = put.request.body;
    expect(body.homeShowcase.howItWorksSlides.length).toBe(1);
    expect(body.homeShowcase.howItWorksSlides[0].title).toContain('сторінку пам');
    expect(body.homeShowcase.demoEnabled).toBeTrue();
    expect(body.homeShowcase.demoUrl).toBe('/m/DEMO1234AB');
    put.flush({ ...baseSettings, homeShowcase: body.homeShowcase });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Збережено');
  });

  it('shows one settings section at a time via tabs', () => {
    TestBed.configureTestingModule({
      imports: [AdminSettingsPageComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    const fixture = TestBed.createComponent(AdminSettingsPageComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/admin/settings').flush(baseSettings);
    fixture.detectChanges();

    const text = () => fixture.nativeElement.textContent as string;
    expect(text()).toContain('Як це працює');
    expect(text()).not.toContain('Ціна додаткового оновлення');

    fixture.componentInstance.settingsTab = 'commerce';
    fixture.detectChanges();
    expect(text()).toContain('Ціна додаткового оновлення');
    expect(text()).not.toContain('Як це працює');

    fixture.componentInstance.settingsTab = 'contacts';
    fixture.detectChanges();
    expect(text()).toContain('Телефон');
    expect(text()).not.toContain('Ціна додаткового оновлення');
  });
});
