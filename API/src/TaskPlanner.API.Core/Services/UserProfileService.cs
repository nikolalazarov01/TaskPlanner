using MongoDB.Bson;
using MongoDB.Driver;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;

namespace TaskPlanner.API.Core.Services;

/// <inheritdoc/>
public class UserProfileService : IUserProfileService
{
    private readonly IBaseRepository<TaskExecutionLog> _taskExecutionLogRepository;
    private readonly IBaseRepository<UserProfile> _userProfileRepository;
    private readonly IBaseRepository<Data.Models.Task> _taskRepository;
    private readonly IUserProfileRecomputeQueueService _queueService;

    public UserProfileService(IBaseRepository<TaskExecutionLog> taskExecutionLogRepository, IBaseRepository<UserProfile> userProfileRepository, IBaseRepository<Data.Models.Task> taskRepository, IUserProfileRecomputeQueueService queueService)
    {
        _taskExecutionLogRepository = taskExecutionLogRepository;
        _userProfileRepository = userProfileRepository;
        _taskRepository = taskRepository;
        _queueService = queueService;
    }

    /// <inheritdoc/>
    public async Task<OperationResult<UserProfile>> RecomputeUserAsync(ObjectId userId, CancellationToken cancellationToken, int windowDays = 14)
    {
        var result = new OperationResult<UserProfile>();

        if (userId == ObjectId.Empty)
            return result.AppendError("Invalid user id.");

        if (windowDays <= 0)
            return result.AppendError("Invalid window days.");

        var nowUtc = DateTime.UtcNow;
        var windowStartUtc = nowUtc.AddDays(-windowDays);

        // 1) Load logs in window
        var logFilter = Builders<TaskExecutionLog>.Filter.And(
            Builders<TaskExecutionLog>.Filter.Eq(x => x.UserId, userId),
            Builders<TaskExecutionLog>.Filter.Gte(x => x.Timestamp, windowStartUtc),
            Builders<TaskExecutionLog>.Filter.Lte(x => x.Timestamp, nowUtc));

        var logsGet = await _taskExecutionLogRepository.GetAsync(logFilter, cancellationToken);
        if (!logsGet.Success)
            return result.AppendErrors(logsGet);

        var logs = logsGet.ResultObject ?? Array.Empty<TaskExecutionLog>();

        // 2) Load tasks for user (for deadline/estimate-based features)
        var taskFilter = Builders<Data.Models.Task>.Filter.Eq(x => x.UserId, userId);
        var tasksGet = await _taskRepository.GetAsync(taskFilter, cancellationToken);
        if (!tasksGet.Success)
            return result.AppendErrors(tasksGet);

        var tasks = tasksGet.ResultObject ?? Array.Empty<Data.Models.Task>();
        var taskById = tasks.ToDictionary(t => t.Id, t => t);

        // 3) Compute metrics (deterministic)
        var computedProfile = ComputeProfile(windowDays, logs, taskById);

        // 4) Upsert UserProfile by UserId
        // Requires ModifyOneAsync(filter, update, isUpsert:true) added to the repository.
        var profileFilter = Builders<UserProfile>.Filter.Eq(x => x.UserId, userId);

        var update = Builders<UserProfile>.Update
            .SetOnInsert(x => x.UserId, userId)
            .Set(x => x.ComputedAt, nowUtc)
            .Set(x => x.WindowDays, windowDays)
            .Set(x => x.Profile, computedProfile);

        // This call relies on your new repository method: ModifyOneAsync(...)
        var upserted = await _userProfileRepository.ModifyAsync(profileFilter, update, cancellationToken, isUpsert: true);
        if (!upserted.Success)
            return result.AppendErrors(upserted);

        // 5) Dequeue best-effort (do not fail profile write if dequeue fails)
        var dequeue = await _queueService.DequeueAsync(userId, cancellationToken);
        // If dequeue fails, you can log it; not returning failure here is usually fine.

        return result.WithRelatedObject(upserted.ResultObject);
    }

    /// <inheritdoc/>
    public async Task<OperationResult<long>> RecomputeUsersAsync(ObjectId[] userIds, CancellationToken cancellationToken, int windowDays = 14)
    {
        var result = new OperationResult<long>();

        if (userIds is null || userIds.Length == 0)
            return result.AppendError("No users provided.");

        if (userIds.Any(x => x == ObjectId.Empty))
            return result.AppendError("Invalid user id.");

        long successCount = 0;

        foreach (var userId in userIds)
        {
            var recompute = await RecomputeUserAsync(userId, cancellationToken, windowDays);
            if (!recompute.Success)
                return result.AppendErrors(recompute);

            successCount++;
        }

        return result.WithRelatedObject(successCount);
    }

    private static BsonDocument ComputeProfile(int windowDays, IReadOnlyCollection<TaskExecutionLog> logs, IReadOnlyDictionary<ObjectId, Data.Models.Task> taskById)
    {
        // avg_daily_completed_minutes (rolling window)
        var completedSeconds = logs
            .Where(l => l.EventType == TaskExecutionEventType.Completed && l.DurationSeconds.HasValue)
            .Sum(l => l.DurationSeconds!.Value);

        var avgDailyCompletedMinutes = windowDays > 0
            ? (completedSeconds / 60.0) / windowDays
            : 0.0;

        // preferred_work_blocks (morning vs evening completion) - UTC-based placeholder.
        // If you later store user timezone or local_date, use that for proper bucketing.
        var completedWithTime = logs
            .Where(l => l.EventType == TaskExecutionEventType.Completed)
            .Select(l => l.Timestamp)
            .ToList();

        var morningCount = completedWithTime.Count(t => t.Hour >= 6 && t.Hour < 12);
        var eveningCount = completedWithTime.Count(t => t.Hour >= 18 && t.Hour < 24);
        var totalCount = completedWithTime.Count;

        double morningShare = totalCount == 0 ? 0 : (double)morningCount / totalCount;
        double eveningShare = totalCount == 0 ? 0 : (double)eveningCount / totalCount;

        // estimation_bias / deadline_reliability / procrastination_index / context_switch_cost
        // These require either:
        // - session start/stop pairing and durations per task, and
        // - task estimate/deadline snapshots or current task data.
        //
        // Implementing them fully is possible, but depends on your exact event semantics.
        // For now, placeholders are persisted so your plan/AI has a stable shape.
        var estimationBias = ComputeEstimationBias(logs, taskById);
        var deadlineReliability = ComputeDeadlineReliability(logs, taskById);
        var procrastinationIndex = ComputeProcrastinationIndex(logs, taskById);
        var contextSwitchCost = ComputeContextSwitchCost(logs, taskById);

        return new BsonDocument
        {
            { "avg_daily_completed_minutes", avgDailyCompletedMinutes },
            { "estimation_bias", estimationBias },
            { "deadline_reliability", deadlineReliability },
            { "procrastination_index", procrastinationIndex },
            { "preferred_work_blocks", new BsonDocument { { "morning", morningShare }, { "evening", eveningShare } } },
            { "context_switch_cost", contextSwitchCost }
        };
    }

    private static double ComputeEstimationBias(IReadOnlyCollection<TaskExecutionLog> logs, IReadOnlyDictionary<ObjectId, Data.Models.Task> taskById)
    {
        // Basic version: actual_duration / estimated_duration for completed tasks with duration + estimate.
        // Uses current task estimate; if you later snapshot estimates in logs, use that instead.
        var pairs = logs
            .Where(l => l.EventType == TaskExecutionEventType.Completed && l.DurationSeconds.HasValue)
            .Select(l =>
            {
                taskById.TryGetValue(l.TaskId, out var t);
                return new { Log = l, Task = t };
            })
            .Where(x => x.Task?.EstimatedMinutes is not null && x.Task.EstimatedMinutes.Value > 0)
            .Select(x => (ActualMinutes: x.Log.DurationSeconds!.Value / 60.0, EstimatedMinutes: (double)x.Task!.EstimatedMinutes!.Value))
            .ToList();

        if (pairs.Count == 0) return 0;

        var ratios = pairs.Select(p => p.ActualMinutes / p.EstimatedMinutes).ToList();
        return ratios.Average();
    }

    private static double ComputeDeadlineReliability(IReadOnlyCollection<TaskExecutionLog> logs, IReadOnlyDictionary<ObjectId, Data.Models.Task> taskById)
    {
        // Basic version: percent of completed tasks that were completed before their current deadline (if any).
        // If you later log missed_deadline events or snapshot deadlines, incorporate those.
        var completed = logs
            .Where(l => l.EventType == TaskExecutionEventType.Completed)
            .Select(l =>
            {
                taskById.TryGetValue(l.TaskId, out var t);
                return new { Log = l, Task = t };
            })
            .Where(x => x.Task?.Deadline is not null)
            .ToList();

        if (completed.Count == 0) return 0;

        var met = completed.Count(x => x.Log.Timestamp <= x.Task!.Deadline!.Value);
        return (double)met / completed.Count;
    }

    private static double ComputeProcrastinationIndex(IReadOnlyCollection<TaskExecutionLog> logs, IReadOnlyDictionary<ObjectId, Data.Models.Task> taskById)
    {
        // Placeholder: requires "start" events and deadlines.
        // A simple definition: average fraction of time-to-deadline remaining when starting.
        var starts = logs
            .Where(l => l.EventType == TaskExecutionEventType.Start)
            .Select(l =>
            {
                taskById.TryGetValue(l.TaskId, out var t);
                return new { Log = l, Task = t };
            })
            .Where(x => x.Task?.Deadline is not null)
            .ToList();

        if (starts.Count == 0) return 0;

        var values = new List<double>();

        foreach (var s in starts)
        {
            var deadline = s.Task!.Deadline!.Value;
            var start = s.Log.Timestamp;

            // If started after deadline, treat as max procrastination for this sample.
            if (start >= deadline)
            {
                values.Add(1.0);
                continue;
            }

            // Without creation time, we approximate using a fixed 7-day horizon to normalize.
            // Replace this with (deadline - created_at) once you have a baseline.
            var horizon = TimeSpan.FromDays(7);
            var remaining = deadline - start;
            var ratio = 1.0 - Math.Clamp(remaining.TotalSeconds / horizon.TotalSeconds, 0.0, 1.0);
            values.Add(ratio);
        }

        return values.Average();
    }

    private static double ComputeContextSwitchCost(IReadOnlyCollection<TaskExecutionLog> logs, IReadOnlyDictionary<ObjectId, Data.Models.Task> taskById)
    {
        // Placeholder: requires category info per task and a definition of "switching".
        // Return 0 until you decide the exact formula.
        return 0;
    }
}