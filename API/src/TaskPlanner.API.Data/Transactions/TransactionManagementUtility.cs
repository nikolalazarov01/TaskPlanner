using MongoDB.Driver;
using OneBitSoftware.Utilities;

namespace TaskPlanner.API.Data.Transactions;

public class TransactionManagementUtility
{
    private readonly IMongoClient _mongoClient;
    private IClientSessionHandle _existingSession;

    public TransactionManagementUtility(IMongoClient mongoClient)
    {
        _mongoClient = mongoClient;
    }

    public async Task<OperationResult<TResult>> ExecuteInTransactionAsync<TResult>(Func<Task<OperationResult<TResult>>> operation, CancellationToken cancellationToken)
    {
        var operationResult = new OperationResult<TResult>();
        if (operation is null) return operationResult;
        
        var beginTransaction = await this.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (!beginTransaction.Success) return operationResult.AppendErrors(beginTransaction);
        
        OperationResult<TResult> inner = null;
        try
        {
            inner = await operation().ConfigureAwait(false);
            if (!inner.Success) operationResult.AppendErrors(inner);
        }
        catch (Exception e)
        {
            operationResult.AppendException(e);
        }
        
        var finalizeTransaction = operationResult.Success ? this.CommitTransactionAsync(cancellationToken) : this.AbortTransactionAsync(cancellationToken);
        var finalizationResult = await finalizeTransaction.ConfigureAwait(false);
        if (finalizationResult.Success == false) operationResult.AppendErrors(finalizationResult);
    
        if (inner is not null) operationResult.ResultObject = inner.ResultObject;
        return operationResult;
    }

    private async Task<OperationResult> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var operationResult = new OperationResult();
        var session = await this._mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        if (session is null) return operationResult.AppendError("Session is null");

        try
        {
            session.StartTransaction();
        }
        catch (Exception e)
        {
            operationResult.AppendException(e);
        }

        this._existingSession = session;

        return operationResult;
    }
    
    private async Task<OperationResult> CommitTransactionAsync(CancellationToken cancellationToken)
    {
        var operationResult = new OperationResult();
        
        if (this._existingSession is null) return operationResult.AppendError("Session is null");

        try
        {
            await _existingSession.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception e)
        {
            operationResult.AppendException(e);
        }

        return operationResult;
    }
    
    private async Task<OperationResult> AbortTransactionAsync(CancellationToken cancellationToken)
    {
        var operationResult = new OperationResult();
        
        if (this._existingSession is null) return operationResult.AppendError("Session is null");

        try
        {
            await _existingSession.AbortTransactionAsync(cancellationToken);
        }
        catch (Exception e)
        {
            operationResult.AppendException(e);
        }

        return operationResult;
    }
}