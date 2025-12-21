using OneBitSoftware.Utilities;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Data.Transactions;

/// <inheritdoc/>
public class TransactionManagementUtility : ITransactionManagementUtility
{
    private readonly ITransactionManager _manager;

    public TransactionManagementUtility(ITransactionManager manager)
    {
        _manager = manager;
    }

    /// <inheritdoc/>
    public async Task<OperationResult<TResult>> ExecuteInTransactionAsync<TResult>(Func<Task<OperationResult<TResult>>> operation, CancellationToken cancellationToken)
    {
        var result = new OperationResult<TResult>();

        var started = await _manager.BeginAsync(cancellationToken);

        try
        {
            var inner = await operation();
            if (!inner.Success)
            {
                await _manager.AbortAsync(cancellationToken);
                return result.AppendErrors(inner);
            }

            if (started) await _manager.CommitAsync(cancellationToken);

            return result.WithRelatedObject(inner.ResultObject);
        }
        catch (Exception ex)
        {
            await _manager.AbortAsync(cancellationToken);
            return result.AppendException(ex);
        }
    }
}