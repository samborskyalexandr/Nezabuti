using Nezabuti.Api.DTOs;
using Nezabuti.Api.Models;
using Nezabuti.Api.Repositories;

namespace Nezabuti.Api.Services;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> ListAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task<CustomerDto?> GetAsync(string id, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<CustomerDto?> UpdateAsync(string id, UpdateCustomerRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repo;

    public CustomerService(ICustomerRepository repo)
    {
        _repo = repo;
    }

    public async Task<PagedResult<CustomerDto>> ListAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var (items, total) = await _repo.ListAsync(search, page, pageSize, ct);
        return new PagedResult<CustomerDto>
        {
            Items = items.Select(Map).ToList(),
            Total = total,
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 1, 100)
        };
    }

    public async Task<CustomerDto?> GetAsync(string id, CancellationToken ct = default)
    {
        var customer = await _repo.GetByIdAsync(id, ct);
        return customer is null ? null : Map(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        var phone = request.Phone.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
        {
            throw new InvalidOperationException("Ім'я та телефон замовника обов'язкові.");
        }

        var customer = new Customer
        {
            Name = name,
            Phone = phone,
            Email = NormalizeOptional(request.Email),
            TelegramUsername = NormalizeOptional(request.TelegramUsername),
            Notes = NormalizeOptional(request.Notes)
        };

        var created = await _repo.CreateAsync(customer, ct);
        return Map(created);
    }

    public async Task<CustomerDto?> UpdateAsync(string id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing is null)
        {
            return null;
        }

        var name = request.Name.Trim();
        var phone = request.Phone.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
        {
            throw new InvalidOperationException("Ім'я та телефон замовника обов'язкові.");
        }

        existing.Name = name;
        existing.Phone = phone;
        existing.Email = NormalizeOptional(request.Email);
        existing.TelegramUsername = NormalizeOptional(request.TelegramUsername);
        existing.Notes = NormalizeOptional(request.Notes);

        var updated = await _repo.UpdateAsync(existing, ct);
        return updated is null ? null : Map(updated);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken ct = default) =>
        _repo.DeleteAsync(id, ct);

    private static CustomerDto Map(Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Phone = c.Phone,
        Email = c.Email,
        TelegramUsername = c.TelegramUsername,
        Notes = c.Notes,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
