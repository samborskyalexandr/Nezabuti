import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AdminMemorialListPageComponent } from './admin-memorial-list-page.component';
import { MemorialListItem } from '../../../core/models/memorial.models';

describe('AdminMemorialListPageComponent', () => {
  it('renders ViewCount and sends sortBy=viewCount', () => {
    TestBed.configureTestingModule({
      imports: [AdminMemorialListPageComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    });
    const fixture = TestBed.createComponent(AdminMemorialListPageComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    flushPlans(http);
    const first = http.expectOne((r) => r.url.startsWith('/api/admin/memorials'));
    expect(first.request.params.get('sortBy')).toBe('updatedAt');
    first.flush({ items: [item('A', 3), item('B', 12)], total: 2, page: 1, pageSize: 20 });
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Перегляди');
    expect(text).toContain('3');
    expect(text).toContain('12');

    fixture.nativeElement.querySelector('th button')?.click();
    fixture.detectChanges();

    const sorted = http.expectOne((r) => r.url.startsWith('/api/admin/memorials'));
    expect(sorted.request.params.get('sortBy')).toBe('viewCount');
    expect(sorted.request.params.get('sortDir')).toBe('desc');
    sorted.flush({ items: [item('B', 12), item('A', 3)], total: 2, page: 1, pageSize: 20 });
  });
});

function flushPlans(http: HttpTestingController): void {
  http.expectOne('/api/admin/plans').flush([]);
}

function item(name: string, viewCount: number): MemorialListItem {
  return {
    id: name,
    publicId: name,
    fullName: name,
    status: 'Published',
    privacy: 'Public',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-02T00:00:00Z',
    paymentState: 'Paid',
    paymentStateLabel: 'Оплачено',
    viewCount
  };
}
