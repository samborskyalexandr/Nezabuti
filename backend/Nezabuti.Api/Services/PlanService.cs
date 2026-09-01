using Nezabuti.Api.DTOs;
using Nezabuti.Api.Models;
using Nezabuti.Api.Repositories;

namespace Nezabuti.Api.Services;

public interface IPlanService
{
    Task<List<PlanDto>> ListAdminAsync(CancellationToken ct = default);
    Task<PlanDto?> UpdateAsync(string id, UpdatePlanRequest request, CancellationToken ct = default);
    Task<List<PublicPlanDto>> ListPublicAsync(CancellationToken ct = default);
}

public sealed class PlanService : IPlanService
{
    private readonly IPlanRepository _plans;

    public PlanService(IPlanRepository plans)
    {
        _plans = plans;
    }

    public async Task<List<PlanDto>> ListAdminAsync(CancellationToken ct = default)
    {
        var plans = await _plans.ListAsync(activeOnly: false, ct);
        return plans.Select(Map).ToList();
    }

    public async Task<PlanDto?> UpdateAsync(string id, UpdatePlanRequest request, CancellationToken ct = default)
    {
        var plan = await _plans.GetByIdAsync(id, ct);
        if (plan is null)
        {
            return null;
        }

        plan.Name = request.Name.Trim();
        plan.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        plan.Price = request.Price;
        plan.IsActive = request.IsActive;
        plan.IncludedUpdates = Math.Max(0, request.IncludedUpdates);

        if (plan.IsCustom)
        {
            plan.IsUnlimited = request.IsUnlimited;
            if (plan.IsUnlimited)
            {
                plan.MaxBlocks = null;
                plan.MaxGalleryBlocks = null;
                plan.MaxPhotosPerGallery = null;
                plan.MaxTimelineEvents = null;
                plan.MaxMemories = null;
            }
            else
            {
                plan.MaxBlocks = request.MaxBlocks;
                plan.MaxGalleryBlocks = request.MaxGalleryBlocks;
                plan.MaxPhotosPerGallery = request.MaxPhotosPerGallery;
                plan.MaxTimelineEvents = request.MaxTimelineEvents;
                plan.MaxMemories = request.MaxMemories;
            }
        }
        else
        {
            plan.IsUnlimited = false;
            plan.MaxBlocks = request.MaxBlocks;
            plan.MaxGalleryBlocks = request.MaxGalleryBlocks;
            plan.MaxPhotosPerGallery = request.MaxPhotosPerGallery;
            plan.MaxTimelineEvents = request.MaxTimelineEvents;
            plan.MaxMemories = request.MaxMemories;
        }

        var updated = await _plans.UpdateAsync(plan, ct);
        return updated is null ? null : Map(updated);
    }

    public async Task<List<PublicPlanDto>> ListPublicAsync(CancellationToken ct = default)
    {
        var plans = await _plans.ListAsync(activeOnly: true, ct);
        return plans
            .Where(p => !p.IsCustom && PlanCodes.Standard.Contains(p.Code) && p.Code != PlanCodes.Custom)
            .OrderBy(p => p.Price)
            .Select(p => new PublicPlanDto
            {
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                MaxGalleryBlocks = p.MaxGalleryBlocks,
                MaxPhotosPerGallery = p.MaxPhotosPerGallery,
                MaxTimelineEvents = p.MaxTimelineEvents,
                MaxMemories = p.MaxMemories,
                IncludedUpdates = p.IncludedUpdates,
                IsRecommended = p.Code == PlanCodes.Story
            })
            .ToList();
    }

    private static PlanDto Map(Plan p) => new()
    {
        Id = p.Id,
        Code = p.Code,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        IsActive = p.IsActive,
        IsCustom = p.IsCustom,
        IsUnlimited = p.IsUnlimited,
        MaxBlocks = p.MaxBlocks,
        MaxGalleryBlocks = p.MaxGalleryBlocks,
        MaxPhotosPerGallery = p.MaxPhotosPerGallery,
        MaxTimelineEvents = p.MaxTimelineEvents,
        MaxMemories = p.MaxMemories,
        IncludedUpdates = p.IncludedUpdates
    };
}
