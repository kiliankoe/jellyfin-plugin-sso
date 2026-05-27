using System.Diagnostics;

namespace Jellyfin.Plugin.SSO_Auth.TestEnv;

public sealed class PluginPublisher(EnvConfig config)
{
    public async Task PublishAsync(CancellationToken ct = default)
    {
        Console.Out.WriteLine($"[+] Publishing plugin to {config.PublishDir} ...");

        var csproj = Path.Combine(config.RepoRoot, "SSO-Auth", "SSO-Auth.csproj");
        var psi = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("publish");
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("Release");
        psi.ArgumentList.Add(csproj);
        psi.ArgumentList.Add("-o");
        psi.ArgumentList.Add(config.PublishDir);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet.");
        await process.WaitForExitAsync(ct);
        if (process.ExitCode != 0)
        {
            throw new OrchestrationException($"dotnet publish exited with code {process.ExitCode}.");
        }
    }
}
