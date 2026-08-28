import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import {
  BLOCK_TYPE_LABELS,
  MemorialAdmin,
  MemorialBlock,
  MemorialStatistics,
  PRIVACY_LABELS,
  PhotoRef,
  STATUS_LABELS,
  createEmptyBlockData,
  isBlockEmpty
} from '../../../core/models/memorial.models';
import { SemanticTextEditorComponent } from '../../../shared/components/semantic-text-editor/semantic-text-editor.component';
import { adminUrl } from '../../../core/config/admin-routes';

@Component({
  selector: 'app-admin-memorial-editor-page',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe, SemanticTextEditorComponent],
  templateUrl: './admin-memorial-editor-page.component.html'
})
export class AdminMemorialEditorPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);

  memorial: MemorialAdmin | null = null;
  stats: MemorialStatistics | null = null;
  message = '';
  error = '';
  saving = false;
  restoring = false;
  uploadingMain = false;
  qrBusy = false;
  dirty = false;
  showAddBlock = false;
  expandedBlockId: string | null = null;

  readonly listHref = adminUrl('memorials');
  readonly statusLabels = STATUS_LABELS;
  readonly privacyLabels = PRIVACY_LABELS;
  readonly blockTypeLabels = BLOCK_TYPE_LABELS;
  readonly blockTypes = Object.keys(BLOCK_TYPE_LABELS);

  get previewHref(): string {
    return this.memorial ? adminUrl('preview', this.memorial.id) : this.listHref;
  }

  get isArchived(): boolean {
    return this.memorial?.status === 'Archived';
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      return;
    }
    this.load(id);
  }

  load(id: string): void {
    this.api.getMemorial(id).subscribe({
      next: (m) => {
        this.memorial = m;
        this.dirty = false;
        this.api.getStatistics(id).subscribe({ next: (s) => (this.stats = s), error: () => undefined });
      },
      error: () => (this.error = 'Меморіал не знайдено')
    });
  }

  markDirty(): void {
    if (this.isArchived) {
      return;
    }
    this.dirty = true;
  }

  save(): void {
    if (!this.memorial || this.saving || this.isArchived) {
      return;
    }
    this.saving = true;
    this.message = '';
    this.error = '';

    const blocks = this.memorial.blocks
      .map((b, index) => ({
        id: b.id,
        type: b.type,
        order: index,
        data: b.data
      }))
      .filter((b) => !isBlockEmpty(b.type, b.data));

    this.api
      .updateMemorial(this.memorial.id, {
        fullName: this.memorial.fullName,
        privacy: this.memorial.privacy,
        callsign: this.memorial.callsign,
        lifePeriod: this.memorial.lifePeriod,
        shortText: this.memorial.shortText,
        mainPhotoId: this.memorial.mainPhoto?.photoId ?? null,
        blocks
      })
      .subscribe({
        next: (m) => {
          this.memorial = m;
          this.dirty = false;
          this.saving = false;
          this.message = 'Збережено';
        },
        error: (err) => {
          this.saving = false;
          this.error = err?.error?.message || 'Помилка збереження';
        }
      });
  }

  publish(): void {
    if (!this.memorial || this.memorial.status !== 'Draft') {
      return;
    }

    const runPublish = () => {
      this.api.publish(this.memorial!.id).subscribe({
        next: (m) => {
          this.memorial = m;
          this.message = 'Опубліковано';
          this.error = '';
        },
        error: (err) => (this.error = err?.error?.message || 'Не вдалося опублікувати')
      });
    };

    if (!this.dirty) {
      runPublish();
      return;
    }

    this.saving = true;
    this.api
      .updateMemorial(this.memorial.id, {
        fullName: this.memorial.fullName,
        privacy: this.memorial.privacy,
        callsign: this.memorial.callsign,
        lifePeriod: this.memorial.lifePeriod,
        shortText: this.memorial.shortText,
        mainPhotoId: this.memorial.mainPhoto?.photoId ?? null,
        blocks: this.memorial.blocks
          .map((b, index) => ({ id: b.id, type: b.type, order: index, data: b.data }))
          .filter((b) => !isBlockEmpty(b.type, b.data))
      })
      .subscribe({
        next: (m) => {
          this.memorial = m;
          this.dirty = false;
          this.saving = false;
          runPublish();
        },
        error: (err) => {
          this.saving = false;
          this.error = err?.error?.message || 'Помилка збереження перед публікацією';
        }
      });
  }

  archive(): void {
    if (!this.memorial || this.isArchived) {
      return;
    }
    if (!confirm('Архівувати меморіал? Публічне посилання стане недоступним (404).')) {
      return;
    }
    this.api.archive(this.memorial.id).subscribe({
      next: (m) => {
        this.memorial = m;
        this.dirty = false;
        this.showAddBlock = false;
        this.message = 'Переміщено в архів';
        this.error = '';
      },
      error: (err) => (this.error = err?.error?.message || 'Не вдалося архівувати')
    });
  }

  restore(): void {
    if (!this.memorial || !this.isArchived || this.restoring) {
      return;
    }
    this.restoring = true;
    this.error = '';
    this.api.restore(this.memorial.id).subscribe({
      next: (m) => {
        this.memorial = m;
        this.dirty = false;
        this.restoring = false;
        this.message = 'Відновлено з архіву як чернетку';
      },
      error: (err) => {
        this.restoring = false;
        this.error = err?.error?.message || 'Не вдалося відновити';
      }
    });
  }

  onMainPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file || !this.memorial || this.isArchived) {
      input.value = '';
      return;
    }
    this.uploadingMain = true;
    this.error = '';
    this.api.uploadPhoto(this.memorial.id, file, true).subscribe({
      next: (photo) => {
        this.memorial!.mainPhoto = photo;
        this.uploadingMain = false;
        this.message = 'Головне фото оновлено';
        this.dirty = true;
        input.value = '';
      },
      error: (err) => {
        this.uploadingMain = false;
        this.error = err?.error?.message || 'Помилка завантаження фото';
        input.value = '';
      }
    });
  }

  removeMainPhoto(): void {
    if (!this.memorial?.mainPhoto || this.isArchived) {
      return;
    }
    const photoId = this.memorial.mainPhoto.photoId;
    this.api.deletePhoto(this.memorial.id, photoId).subscribe({
      next: () => {
        this.memorial!.mainPhoto = null;
        this.message = 'Головне фото видалено';
        this.dirty = true;
      },
      error: (err) => (this.error = err?.error?.message || 'Не вдалося видалити фото')
    });
  }

  addBlock(type: string): void {
    if (!this.memorial || this.isArchived) {
      return;
    }
    const block: MemorialBlock = {
      id: crypto.randomUUID().replace(/-/g, ''),
      type,
      order: this.memorial.blocks.length,
      data: createEmptyBlockData(type)
    };
    this.memorial.blocks = [...this.memorial.blocks, block];
    this.expandedBlockId = block.id!;
    this.showAddBlock = false;
    this.dirty = true;
  }

  moveBlock(index: number, delta: number): void {
    if (!this.memorial) {
      return;
    }
    const target = index + delta;
    if (target < 0 || target >= this.memorial.blocks.length) {
      return;
    }
    const blocks = [...this.memorial.blocks];
    const [item] = blocks.splice(index, 1);
    blocks.splice(target, 0, item);
    this.memorial.blocks = blocks.map((b, i) => ({ ...b, order: i }));
    this.dirty = true;
  }

  removeBlock(index: number): void {
    if (!this.memorial) {
      return;
    }
    if (!confirm('Видалити цей блок?')) {
      return;
    }
    this.memorial.blocks = this.memorial.blocks.filter((_, i) => i !== index).map((b, i) => ({ ...b, order: i }));
    this.dirty = true;
  }

  toggleBlock(id: string | undefined): void {
    if (!id) {
      return;
    }
    this.expandedBlockId = this.expandedBlockId === id ? null : id;
  }

  blockEmpty(block: MemorialBlock): boolean {
    return isBlockEmpty(block.type, block.data);
  }

  // --- nested list helpers ---
  asItems(block: MemorialBlock): Record<string, unknown>[] {
    if (!Array.isArray(block.data['items'])) {
      block.data['items'] = [];
    }
    return block.data['items'] as Record<string, unknown>[];
  }

  asEvents(block: MemorialBlock): Record<string, unknown>[] {
    if (!Array.isArray(block.data['events'])) {
      block.data['events'] = [];
    }
    return block.data['events'] as Record<string, unknown>[];
  }

  addEvent(block: MemorialBlock): void {
    this.asEvents(block).push({
      id: crypto.randomUUID().replace(/-/g, ''),
      dateOrPeriod: '',
      title: '',
      description: '',
      photo: null
    });
    this.dirty = true;
  }

  removeEvent(block: MemorialBlock, index: number): void {
    this.asEvents(block).splice(index, 1);
    this.dirty = true;
  }

  moveEvent(block: MemorialBlock, index: number, delta: number): void {
    const events = this.asEvents(block);
    const target = index + delta;
    if (target < 0 || target >= events.length) {
      return;
    }
    const [item] = events.splice(index, 1);
    events.splice(target, 0, item);
    this.dirty = true;
  }

  addAward(block: MemorialBlock): void {
    this.asItems(block).push({
      id: crypto.randomUUID().replace(/-/g, ''),
      name: '',
      yearOrDate: '',
      description: '',
      photo: null
    });
    this.dirty = true;
  }

  addMemory(block: MemorialBlock): void {
    this.asItems(block).push({
      id: crypto.randomUUID().replace(/-/g, ''),
      author: '',
      relationOrDescription: '',
      text: '',
      photo: null
    });
    this.dirty = true;
  }

  removeItem(block: MemorialBlock, index: number): void {
    this.asItems(block).splice(index, 1);
    this.dirty = true;
  }

  moveItem(block: MemorialBlock, index: number, delta: number): void {
    const items = this.asItems(block);
    const target = index + delta;
    if (target < 0 || target >= items.length) {
      return;
    }
    const [item] = items.splice(index, 1);
    items.splice(target, 0, item);
    this.dirty = true;
  }

  uploadBlockPhoto(block: MemorialBlock, event: Event, target: 'image' | 'gallery' | { kind: 'event' | 'award' | 'memory'; index: number }): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file || !this.memorial || this.isArchived) {
      input.value = '';
      return;
    }

    this.api.uploadPhoto(this.memorial.id, file, false).subscribe({
      next: (photo) => {
        if (target === 'image') {
          block.data['photoId'] = photo.photoId;
          block.data['thumbUrl'] = photo.thumbUrl;
          block.data['previewUrl'] = photo.previewUrl;
          block.data['fullUrl'] = photo.fullUrl;
          block.data['photo'] = photo;
        } else if (target === 'gallery') {
          this.asItems(block).push({
            photoId: photo.photoId,
            thumbUrl: photo.thumbUrl,
            previewUrl: photo.previewUrl,
            fullUrl: photo.fullUrl,
            caption: '',
            order: this.asItems(block).length
          });
        } else {
          const collection =
            target.kind === 'event' ? this.asEvents(block) : this.asItems(block);
          collection[target.index]['photo'] = photo;
        }
        this.dirty = true;
        input.value = '';
      },
      error: (err) => {
        this.error = err?.error?.message || 'Помилка завантаження фото';
        input.value = '';
      }
    });
  }

  removeGalleryItem(block: MemorialBlock, index: number): void {
    const item = this.asItems(block)[index];
    const photoId = String(item['photoId'] ?? '');
    this.asItems(block).splice(index, 1);
    this.dirty = true;
    if (photoId && this.memorial) {
      this.api.deletePhoto(this.memorial.id, photoId).subscribe({ error: () => undefined });
    }
  }

  photoPreview(photo: unknown): string | null {
    if (!photo || typeof photo !== 'object') {
      return null;
    }
    const p = photo as PhotoRef;
    return p.previewUrl || p.thumbUrl || null;
  }

  downloadQr(kind: 'png' | 'svg'): void {
    if (!this.memorial || this.qrBusy) {
      return;
    }
    this.qrBusy = true;
    this.error = '';
    const req = kind === 'png' ? this.api.downloadQrPng(this.memorial.id) : this.api.downloadQrSvg(this.memorial.id);
    req.subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${this.memorial!.publicId}.${kind}`;
        a.click();
        URL.revokeObjectURL(url);
        this.qrBusy = false;
      },
      error: () => {
        this.qrBusy = false;
        this.error = `Не вдалося завантажити QR ${kind.toUpperCase()}`;
      }
    });
  }

  textHtml(block: MemorialBlock): string {
    return String(block.data['html'] ?? '');
  }

  setTextHtml(block: MemorialBlock, html: string): void {
    block.data['html'] = html;
    this.dirty = true;
  }
}
