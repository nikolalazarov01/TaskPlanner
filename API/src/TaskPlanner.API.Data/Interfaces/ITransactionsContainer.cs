namespace TaskPlanner.API.Data.Interfaces;

/// <summary>
/// Defines a minimal container for storing and retrieving active transaction sessions.
/// </summary>
/// <typeparam name="TSession">
/// The type of the transaction session (e.g. <see cref="MongoDB.Driver.IClientSessionHandle"/>).
/// </typeparam>
public interface ITransactionsContainer<TSession> where TSession : class
{
    /// <summary>
    /// Retrieves the active transaction session associated with the given key.
    /// </summary>
    /// <param name="key">
    /// A unique identifier for the transaction context (e.g. database name, tenant id).
    /// </param>
    /// <returns>
    /// The active session if one exists; otherwise <c>null</c>.
    /// </returns>
    TSession? Get(string key);

    /// <summary>
    /// Stores or replaces the active transaction session for the given key.
    /// </summary>
    /// <param name="key">
    /// A unique identifier for the transaction context.
    /// </param>
    /// <param name="session">
    /// The transaction session to store.
    /// </param>
    /// <returns>
    /// <c>true</c> if the session was stored successfully; otherwise <c>false</c>.
    /// </returns>
    bool Set(string key, TSession session);

    /// <summary>
    /// Removes the active transaction session associated with the given key.
    /// </summary>
    /// <param name="key">
    /// A unique identifier for the transaction context.
    /// </param>
    /// <returns>
    /// <c>true</c> if a session was removed; otherwise <c>false</c>.
    /// </returns>
    bool Remove(string key);

    /// <summary>
    /// Removes and returns the active transaction session for the given key.
    /// </summary>
    /// <param name="key">
    /// A unique identifier for the transaction context.
    /// </param>
    /// <returns>
    /// The removed session if one existed; otherwise <c>null</c>.
    /// </returns>
    TSession? Take(string key);
}