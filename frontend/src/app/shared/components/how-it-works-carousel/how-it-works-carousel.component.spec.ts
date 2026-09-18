import { TestBed } from '@angular/core/testing';
import { HowItWorksCarouselComponent } from './how-it-works-carousel.component';
import { PublicHowItWorksSlide } from '../../../core/models/memorial.models';

describe('HowItWorksCarouselComponent', () => {
  const slides: PublicHowItWorksSlide[] = [
    {
      title: 'QR-код, що веде до пам’яті',
      description: 'На пам’ятнику розміщується QR-код.',
      altText: 'QR',
      sortOrder: 0,
      desktopImage: { photoId: 'd1', thumbUrl: '/dt1', previewUrl: '/dp1', fullUrl: '/df1' },
      mobileImage: { photoId: 'm1', thumbUrl: '/mt1', previewUrl: '/mp1', fullUrl: '/mf1' },
      desktopImageUrl: '/dp1',
      mobileImageUrl: '/mp1'
    },
    {
      title: 'Сторінка пам’яті одним скануванням',
      description: 'Відкривається меморіальна сторінка.',
      sortOrder: 1,
      desktopImage: { photoId: 'd2', thumbUrl: '/dt2', previewUrl: '/dp2', fullUrl: '/df2' },
      desktopImageUrl: '/dp2',
      mobileImageUrl: '/dp2'
    },
    {
      title: 'Життєва історія в хронології',
      description: 'Події життя зібрані у зрозумілу послідовність важливих етапів.',
      sortOrder: 2,
      image: { photoId: 'd3', previewUrl: '/dp3', thumbUrl: '/dt3', fullUrl: '/df3' }
    }
  ];

  it('renders enabled slides and shows title/description of the active slide only', () => {
    const fixture = create(slides);
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('QR-код, що веде до пам’яті');
    expect(text).toContain('На пам’ятнику розміщується QR-код.');
    expect(text).not.toContain('Життєва історія в хронології');
    expect(fixture.nativeElement.querySelectorAll('article').length).toBe(3);
  });

  it('uses contain fit, lazy-loads non-first slides, and prefers mobile source', () => {
    const fixture = create(slides);
    const images = Array.from(fixture.nativeElement.querySelectorAll('img')) as HTMLImageElement[];
    expect(images.length).toBe(3);
    expect(images[0].className).toContain('object-contain');
    expect(images[0].className).toContain('aspect-[9/16]');
    expect(images[0].getAttribute('loading')).toBe('eager');
    expect(images[1].getAttribute('loading')).toBe('lazy');
    const source = fixture.nativeElement.querySelector('source') as HTMLSourceElement | null;
    expect(source?.getAttribute('media')).toContain('768px');
    expect(source?.getAttribute('srcset')).toContain('/mp1');
    const track = fixture.nativeElement.querySelector('[role="region"] > div') as HTMLElement;
    expect(track.className).toContain('touch-pan-y');
    expect(track.className).toContain('touch-pan-x');
  });

  it('falls back to desktop image when mobile is missing', () => {
    const fixture = create(slides);
    const sources = fixture.nativeElement.querySelectorAll('article')[1].querySelectorAll('source');
    expect(sources.length).toBe(0);
    const img = fixture.nativeElement.querySelectorAll('article')[1].querySelector('img') as HTMLImageElement;
    expect(img.getAttribute('src')).toContain('/dp2');
  });

  it('marks the first slide as active and neighbor click changes the active slide', () => {
    const fixture = create(slides);
    const cmp = fixture.componentInstance;
    expect(cmp.activeIndex()).toBe(0);
    const articles = fixture.nativeElement.querySelectorAll('article') as NodeListOf<HTMLElement>;
    expect(articles[0].className).toContain('opacity-100');
    expect(articles[1].className).toContain('opacity-50');
    articles[1].click();
    fixture.detectChanges();
    expect(cmp.activeIndex()).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Сторінка пам’яті одним скануванням');
  });

  it('opens and closes the shared lightbox from the active image', () => {
    const fixture = create(slides);
    const cmp = fixture.componentInstance;
    const imgBtn = fixture.nativeElement.querySelector('article button') as HTMLButtonElement;
    imgBtn.click();
    fixture.detectChanges();
    expect(cmp.lightboxIndex()).toBe(0);
    cmp.closeLightbox();
    fixture.detectChanges();
    expect(cmp.lightboxIndex()).toBe(null);
  });

  it('does not render articles when given an empty list', () => {
    const fixture = create([]);
    expect(fixture.nativeElement.querySelectorAll('article').length).toBe(0);
  });
});

function create(slides: PublicHowItWorksSlide[]) {
  TestBed.configureTestingModule({
    imports: [HowItWorksCarouselComponent]
  });
  const fixture = TestBed.createComponent(HowItWorksCarouselComponent);
  fixture.componentRef.setInput('slides', slides);
  fixture.detectChanges();
  return fixture;
}
