import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import {
  BLOCK_TYPE_LABELS,
  MemorialAdmin,
  MemorialBlock,
  MemorialStatistics,
  PAYMENT_STATUS_LABELS,
  PRIVACY_LABELS,
  PhotoRef,
  Plan,
  PlanSnapshot,
  QR_PLATE_LABELS,
  QrPlateSize,
  STATUS_LABELS,
  SiteSettings,
  createEmptyBlockData,
  isBlockEmpty
} from '../../../core/models/memorial.models';
import { calculatedPriceFor, qrPriceDeltaFromSettings } from '../../../core/utils/memorial-pricing';
import { computePlanUsage, countGalleryPhotos } from '../../../core/utils/plan-usage-calculator';
import { SemanticTextEditorComponent } from '../../../shared/components/semantic-text-editor/semantic-text-editor.component';
import { adminUrl } from '../../../core/config/admin-routes';

@Component({
  selector: 'app-admin-memorial-editor-page',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe, DecimalPipe, SemanticTextEditorComponent],
  templateUrl: './admin-memorial-editor-page.component.html'
})
export class AdminMemorialEditorPageComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);

  memorial: MemorialAdmin | null = null;
  stats: MemorialStatistics | null = null;
  plans: Plan[] = [];
  settings: SiteSettings | null = null;
  message = '';
  error = '';
  saving = false;
  restoring = false;
  uploadingMain = false;
  qrBusy = false;
  dirty = false;
  showAddBlock = false;
  expandedBlockId: string | null = null;
  assignPlanId = '';
  planBusy = false;
  updatesBusy = false;
  paymentBusy = false;
  /** Local draft of final price while editing; persisted on Save. */
  finalPriceDraft: number | null = null;
  /** QR delta after local size change, before Save. */
  pendingQrDelta: number | null = null;

  /** Current editor blocks — source of truth for live plan usage. */
  readonly blocks = signal<MemorialBlock[]>([]);
  private readonly planSnapshotSig = signal<PlanSnapshot | null>(null);
  private readonly usedUpdatesSig = signal(0);

  /** Derived from current local blocks, not the GET usage snapshot. */
  readonly liveUsage = computed(() => {
    const snap = this.planSnapshotSig();
    if (!snap) {
      return null;
    }
    return computePlanUsage(this.blocks(), snap, this.usedUpdatesSig());
  });

  readonly listHref = adminUrl('memorials');
  readonly statusLabels = STATUS_LABELS;
  readonly privacyLabels = PRIVACY_LABELS;
  readonly paymentLabels = PAYMENT_STATUS_LABELS;
  readonly blockTypeLabels = BLOCK_TYPE_LABELS;
  readonly blockTypes = Object.keys(BLOCK_TYPE_LABELS);
  readonly qrLabels = QR_PLATE_LABELS;
  readonly qrSizes: QrPlateSize[] = ['Size50', 'Size75', 'Size100'];

  get previewHref(): string {
    return this.memorial ? adminUrl('preview', this.memorial.id) : this.listHref;
  }

  /** Public page URL encoded in the QR code. */
  get publicPageUrl(): string {
    const m = this.memorial;
    if (!m) {
      return '';
    }
    if (m.publicUrl) {
      return m.publicUrl;
    }
    if (typeof window !== 'undefined' && window.location?.origin) {
      return `${window.location.origin}/m/${m.publicId}`;
    }
    return `/m/${m.publicId}`;
  }

  get isArchived(): boolean {
    return this.memorial?.status === 'Archived';
  }

  get showExtraUpdatePrice(): boolean {
    const m = this.memorial;
    if (!m?.planSnapshot || !this.settings) {
      return false;
    }
    return m.usedUpdates >= m.planSnapshot.includedUpdates;
  }

  get liveQrDelta(): number {
    const m = this.memorial;
    if (!m) {
      return 0;
    }
    if (this.pendingQrDelta != null) {
      return this.pendingQrDelta;
    }
    if (m.qrPriceDeltaSnapshot != null) {
      return m.qrPriceDeltaSnapshot;
    }
    return qrPriceDeltaFromSettings(this.settings, m.qrPlateSize);
  }

  get liveCalculatedPrice(): number | null {
    return calculatedPriceFor(this.memorial?.planSnapshot, this.liveQrDelta);
  }

  get displayFinalPrice(): number | null {
    if (this.finalPriceDraft != null) {
      return this.finalPriceDraft;
    }
    return this.memorial?.finalPrice ?? this.liveCalculatedPrice;
  }

  private applyMemorial(m: MemorialAdmin): void {
    m.isFinalPriceOverridden ??= false;
    m.paymentStatus ??= 'Unpaid';
    m.qrPriceDeltaSnapshot ??= 0;
    this.memorial = m;
    this.assignPlanId = m.planSnapshot?.planId || '';
    this.pendingQrDelta = null;
    this.syncEditorSignals(m);
    this.finalPriceDraft = m.finalPrice ?? this.liveCalculatedPrice;
  }

  private syncEditorSignals(m: MemorialAdmin): void {
    this.blocks.set(m.blocks.map((b) => this.cloneBlock(b)));
    this.planSnapshotSig.set(m.planSnapshot ?? null);
    this.usedUpdatesSig.set(m.usedUpdates);
  }

  private cloneBlock(block: MemorialBlock): MemorialBlock {
    return {
      ...block,
      data: {
        ...block.data,
        items: Array.isArray(block.data['items'])
          ? (block.data['items'] as unknown[]).map((x) =>
              x && typeof x === 'object' ? { ...(x as object) } : x
            )
          : block.data['items'],
        events: Array.isArray(block.data['events'])
          ? (block.data['events'] as unknown[]).map((x) =>
              x && typeof x === 'object' ? { ...(x as object) } : x
            )
          : block.data['events']
      }
    };
  }

  /** Replace blocks with a new array reference and keep memorial in sync. */
  private setBlocks(next: MemorialBlock[]): void {
    const normalized = next.map((b, i) => ({ ...b, order: i }));
    this.blocks.set(normalized);
    if (this.memorial) {
      this.memorial = { ...this.memorial, blocks: normalized };
    }
  }

  private updateBlockById(blockId: string | undefined, updater: (block: MemorialBlock) => MemorialBlock): void {
    if (!blockId) {
      return;
    }
    this.setBlocks(this.blocks().map((b) => (b.id === blockId ? updater(b) : b)));
  }

  private setBlockCollection(
    block: MemorialBlock,
    key: 'items' | 'events',
    next: Record<string, unknown>[]
  ): void {
    this.updateBlockById(block.id, (b) => ({
      ...b,
      data: { ...b.data, [key]: next }
    }));
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      return;
    }
    this.load(id);
    this.api.listPlans().subscribe({ next: (p) => (this.plans = p.filter((x) => x.isActive)) });
    this.api.getAdminSettings().subscribe({ next: (s) => (this.settings = s), error: () => undefined });
  }

  load(id: string): void {
    this.api.getMemorial(id).subscribe({
      next: (m) => {
        this.applyMemorial(m);
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

  onQrSizeChange(size: QrPlateSize): void {
    if (!this.memorial || this.isArchived) {
      return;
    }
    this.memorial.qrPlateSize = size;
    this.pendingQrDelta = qrPriceDeltaFromSettings(this.settings, size);
    if (!this.memorial.isFinalPriceOverridden) {
      const calc = this.liveCalculatedPrice;
      if (calc != null) {
        this.finalPriceDraft = calc;
      }
    }
    this.markDirty();
  }

  onFinalPriceChange(value: number | string | null): void {
    if (!this.memorial || this.isArchived) {
      return;
    }
    const num = value === '' || value == null ? null : Number(value);
    if (num == null || Number.isNaN(num) || num < 0) {
      return;
    }
    this.finalPriceDraft = num;
    const calc = this.liveCalculatedPrice;
    this.memorial.isFinalPriceOverridden = calc == null || num !== calc;
    this.markDirty();
  }

  private buildUpdateBody() {
    const m = this.memorial!;
    return {
      fullName: m.fullName,
      privacy: m.privacy,
      callsign: m.callsign,
      lifePeriod: m.lifePeriod,
      shortText: m.shortText,
      mainPhotoId: m.mainPhoto?.photoId ?? null,
      qrPlateSize: m.qrPlateSize,
      finalPrice: this.finalPriceDraft ?? m.finalPrice ?? null,
      isFinalPriceOverridden: m.isFinalPriceOverridden,
      blocks: this.blocks()
        .map((b, index) => ({ id: b.id, type: b.type, order: index, data: b.data }))
        .filter((b) => !isBlockEmpty(b.type, b.data))
    };
  }

  save(): void {
    if (!this.memorial || this.saving || this.isArchived) {
      return;
    }
    this.saving = true;
    this.message = '';
    this.error = '';

    this.api.updateMemorial(this.memorial.id, this.buildUpdateBody()).subscribe({
      next: (m) => {
        this.applyMemorial(m);
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

    if (this.memorial.paymentStatus === 'Unpaid') {
      if (!confirm('Меморіал ще не позначений як оплачений. Все одно опублікувати?')) {
        return;
      }
    }

    const runPublish = () => {
      this.api.publish(this.memorial!.id).subscribe({
        next: (m) => {
          this.applyMemorial(m);
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
    this.api.updateMemorial(this.memorial.id, this.buildUpdateBody()).subscribe({
      next: (m) => {
        this.applyMemorial(m);
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
        this.applyMemorial(m);
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
        this.applyMemorial(m);
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

  canAddBlock(type: string): { ok: boolean; error?: string } {
    const m = this.memorial;
    const usage = this.liveUsage();
    const snap = this.planSnapshotSig() ?? m?.planSnapshot;
    if (!m || !snap) {
      return { ok: false, error: 'Спочатку призначте план для цього меморіалу.' };
    }
    if (snap.isUnlimited || usage?.isUnlimited) {
      return { ok: true };
    }
    if (usage?.maxBlocks != null && usage.blocksUsed >= usage.maxBlocks) {
      return { ok: false, error: `У плані «${snap.name}» доступно до ${usage.maxBlocks} блоків.` };
    }
    if (
      type === 'Gallery' &&
      usage?.maxGalleryBlocks != null &&
      usage.galleriesUsed >= usage.maxGalleryBlocks
    ) {
      return { ok: false, error: `У плані «${snap.name}» доступно до ${usage.maxGalleryBlocks} галерей.` };
    }
    return { ok: true };
  }

  addBlock(type: string): void {
    if (!this.memorial || this.isArchived) {
      return;
    }
    const check = this.canAddBlock(type);
    if (!check.ok) {
      this.error = check.error || 'Ліміт плану вичерпано.';
      return;
    }
    const block: MemorialBlock = {
      id: crypto.randomUUID().replace(/-/g, ''),
      type,
      order: this.blocks().length,
      data: createEmptyBlockData(type)
    };
    this.setBlocks([...this.blocks(), block]);
    this.expandedBlockId = block.id!;
    this.showAddBlock = false;
    this.dirty = true;
    this.error = '';
  }

  moveBlock(index: number, delta: number): void {
    const list = this.blocks();
    const target = index + delta;
    if (target < 0 || target >= list.length) {
      return;
    }
    const next = [...list];
    const [item] = next.splice(index, 1);
    next.splice(target, 0, item);
    this.setBlocks(next);
    this.dirty = true;
  }

  removeBlock(index: number): void {
    if (!this.memorial) {
      return;
    }
    if (!confirm('Видалити цей блок?')) {
      return;
    }
    this.setBlocks(this.blocks().filter((_, i) => i !== index));
    this.dirty = true;
    this.error = '';
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

  asItems(block: MemorialBlock): Record<string, unknown>[] {
    return Array.isArray(block.data['items']) ? (block.data['items'] as Record<string, unknown>[]) : [];
  }

  asEvents(block: MemorialBlock): Record<string, unknown>[] {
    return Array.isArray(block.data['events']) ? (block.data['events'] as Record<string, unknown>[]) : [];
  }

  canAddTimelineEvent(): { ok: boolean; error?: string } {
    const usage = this.liveUsage();
    const snap = this.planSnapshotSig() ?? this.memorial?.planSnapshot;
    if (!snap) {
      return { ok: false, error: 'Спочатку призначте план для цього меморіалу.' };
    }
    if (snap.isUnlimited || usage?.isUnlimited) {
      return { ok: true };
    }
    if (usage?.maxTimelineEvents != null && usage.timelineEventsUsed >= usage.maxTimelineEvents) {
      return {
        ok: false,
        error: `У плані «${snap.name}» доступно до ${usage.maxTimelineEvents} подій життєвого шляху.`
      };
    }
    return { ok: true };
  }

  addEvent(block: MemorialBlock): void {
    const check = this.canAddTimelineEvent();
    if (!check.ok) {
      this.error = check.error || 'Ліміт подій вичерпано.';
      return;
    }
    const next = [
      ...this.asEvents(block),
      {
        id: crypto.randomUUID().replace(/-/g, ''),
        dateOrPeriod: '',
        title: '',
        description: '',
        photo: null
      }
    ];
    this.setBlockCollection(block, 'events', next);
    this.dirty = true;
    this.error = '';
  }

  removeEvent(block: MemorialBlock, index: number): void {
    this.setBlockCollection(
      block,
      'events',
      this.asEvents(block).filter((_, i) => i !== index)
    );
    this.dirty = true;
  }

  moveEvent(block: MemorialBlock, index: number, delta: number): void {
    const events = [...this.asEvents(block)];
    const target = index + delta;
    if (target < 0 || target >= events.length) {
      return;
    }
    const [item] = events.splice(index, 1);
    events.splice(target, 0, item);
    this.setBlockCollection(block, 'events', events);
    this.dirty = true;
  }

  addAward(block: MemorialBlock): void {
    const next = [
      ...this.asItems(block),
      {
        id: crypto.randomUUID().replace(/-/g, ''),
        name: '',
        yearOrDate: '',
        description: ''
      }
    ];
    this.setBlockCollection(block, 'items', next);
    this.dirty = true;
  }

  canAddMemory(): { ok: boolean; error?: string } {
    const usage = this.liveUsage();
    const snap = this.planSnapshotSig() ?? this.memorial?.planSnapshot;
    if (!snap) {
      return { ok: false, error: 'Спочатку призначте план для цього меморіалу.' };
    }
    if (snap.isUnlimited || usage?.isUnlimited) {
      return { ok: true };
    }
    if (usage?.maxMemories != null && usage.memoriesUsed >= usage.maxMemories) {
      return { ok: false, error: `У плані «${snap.name}» доступно до ${usage.maxMemories} спогадів.` };
    }
    return { ok: true };
  }

  addMemory(block: MemorialBlock): void {
    const check = this.canAddMemory();
    if (!check.ok) {
      this.error = check.error || 'Ліміт спогадів вичерпано.';
      return;
    }
    const next = [
      ...this.asItems(block),
      {
        id: crypto.randomUUID().replace(/-/g, ''),
        author: '',
        relationOrDescription: '',
        text: '',
        photo: null
      }
    ];
    this.setBlockCollection(block, 'items', next);
    this.dirty = true;
    this.error = '';
  }

  removeItem(block: MemorialBlock, index: number): void {
    this.setBlockCollection(
      block,
      'items',
      this.asItems(block).filter((_, i) => i !== index)
    );
    this.dirty = true;
  }

  moveItem(block: MemorialBlock, index: number, delta: number): void {
    const items = [...this.asItems(block)];
    const target = index + delta;
    if (target < 0 || target >= items.length) {
      return;
    }
    const [item] = items.splice(index, 1);
    items.splice(target, 0, item);
    this.setBlockCollection(block, 'items', items);
    this.dirty = true;
  }

  canAddGalleryPhoto(block: MemorialBlock): { ok: boolean; error?: string } {
    const snap = this.planSnapshotSig() ?? this.memorial?.planSnapshot;
    if (!snap) {
      return { ok: false, error: 'Спочатку призначте план для цього меморіалу.' };
    }
    if (snap.isUnlimited || snap.maxPhotosPerGallery == null) {
      return { ok: true };
    }
    const used = countGalleryPhotos(block);
    if (used >= snap.maxPhotosPerGallery) {
      return { ok: false, error: `У цій галереї можна додати до ${snap.maxPhotosPerGallery} фото.` };
    }
    return { ok: true };
  }

  uploadBlockPhoto(
    block: MemorialBlock,
    event: Event,
    target: 'image' | 'gallery' | { kind: 'event' | 'memory'; index: number }
  ): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file || !this.memorial || this.isArchived) {
      input.value = '';
      return;
    }

    if (target === 'gallery') {
      const check = this.canAddGalleryPhoto(block);
      if (!check.ok) {
        this.error = check.error || 'Ліміт фото вичерпано.';
        input.value = '';
        return;
      }
    }

    const blockId = block.id;
    this.api.uploadPhoto(this.memorial.id, file, false).subscribe({
      next: (photo) => {
        const current = this.blocks().find((b) => b.id === blockId);
        if (!current) {
          input.value = '';
          return;
        }

        if (target === 'image') {
          this.updateBlockById(blockId, (b) => ({
            ...b,
            data: {
              ...b.data,
              photoId: photo.photoId,
              thumbUrl: photo.thumbUrl,
              previewUrl: photo.previewUrl,
              fullUrl: photo.fullUrl,
              photo
            }
          }));
        } else if (target === 'gallery') {
          const items = [
            ...this.asItems(current),
            {
              photoId: photo.photoId,
              thumbUrl: photo.thumbUrl,
              previewUrl: photo.previewUrl,
              fullUrl: photo.fullUrl,
              caption: '',
              order: this.asItems(current).length
            }
          ];
          this.setBlockCollection(current, 'items', items);
        } else {
          const key = target.kind === 'event' ? 'events' : 'items';
          const collection = [...(target.kind === 'event' ? this.asEvents(current) : this.asItems(current))];
          if (!collection[target.index]) {
            input.value = '';
            return;
          }
          collection[target.index] = { ...collection[target.index], photo };
          this.setBlockCollection(current, key, collection);
        }
        this.dirty = true;
        this.error = '';
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
    const photoId = String(item?.['photoId'] ?? '');
    this.setBlockCollection(
      block,
      'items',
      this.asItems(block).filter((_, i) => i !== index)
    );
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
    this.updateBlockById(block.id, (b) => ({
      ...b,
      data: { ...b.data, html }
    }));
    this.dirty = true;
  }

  adjustUpdates(delta: 1 | -1): void {
    if (!this.memorial || this.updatesBusy || this.isArchived) {
      return;
    }
    this.updatesBusy = true;
    this.api.adjustUpdates(this.memorial.id, delta).subscribe({
      next: (m) => {
        this.applyMemorial(m);
        this.updatesBusy = false;
        this.message = 'Лічильник оновлень змінено';
        this.error = '';
      },
      error: (err) => {
        this.updatesBusy = false;
        this.error = err?.error?.message || 'Не вдалося змінити лічильник';
      }
    });
  }

  assignPlan(): void {
    if (!this.memorial || !this.assignPlanId || this.planBusy || this.isArchived) {
      return;
    }
    if (!confirm('Змінити план цього меморіалу? Поточний вміст має вміститися в новий план.')) {
      return;
    }
    this.planBusy = true;
    this.api.assignPlan(this.memorial.id, { planId: this.assignPlanId }).subscribe({
      next: (m) => {
        this.applyMemorial(m);
        this.planBusy = false;
        this.message = 'План оновлено';
        this.error = '';
      },
      error: (err) => {
        this.planBusy = false;
        this.error = err?.error?.message || 'Не вдалося змінити план';
      }
    });
  }

  markPaid(): void {
    if (!this.memorial || this.paymentBusy || this.isArchived) {
      return;
    }
    this.paymentBusy = true;
    this.api.updatePayment(this.memorial.id, 'Paid').subscribe({
      next: (m) => {
        this.applyMemorial(m);
        this.paymentBusy = false;
        this.message = 'Позначено як оплачено';
        this.error = '';
      },
      error: (err) => {
        this.paymentBusy = false;
        this.error = err?.error?.message || 'Не вдалося оновити статус оплати';
      }
    });
  }

  markUnpaid(): void {
    if (!this.memorial || this.paymentBusy || this.isArchived) {
      return;
    }
    if (!confirm('Позначити меморіал як неоплачений?')) {
      return;
    }
    this.paymentBusy = true;
    this.api.updatePayment(this.memorial.id, 'Unpaid').subscribe({
      next: (m) => {
        this.applyMemorial(m);
        this.paymentBusy = false;
        this.message = 'Позначено як неоплачений';
        this.error = '';
      },
      error: (err) => {
        this.paymentBusy = false;
        this.error = err?.error?.message || 'Не вдалося оновити статус оплати';
      }
    });
  }

  usageLabel(used: number, max?: number | null): string {
    if (max == null) {
      return `${used} / ∞`;
    }
    return `${used} / ${max}`;
  }

  moneyLabel(value: number | null | undefined): string {
    if (value == null) {
      return '—';
    }
    return `${value.toLocaleString('uk-UA', { maximumFractionDigits: 0 })} грн`;
  }
}
