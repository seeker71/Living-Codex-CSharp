namespace CodexBootstrap.Core;

// Core response DTOs - only truly generic types that belong in core
public record ModuleInfo(string Id, string Name, string Version, string? Description, string Title);

public record ApiResponse<T>(T Data, bool Success = true, string? Error = null);

public record ErrorResponse(string Error);

public record SuccessResponse(string Message);
