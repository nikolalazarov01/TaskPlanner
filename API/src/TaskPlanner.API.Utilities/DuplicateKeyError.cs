using OneBitSoftware.Utilities.Errors;

namespace TaskPlanner.API.Utilities;

public class DuplicateKeyError(string message, int? code = null, string details = null) : OperationError(message, code, details);