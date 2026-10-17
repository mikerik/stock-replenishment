using System;

namespace StockReplenishment.Domain;

/// <summary>The operation is not allowed in the request's current state (HTTP 409).</summary>
public sealed class InvalidStateException(string message) : Exception(message);

/// <summary>The input breaks a business rule (HTTP 400).</summary>
public sealed class BusinessRuleException(string message) : Exception(message);

public sealed class NotFoundException(string message) : Exception(message);