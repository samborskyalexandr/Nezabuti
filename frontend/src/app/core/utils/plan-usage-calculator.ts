import {
  GalleryUsage,
  MemorialBlock,
  PlanSnapshot,
  PlanUsage
} from '../models/memorial.models';

/**
 * Live plan usage from the current in-memory editor model.
 * Mirrors backend PlanLimitService.GetUsage for UX only.
 */
export function computePlanUsage(
  blocks: MemorialBlock[],
  snapshot: PlanSnapshot | null | undefined,
  usedUpdates: number
): PlanUsage {
  const unlimited = snapshot?.isUnlimited === true;
  const galleries = blocks.filter((b) => b.type === 'Gallery');

  return {
    blocksUsed: blocks.length,
    maxBlocks: unlimited ? null : (snapshot?.maxBlocks ?? null),
    galleriesUsed: galleries.length,
    maxGalleryBlocks: unlimited ? null : (snapshot?.maxGalleryBlocks ?? null),
    timelineEventsUsed: countTimelineEvents(blocks),
    maxTimelineEvents: unlimited ? null : (snapshot?.maxTimelineEvents ?? null),
    memoriesUsed: countMemories(blocks),
    maxMemories: unlimited ? null : (snapshot?.maxMemories ?? null),
    usedUpdates,
    includedUpdates: snapshot?.includedUpdates ?? 0,
    isUnlimited: unlimited,
    galleries: galleries.map(
      (g): GalleryUsage => ({
        blockId: g.id || '',
        photosUsed: countGalleryPhotos(g),
        maxPhotosPerGallery: unlimited ? null : (snapshot?.maxPhotosPerGallery ?? null)
      })
    )
  };
}

export function countGalleryPhotos(block: MemorialBlock): number {
  const items = Array.isArray(block.data['items']) ? (block.data['items'] as Record<string, unknown>[]) : [];
  return items.filter((item) => {
    const id = item['photoId'];
    return typeof id === 'string' && id.trim().length > 0;
  }).length;
}

function countTimelineEvents(blocks: MemorialBlock[]): number {
  let total = 0;
  for (const block of blocks.filter((b) => b.type === 'Timeline')) {
    const events = block.data['events'];
    if (Array.isArray(events)) {
      total += events.length;
    }
  }
  return total;
}

function countMemories(blocks: MemorialBlock[]): number {
  let total = 0;
  for (const block of blocks.filter((b) => b.type === 'Memories')) {
    const items = block.data['items'];
    if (Array.isArray(items)) {
      total += items.length;
    }
  }
  return total;
}
