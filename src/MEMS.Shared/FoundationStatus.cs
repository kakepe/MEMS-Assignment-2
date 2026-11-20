namespace MEMS.Shared;

// Temporary infrastructure contract; contains no configuration or credentials.
public sealed record FoundationStatus(string Application, string Phase, bool DatabaseConnected, DateTimeOffset CheckedAtUtc);
