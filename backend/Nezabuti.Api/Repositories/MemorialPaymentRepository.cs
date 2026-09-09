using MongoDB.Bson;
using MongoDB.Driver;
using Nezabuti.Api.Models;

namespace Nezabuti.Api.Repositories;

public interface IMemorialPaymentRepository
{
    Task<MemorialPayment> CreateAsync(MemorialPayment payment, CancellationToken ct = default);
    Task<List<MemorialPayment>> ListByMemorialIdAsync(string memorialId, CancellationToken ct = default);
}

public sealed class MemorialPaymentRepository : IMemorialPaymentRepository
{
    private readonly IMongoCollection<MemorialPayment> _payments;

    public MemorialPaymentRepository(IMongoContext mongo)
    {
        _payments = mongo.MemorialPayments;
    }

    public async Task<MemorialPayment> CreateAsync(MemorialPayment payment, CancellationToken ct = default)
    {
        payment.CreatedAt = DateTime.UtcNow;
        await _payments.InsertOneAsync(payment, cancellationToken: ct);
        return payment;
    }

    public Task<List<MemorialPayment>> ListByMemorialIdAsync(string memorialId, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(memorialId, out _))
        {
            return Task.FromResult(new List<MemorialPayment>());
        }

        return _payments.Find(p => p.MemorialId == memorialId)
            .SortByDescending(p => p.PaidAt)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }
}
