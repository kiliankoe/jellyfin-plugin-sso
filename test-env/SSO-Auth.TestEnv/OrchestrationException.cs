namespace Jellyfin.Plugin.SSO_Auth.TestEnv;

/// <summary>Thrown for controlled, expected failures (missing file, bad arg, container missing).
/// <see cref="Program"/> catches these and surfaces them as exit code 1 with a yellow warning.</summary>
public sealed class OrchestrationException(string message) : Exception(message);
