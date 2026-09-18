import { Component, input, signal } from '@angular/core';
import { LightboxComponent, LightboxItem } from '../lightbox/lightbox.component';

export interface GalleryItem {
  photoId: string;
  thumbUrl?: string;
  previewUrl: string;
  fullUrl?: string;
  caption?: string;
}

@Component({
  selector: 'app-gallery',
  standalone: true,
  imports: [LightboxComponent],
  template: `
    <div class="columns-1 gap-5 sm:columns-2 lg:columns-3">
      @for (item of items(); track item.photoId || $index; let i = $index) {
        <button
          type="button"
          class="mb-5 block w-full break-inside-avoid overflow-hidden text-left focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-memorial-accent"
          (click)="open(i)"
        >
          <img
            [src]="item.previewUrl || item.thumbUrl"
            [alt]="item.caption || 'Фотогалерея'"
            loading="lazy"
            decoding="async"
            class="w-full object-cover transition duration-500 motion-safe:hover:scale-[1.015]"
          />
          @if (item.caption) {
            <p class="mt-2.5 font-sans text-sm text-memorial-muted">{{ item.caption }}</p>
          }
        </button>
      }
    </div>

    <app-lightbox
      [items]="lightboxItems()"
      [index]="lightboxIndex()"
      (indexChange)="lightboxIndex.set($event)"
      (closed)="close()"
    />
  `
})
export class GalleryComponent {
  readonly items = input<GalleryItem[]>([]);
  readonly lightboxIndex = signal<number | null>(null);

  lightboxItems(): LightboxItem[] {
    return this.items().map((item) => ({
      photoId: item.photoId,
      thumbUrl: item.thumbUrl,
      previewUrl: item.previewUrl,
      fullUrl: item.fullUrl,
      caption: item.caption,
      alt: item.caption
    }));
  }

  open(index: number): void {
    if (!this.items().length) {
      return;
    }
    this.lightboxIndex.set(index);
  }

  close(): void {
    this.lightboxIndex.set(null);
  }
}
