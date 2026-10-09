namespace SaaSPlatform.Api.BuildingBlocks.Application;

public sealed class ValidationException(string message) : Exception(message);
public sealed class ResourceNotFoundException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
