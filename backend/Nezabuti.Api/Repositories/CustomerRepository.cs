using MongoDB.Bson;
using MongoDB.Driver;
using Nezabuti.Api.Models;

namespace Nezabuti.Api.Repositories;

public interface ICustomerRepository
{
    Task<(List<Customer> Items, long Total)> ListAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task<Customer?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<Customer>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default);
    Task<Customer> CreateAsync(Customer customer, CancellationToken ct = default);
    Task<Customer?> UpdateAsync(Customer customer, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly IMongoCollection<Customer> _customers;

    public CustomerRepository(IMongoContext mongo)
    {
        _customers = mongo.Customers;
    }

    public async Task<(List<Customer> Items, long Total)> ListAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var filter = Builders<Customer>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var regex = new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(term), "i");
            filter = Builders<Customer>.Filter.Or(
                Builders<Customer>.Filter.Regex(c => c.Name, regex),
                Builders<Customer>.Filter.Regex(c => c.Phone, regex),
                Builders<Customer>.Filter.Regex(c => c.Email!, regex),
                Builders<Customer>.Filter.Regex(c => c.TelegramUsername!, regex));
        }

        var total = await _customers.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _customers.Find(filter)
            .SortByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<Customer?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return Task.FromResult<Customer?>(null);
        }

        return _customers.Find(c => c.Id == id).FirstOrDefaultAsync(ct)!;
    }

    public async Task<List<Customer>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default)
    {
        var list = ids
            .Where(id => !string.IsNullOrWhiteSpace(id) && ObjectId.TryParse(id, out _))
            .Distinct()
            .ToList();

        if (list.Count == 0)
        {
            return [];
        }

        return await _customers.Find(c => list.Contains(c.Id)).ToListAsync(ct);
    }

    public async Task<Customer> CreateAsync(Customer customer, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        customer.CreatedAt = now;
        customer.UpdatedAt = now;
        await _customers.InsertOneAsync(customer, cancellationToken: ct);
        return customer;
    }

    public async Task<Customer?> UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(customer.Id, out _))
        {
            return null;
        }

        customer.UpdatedAt = DateTime.UtcNow;
        var result = await _customers.ReplaceOneAsync(c => c.Id == customer.Id, customer, cancellationToken: ct);
        return result.MatchedCount == 0 ? null : customer;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return false;
        }

        var result = await _customers.DeleteOneAsync(c => c.Id == id, ct);
        return result.DeletedCount > 0;
    }
}
