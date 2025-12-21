using OneBitSoftware.Utilities;

namespace TaskPlanner.API.Data.Interfaces;

public interface ITransactionManagementUtility
{
    /// <summary>
    /// Use this method to execute the passed <paramref name="operation"/> within a transaction.
    /// </summary>
    /// <param name="operation">An asynchronous operation returning an <see cref="OperationResult"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be cancelled.</param>
    /// <returns>
    /// The <see cref="Task"/> representing the asynchronous state of the operation. It wraps inside an <see cref="OperationResult"/> of the operation's execution.
    /// </returns>
    Task<OperationResult<TResult>> ExecuteInTransactionAsync<TResult>(Func<Task<OperationResult<TResult>>> operation, CancellationToken cancellationToken);
}