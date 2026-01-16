using MongoDB.Bson;
using MongoDB.Driver;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Utilities;

namespace TaskPlanner.API.Core.Services;

/// <inheritdoc/>
public class DailyPlanService : IDailyPlanService
{
    private readonly IBaseRepository<DailyPlan> _dailyPlanRepository;

    public DailyPlanService(IBaseRepository<DailyPlan> dailyPlanRepository)
    {
        _dailyPlanRepository = dailyPlanRepository;
    }

    /// <inheritdoc/>
    public async Task<OperationResult<DailyPlan>> GetActiveDailyPlanAsync(ObjectId userId, DateOnly planDate, CancellationToken cancellationToken)
    {
        var result = new OperationResult<DailyPlan>();

        if (userId == ObjectId.Empty)
            return result.AppendError("Invalid user id.");

        var filter = Builders<DailyPlan>.Filter.And(
            Builders<DailyPlan>.Filter.Eq(x => x.UserId, userId),
            Builders<DailyPlan>.Filter.Eq(x => x.PlanDate, planDate),
            Builders<DailyPlan>.Filter.Eq(x => x.Status, DailyPlanStatus.Active));

        // If multiple active plans somehow exist, return the most recent.
        var sort = Builders<DailyPlan>.Sort.Descending(x => x.GeneratedAt);

        var get = await _dailyPlanRepository.GetAsync(filter, cancellationToken, sort: sort, limit: 1);
        if (!get.Success)
            return result.AppendErrors(get);

        var entity = get.ResultObject?.FirstOrDefault();
        if (entity is null)
        {
            result.AppendError(new NotFoundError("Daily plan not found."));
            return result;
        }

        return result.WithRelatedObject(entity);
    }

    /// <inheritdoc/>
    public async Task<OperationResult<DailyPlan>> CreateDailyPlanAsync(ObjectId userId, DateOnly planDate, BsonDocument inputSnapshot, BsonDocument plan, CancellationToken cancellationToken, Guid? planRunId = null)
    {
        var result = new OperationResult<DailyPlan>();

        if (userId == ObjectId.Empty)
            return result.AppendError("Invalid user id.");

        inputSnapshot ??= new BsonDocument();
        plan ??= new BsonDocument();

        // 1) Supersede any existing active plan for that user/date
        var activeFilter = Builders<DailyPlan>.Filter.And(
            Builders<DailyPlan>.Filter.Eq(x => x.UserId, userId),
            Builders<DailyPlan>.Filter.Eq(x => x.PlanDate, planDate),
            Builders<DailyPlan>.Filter.Eq(x => x.Status, DailyPlanStatus.Active));

        var supersedeUpdate = Builders<DailyPlan>.Update
            .Set(x => x.Status, DailyPlanStatus.Superseded);

        var superseded = await _dailyPlanRepository.ModifyManyAsync(activeFilter, supersedeUpdate, cancellationToken);
        if (!superseded.Success)
        {
            // ModifyManyAsync returns NotFoundError when no entities matched (in your repository).
            // That is fine here; it means there was nothing to supersede.
            if (!superseded.Errors.Any(e => e is NotFoundError))
                return result.AppendErrors(superseded);
        }

        // 2) Create the new plan as active
        var entity = new DailyPlan
        {
            UserId = userId,
            PlanDate = planDate,
            GeneratedAt = DateTime.UtcNow,
            InputSnapshot = inputSnapshot,
            Plan = plan,
            Status = DailyPlanStatus.Active,
            PlanRunId = planRunId ?? Guid.NewGuid()
        };

        var create = await _dailyPlanRepository.CreateAsync(entity);
        if (!create.Success)
            return result.AppendErrors(create);

        return result.WithRelatedObject(create.ResultObject);
    }
}
