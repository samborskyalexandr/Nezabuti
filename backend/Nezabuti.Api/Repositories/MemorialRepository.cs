using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Nezabuti.Api.DTOs;
using Nezabuti.Api.Models;
using Nezabuti.Api.Models.Blocks;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.Repositories;

public interface IMemorialRepository
{
    Task<Memorial> CreateAsync(CreateMemorialRequest request, PlanSnapshot snapshot, CancellationToken ct = default);
    Task<Memorial?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Memorial?> GetByPublicIdAsync(string publicId, CancellationToken ct = default);
    Task<(List<Memorial> Items, long Total)> ListAsync(
        string? search,
        MemorialStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<Memorial?> UpdateAsync(string id, UpdateMemorialRequest request, CancellationToken ct = default);
    Task<Memorial?> UpdatePlanSnapshotAsync(string id, PlanSnapshot snapshot, CancellationToken ct = default);
    Task<Memorial?> AdjustUsedUpdatesAsync(string id, int delta, CancellationToken ct = default);
    Task<Memorial?> UpdatePaymentAsync(string id, PaymentStatus status, DateTime? paidAt, CancellationToken ct = default);
    Task ReplaceAsync(Memorial memorial, CancellationToken ct = default);
    Task<Memorial?> ReorderBlocksAsync(string id, IReadOnlyList<string> blockIds, CancellationToken ct = default);
    Task<Memorial?> SetStatusAsync(string id, MemorialStatus status, CancellationToken ct = default);
    Task<bool> DeleteArchivedAsync(string id, CancellationToken ct = default);
    Task<bool> PublicIdExistsAsync(string publicId, CancellationToken ct = default);
    Task UpdateMainPhotoAsync(string id, PhotoRef? photo, CancellationToken ct = default);
}

public sealed class MemorialRepository : IMemorialRepository
{
    private const int MaxPublicIdAttempts = 20;
    private readonly IMongoContext _db;
    private readonly IPublicIdGenerator _publicIdGenerator;
    private readonly IRichTextSanitizer _sanitizer;

    public MemorialRepository(
        IMongoContext db,
        IPublicIdGenerator publicIdGenerator,
        IRichTextSanitizer sanitizer)
    {
        _db = db;
        _publicIdGenerator = publicIdGenerator;
        _sanitizer = sanitizer;
    }

    public async Task<Memorial> CreateAsync(
        CreateMemorialRequest request,
        PlanSnapshot snapshot,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var memorial = new Memorial
        {
            FullName = request.FullName.Trim(),
            Privacy = request.Privacy,
            Callsign = NormalizeOptional(request.Callsign),
            LifePeriod = NormalizeOptional(request.LifePeriod),
            ShortText = NormalizeOptional(request.ShortText),
            Status = MemorialStatus.Draft,
            Blocks = [],
            PlanSnapshot = snapshot,
            UsedUpdates = 0,
            QrPlateSize = QrPlateSize.Size50,
            QrPriceDeltaSnapshot = 0,
            PaymentStatus = PaymentStatus.Unpaid,
            FinalPrice = snapshot.Price,
            IsFinalPriceOverridden = false,
            PaidAt = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        for (var attempt = 0; attempt < MaxPublicIdAttempts; attempt++)
        {
            memorial.PublicId = _publicIdGenerator.Generate();
            if (await PublicIdExistsAsync(memorial.PublicId, ct))
            {
                continue;
            }

            try
            {
                await _db.Memorials.InsertOneAsync(memorial, cancellationToken: ct);
                return memorial;
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                // Retry on rare race with unique index
            }
        }

        throw new InvalidOperationException("Failed to generate a unique PublicId.");
    }

    public Task<Memorial?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return Task.FromResult<Memorial?>(null);
        }

        return _db.Memorials.Find(m => m.Id == id).FirstOrDefaultAsync(ct)!;
    }

    public Task<Memorial?> GetByPublicIdAsync(string publicId, CancellationToken ct = default)
    {
        return _db.Memorials.Find(m => m.PublicId == publicId).FirstOrDefaultAsync(ct)!;
    }

    public async Task<(List<Memorial> Items, long Total)> ListAsync(
        string? search,
        MemorialStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var filter = Builders<Memorial>.Filter.Empty;
        if (status.HasValue)
        {
            filter &= Builders<Memorial>.Filter.Eq(m => m.Status, status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = Regex.Escape(search.Trim());
            var regex = new BsonRegularExpression(term, "i");
            filter &= Builders<Memorial>.Filter.Or(
                Builders<Memorial>.Filter.Regex(m => m.FullName, regex),
                Builders<Memorial>.Filter.Regex(m => m.PublicId, regex),
                Builders<Memorial>.Filter.Regex(m => m.Callsign!, regex));
        }

        var total = await _db.Memorials.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _db.Memorials.Find(filter)
            .SortByDescending(m => m.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Memorial?> UpdateAsync(string id, UpdateMemorialRequest request, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        if (existing is null)
        {
            return null;
        }

        existing.FullName = request.FullName.Trim();
        existing.Privacy = request.Privacy;
        existing.Callsign = NormalizeOptional(request.Callsign);
        existing.LifePeriod = NormalizeOptional(request.LifePeriod);
        existing.ShortText = NormalizeOptional(request.ShortText);
        existing.Blocks = MapBlocks(request.Blocks);
        if (request.QrPlateSize.HasValue)
        {
            existing.QrPlateSize = request.QrPlateSize.Value;
        }

        if (request.IsFinalPriceOverridden.HasValue)
        {
            existing.IsFinalPriceOverridden = request.IsFinalPriceOverridden.Value;
        }

        if (request.FinalPrice.HasValue)
        {
            if (request.FinalPrice.Value < 0)
            {
                throw new ArgumentException("Фінальна вартість не може бути від'ємною.");
            }

            existing.FinalPrice = request.FinalPrice.Value;
        }

        existing.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.MainPhotoId))
        {
            if (existing.MainPhoto?.PhotoId != request.MainPhotoId)
            {
                // Main photo path is set via upload endpoint; here we only keep id if already present
                if (existing.MainPhoto is not null && existing.MainPhoto.PhotoId == request.MainPhotoId)
                {
                    // unchanged
                }
            }
        }
        else if (request.MainPhotoId == string.Empty)
        {
            existing.MainPhoto = null;
        }

        await _db.Memorials.ReplaceOneAsync(m => m.Id == id, existing, cancellationToken: ct);
        return existing;
    }

    public async Task<Memorial?> UpdatePlanSnapshotAsync(string id, PlanSnapshot snapshot, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        if (existing is null)
        {
            return null;
        }

        existing.PlanSnapshot = snapshot;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.Memorials.ReplaceOneAsync(m => m.Id == id, existing, cancellationToken: ct);
        return existing;
    }

    public async Task<Memorial?> AdjustUsedUpdatesAsync(string id, int delta, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        if (existing is null)
        {
            return null;
        }

        existing.UsedUpdates = Math.Max(0, existing.UsedUpdates + delta);
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.Memorials.ReplaceOneAsync(m => m.Id == id, existing, cancellationToken: ct);
        return existing;
    }

    public async Task<Memorial?> UpdatePaymentAsync(
        string id,
        PaymentStatus status,
        DateTime? paidAt,
        CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        if (existing is null)
        {
            return null;
        }

        existing.PaymentStatus = status;
        existing.PaidAt = paidAt;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.Memorials.ReplaceOneAsync(m => m.Id == id, existing, cancellationToken: ct);
        return existing;
    }

    public async Task ReplaceAsync(Memorial memorial, CancellationToken ct = default)
    {
        memorial.UpdatedAt = DateTime.UtcNow;
        await _db.Memorials.ReplaceOneAsync(m => m.Id == memorial.Id, memorial, cancellationToken: ct);
    }

    public async Task UpdateMainPhotoAsync(string id, PhotoRef? photo, CancellationToken ct = default)
    {
        var update = Builders<Memorial>.Update
            .Set(m => m.MainPhoto, photo)
            .Set(m => m.UpdatedAt, DateTime.UtcNow);
        await _db.Memorials.UpdateOneAsync(m => m.Id == id, update, cancellationToken: ct);
    }

    public async Task<Memorial?> ReorderBlocksAsync(string id, IReadOnlyList<string> blockIds, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        if (existing is null)
        {
            return null;
        }

        var map = existing.Blocks.ToDictionary(b => b.Id, b => b);
        if (blockIds.Count != map.Count || blockIds.Any(bid => !map.ContainsKey(bid)))
        {
            throw new ArgumentException("BlockIds must contain exactly the current block set.");
        }

        for (var i = 0; i < blockIds.Count; i++)
        {
            map[blockIds[i]].Order = i;
        }

        existing.Blocks = blockIds.Select(bid => map[bid]).ToList();
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.Memorials.ReplaceOneAsync(m => m.Id == id, existing, cancellationToken: ct);
        return existing;
    }

    public async Task<Memorial?> SetStatusAsync(string id, MemorialStatus status, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        if (existing is null)
        {
            return null;
        }

        existing.Status = status;
        existing.UpdatedAt = DateTime.UtcNow;

        switch (status)
        {
            case MemorialStatus.Published:
                existing.PublishedAt ??= DateTime.UtcNow;
                existing.ArchivedAt = null;
                break;
            case MemorialStatus.Archived:
                existing.ArchivedAt = DateTime.UtcNow;
                break;
            case MemorialStatus.Draft:
                existing.ArchivedAt = null;
                break;
        }

        await _db.Memorials.ReplaceOneAsync(m => m.Id == id, existing, cancellationToken: ct);
        return existing;
    }

    public async Task<bool> DeleteArchivedAsync(string id, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(id, ct);
        if (existing is null || existing.Status != MemorialStatus.Archived)
        {
            return false;
        }

        var result = await _db.Memorials.DeleteOneAsync(m => m.Id == id && m.Status == MemorialStatus.Archived, ct);
        return result.DeletedCount > 0;
    }

    public async Task<bool> PublicIdExistsAsync(string publicId, CancellationToken ct = default)
    {
        return await _db.Memorials.Find(m => m.PublicId == publicId).AnyAsync(ct);
    }

    private List<MemorialBlock> MapBlocks(IEnumerable<MemorialBlockDto> blocks)
    {
        return blocks
            .OrderBy(b => b.Order)
            .Select((b, index) =>
            {
                if (!BlockType.All.Contains(b.Type))
                {
                    throw new ArgumentException($"Unsupported block type: {b.Type}");
                }

                BsonDocument data;
                if (b.Data.ValueKind is System.Text.Json.JsonValueKind.Undefined
                    or System.Text.Json.JsonValueKind.Null)
                {
                    data = new BsonDocument();
                }
                else
                {
                    data = BsonDocument.Parse(b.Data.GetRawText());
                }

                SanitizeBlockData(b.Type, data);

                return new MemorialBlock
                {
                    Id = string.IsNullOrWhiteSpace(b.Id) ? Guid.NewGuid().ToString("N") : b.Id,
                    Type = b.Type,
                    Order = index,
                    Data = data
                };
            })
            .ToList();
    }

    private void SanitizeBlockData(string type, BsonDocument data)
    {
        if (type == BlockType.Text && data.Contains("html"))
        {
            data["html"] = _sanitizer.Sanitize(data["html"].AsString);
        }

        if (type == BlockType.Quote && data.Contains("text"))
        {
            data["text"] = _sanitizer.SanitizePlain(data["text"].AsString);
        }

        if (type == BlockType.Memories && data.Contains("items") && data["items"].IsBsonArray)
        {
            foreach (var item in data["items"].AsBsonArray)
            {
                if (item is BsonDocument doc && doc.Contains("text"))
                {
                    doc["text"] = _sanitizer.SanitizePlain(doc["text"].AsString);
                }
            }
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
