using DietPlanner.Api.Common.Exceptions;
using DietPlanner.Api.Common.Models;
using DietPlanner.Api.Data;
using DietPlanner.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DietPlanner.Api.Features.DietPlans;

public interface IDietPlanService
{
    Task<DietPlanDetailDto> CreateAsync(string userId, CreateDietPlanRequest request);
    Task<PagedResult<DietPlanSummaryDto>> GetUserPlansAsync(string userId, int page, int pageSize);
    Task<DietPlanDetailDto> GetByIdAsync(Guid id, string userId);
    Task DeleteAsync(Guid id, string userId, bool permanent = false);
    Task<List<MealEntryDto>> GetMealsAsync(Guid id, string userId, DateOnly? from, DateOnly? to);
}

public class DietPlanService : IDietPlanService
{
    private readonly AppDbContext _db;
    private readonly ILogger<DietPlanService> _logger;
    private readonly IWebHostEnvironment _env;

    public DietPlanService(
        AppDbContext db,
        ILogger<DietPlanService> logger,
        IWebHostEnvironment env)
    {
        _db = db;
        _logger = logger;
        _env = env;
    }

    public async Task<DietPlanDetailDto> CreateAsync(string userId, CreateDietPlanRequest request)
    {
        var plan = new DietPlan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = DateTime.UtcNow,
        };

        _db.DietPlans.Add(plan);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Diet plan created: {PlanId} for user {UserId}", plan.Id, userId);

        return DietPlanDetailDto.FromEntity(plan, 0);
    }

    public async Task<PagedResult<DietPlanSummaryDto>> GetUserPlansAsync(
        string userId,
        int page,
        int pageSize)
    {
        var query = _db.DietPlans
            .Where(dp => dp.UserId == userId)
            .AsNoTracking();

        var totalCount = await query.CountAsync();

        var plans = await query
            .OrderByDescending(dp => dp.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var planIds = plans.Select(p => p.Id).ToList();
        var mealCounts = await _db.MealEntries
            .Where(me => planIds.Contains(me.DietPlanId))
            .GroupBy(me => me.DietPlanId)
            .Select(g => new { DietPlanId = g.Key, Count = g.Count() })
            .ToListAsync();

        var mealCountDict = mealCounts.ToDictionary(x => x.DietPlanId, x => x.Count);

        var items = plans.Select(plan =>
            DietPlanSummaryDto.FromEntity(plan, mealCountDict.GetValueOrDefault(plan.Id, 0)))
            .ToList();

        return new PagedResult<DietPlanSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DietPlanDetailDto> GetByIdAsync(Guid id, string userId)
    {
        var plan = await _db.DietPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(dp => dp.Id == id);

        if (plan == null)
        {
            throw new NotFoundException("Diet plan", id);
        }

        if (plan.UserId != userId)
        {
            throw new ForbiddenException("You can only access your own diet plans");
        }

        var mealCount = await _db.MealEntries
            .Where(me => me.DietPlanId == id)
            .CountAsync();

        return DietPlanDetailDto.FromEntity(plan, mealCount);
    }

    public async Task DeleteAsync(Guid id, string userId, bool permanent = false)
    {
        // Use IgnoreQueryFilters to allow permanent deletion of soft-deleted plans
        var plan = await _db.DietPlans
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(dp => dp.Id == id);

        if (plan == null)
        {
            throw new NotFoundException("Diet plan", id);
        }

        if (plan.UserId != userId)
        {
            throw new ForbiddenException("You can only delete your own diet plans");
        }

        if (permanent)
        {
            if (_env.IsProduction())
            {
                throw new ForbiddenException("Permanent deletion is not allowed in production environment");
            }

            var mealEntries = await _db.MealEntries
                .Where(me => me.DietPlanId == id)
                .ToListAsync();

            _db.MealEntries.RemoveRange(mealEntries);
            _db.DietPlans.Remove(plan);
            _logger.LogInformation("Diet plan permanently deleted: {PlanId} by user {UserId}", id, userId);
        }
        else
        {
            plan.DeletedAt = DateTime.UtcNow;
            _logger.LogInformation("Diet plan soft-deleted: {PlanId} by user {UserId}", id, userId);
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<MealEntryDto>> GetMealsAsync(
        Guid id,
        string userId,
        DateOnly? from,
        DateOnly? to)
    {
        var plan = await _db.DietPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(dp => dp.Id == id);

        if (plan == null)
        {
            throw new NotFoundException("Diet plan", id);
        }

        if (plan.UserId != userId)
        {
            throw new ForbiddenException("You can only access your own diet plans");
        }

        var startDate = from ?? plan.StartDate;
        var endDate = to ?? plan.EndDate;

        var meals = await _db.MealEntries
            .Include(me => me.Recipe)
            .Where(me => me.DietPlanId == id &&
                         me.Date >= startDate &&
                         me.Date <= endDate)
            .OrderBy(me => me.Date)
            .ThenBy(me => me.MealType)
            .AsNoTracking()
            .ToListAsync();

        return meals.Select(MealEntryDto.FromEntity).ToList();
    }
}
