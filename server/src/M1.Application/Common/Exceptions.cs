namespace M1.Application.Common;

/// <summary>Maps to HTTP 404 in the API layer.</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>Maps to HTTP 400 — a business rule was violated.</summary>
public class DomainRuleException(string message) : Exception(message);

/// <summary>Maps to HTTP 401.</summary>
public class UnauthorizedException(string message) : Exception(message);
