using MongoDB.Driver;

namespace TaskPlanner.API.Data.Interfaces;

/// <summary>
/// Defines a manager responsible for controlling MongoDB transactions.
/// </summary>
/// <remarks>
/// The transaction manager is responsible for starting, committing, and aborting
/// MongoDB transactions and for exposing the currently active transaction session
/// to repositories so that database operations can participate in the transaction.
/// </remarks>
public interface ITransactionManager
{
    /// <summary>
    /// Begins a new transaction if none is currently active.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token used to cancel the operation.
    /// </param>
    /// <returns>
    /// <c>true</c> if a new transaction was started;  
    /// <c>false</c> if a transaction was already active.
    /// </returns>
    Task<bool> BeginAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Commits the currently active transaction.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token used to cancel the operation.
    /// </param>
    /// <remarks>
    /// If no transaction is active, this method performs no operation.
    /// </remarks>
    Task CommitAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Aborts (rolls back) the currently active transaction.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token used to cancel the operation.
    /// </param>
    /// <remarks>
    /// If no transaction is active, this method performs no operation.
    /// </remarks>
    Task AbortAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the current active MongoDB transaction session, if any.
    /// </summary>
    /// <returns>
    /// The active <see cref="IClientSessionHandle"/> if a transaction is in progress;  
    /// otherwise <c>null</c>.
    /// </returns>
    /// <remarks>
    /// Repository implementations should use this session (when not null)
    /// to execute MongoDB operations so they participate in the transaction.
    /// </remarks>
    IClientSessionHandle? CurrentSession();
}
