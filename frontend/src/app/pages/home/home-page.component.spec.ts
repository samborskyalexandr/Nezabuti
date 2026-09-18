import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { HomePageComponent } from './home-page.component';

describe('HomePageComponent', () => {
  let fixture: ComponentFixture<HomePageComponent>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HomePageComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
    fixture = TestBed.createComponent(HomePageComponent);
    http = TestBed.inject(HttpTestingController);
  });

  it('hides carousel and demo CTA when disabled or empty', () => {
    fixture.detectChanges();
    http.expectOne('/api/public/settings').flush({
      phone: '',
      telegram: '',
      viber: '',
      howItWorksEnabled: false,
      howItWorksSlides: [{ title: 'Hidden', description: '', image: null }],
      demo: null
    });
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(text).not.toContain('Hidden');
    expect(text).not.toContain('Відкрити демо-сторінку');
  });

  it('renders enabled slides in order and demo CTA href', () => {
    fixture.detectChanges();
    http.expectOne('/api/public/settings').flush({
      phone: '',
      telegram: '',
      viber: '',
      howItWorksEnabled: true,
      howItWorksSlides: [
        { title: 'QR-код, що веде до пам’яті', description: 'На пам’ятнику розміщується QR-код.', altText: 'QR', sortOrder: 0, desktopImage: { photoId: '1', previewUrl: '/p1', thumbUrl: '/t1', fullUrl: '/f1' }, desktopImageUrl: '/p1', mobileImageUrl: '/p1' },
        { title: 'Сторінка пам’яті одним скануванням', description: 'Б', altText: 'к2', sortOrder: 1, desktopImage: { photoId: '2', previewUrl: '/p2', thumbUrl: '/t2', fullUrl: '/f2' }, desktopImageUrl: '/p2' },
        { title: 'Життєва історія в хронології', description: 'В', sortOrder: 2, desktopImage: { photoId: '3', previewUrl: '/p3', thumbUrl: '/t3', fullUrl: '/f3' } },
        { title: 'Світлини, що зберігають спогади', description: 'Г', sortOrder: 3, desktopImage: { photoId: '4', previewUrl: '/p4', thumbUrl: '/t4', fullUrl: '/f4' } },
        { title: 'Теплі слова, що залишаються поруч', description: 'Ґ', sortOrder: 4, desktopImage: { photoId: '5', previewUrl: '/p5', thumbUrl: '/t5', fullUrl: '/f5' } },
        { title: 'Nezabuti — жива історія пам’яті', description: 'Д', sortOrder: 5, desktopImage: { photoId: '6', previewUrl: '/p6', thumbUrl: '/t6', fullUrl: '/f6' } }
      ],
      demo: {
        title: 'Подивіться, як виглядає готова сторінка',
        description: 'Ознайомтеся з прикладом оформлення меморіальної сторінки.',
        url: 'https://nezabuti.com.ua/m/YSQG27AFFT',
        buttonLabel: 'Відкрити демо-сторінку'
      }
    });
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('QR-код, що веде до пам’яті');
    expect(text).toContain('На пам’ятнику розміщується QR-код.');
    expect(fixture.nativeElement.querySelectorAll('app-how-it-works-carousel article').length).toBe(6);
    const link = fixture.nativeElement.querySelector('a[href="https://nezabuti.com.ua/m/YSQG27AFFT"]') as HTMLAnchorElement | null;
    expect(link).toBeTruthy();
    expect(link?.textContent).toContain('Відкрити демо-сторінку');
  });
});
