using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;

namespace Jellyfin.Plugin.SSO_Auth.TestEnv;

/// <summary>
/// Manages the Jellyfin + Dex container topology. Uses Testcontainers' builder DSL for
/// container creation (with the resource reaper disabled so containers persist beyond the
/// CLI process), and Docker.DotNet directly for process-independent lifecycle operations
/// (stop, remove, restart by name).
/// </summary>
public sealed class ContainerStack(EnvConfig config)
{
    public async Task UpAsync(CancellationToken ct = default)
    {
        // Idempotency: tear down any prior containers with our names so the start is deterministic.
        await StopAndRemoveAllAsync(ct);
        EnsureHostDirsExist();

        Console.Out.WriteLine($"[+] Starting {config.DexContainerName} ...");
        var dex = new ContainerBuilder($"ghcr.io/dexidp/dex:{config.DexVersion}")
            .WithName(config.DexContainerName)
            .WithPortBinding(config.DexHostPort, 5556)
            .WithBindMount(config.DexConfigFile, "/etc/dex/config.yaml", AccessMode.ReadOnly)
            .WithCommand("dex", "serve", "/etc/dex/config.yaml")
            .WithCleanUp(false)
            .WithAutoRemove(false)
            .Build();
        await dex.StartAsync(ct);

        Console.Out.WriteLine($"[+] Starting {config.JellyfinContainerName} ...");
        var jellyfin = new ContainerBuilder($"jellyfin/jellyfin:{config.JellyfinVersion}")
            .WithName(config.JellyfinContainerName)
            .WithPortBinding(config.JellyfinHostPort, 8096)
            .WithBindMount(config.JellyfinConfigDir, "/config")
            .WithBindMount(config.JellyfinCacheDir, "/cache")
            .WithBindMount(config.JellyfinMediaDir, "/media")
            .WithBindMount(config.PublishDir, "/config/plugins/SSO-Auth")
            .WithExtraHost("dex.localtest.me", "host-gateway")
            .WithCleanUp(false)
            .WithAutoRemove(false)
            .Build();
        await jellyfin.StartAsync(ct);
    }

    public Task DownAsync(CancellationToken ct = default) => StopAndRemoveAllAsync(ct);

    public async Task RestartJellyfinAsync(CancellationToken ct = default)
    {
        using var client = CreateDockerClient();
        try
        {
            await client.Containers.RestartContainerAsync(
                config.JellyfinContainerName,
                new ContainerRestartParameters { WaitBeforeKillSeconds = 10 },
                ct);
        }
        catch (DockerContainerNotFoundException)
        {
            throw new OrchestrationException($"Container '{config.JellyfinContainerName}' does not exist; run 'up' first.");
        }
    }

    public async Task WaitForJellyfinAsync(CancellationToken ct = default)
    {
        Console.Out.WriteLine($"[+] Waiting for Jellyfin to respond on {config.JellyfinBaseUrl} ...");
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow.AddSeconds(config.JellyfinReadyTimeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var response = await http.GetAsync($"{config.JellyfinBaseUrl}/System/Info/Public", ct);
                if (response.IsSuccessStatusCode)
                {
                    Console.Out.WriteLine("[+] Jellyfin is up.");
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Not up yet.
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                // GET timeout; retry.
            }

            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }

        throw new OrchestrationException($"Jellyfin did not become ready within {config.JellyfinReadyTimeoutSeconds} seconds.");
    }

    private async Task StopAndRemoveAllAsync(CancellationToken ct)
    {
        using var client = CreateDockerClient();
        await StopAndRemoveByNameAsync(client, config.JellyfinContainerName, ct);
        await StopAndRemoveByNameAsync(client, config.DexContainerName, ct);
    }

    private static async Task StopAndRemoveByNameAsync(IDockerClient client, string name, CancellationToken ct)
    {
        try
        {
            await client.Containers.StopContainerAsync(
                name,
                new ContainerStopParameters { WaitBeforeKillSeconds = 10 },
                ct);
        }
        catch (DockerContainerNotFoundException)
        {
            return;
        }

        try
        {
            await client.Containers.RemoveContainerAsync(
                name,
                new ContainerRemoveParameters(),
                ct);
        }
        catch (DockerContainerNotFoundException)
        {
            // Removed concurrently — fine.
        }
    }

    private void EnsureHostDirsExist()
    {
        Directory.CreateDirectory(config.JellyfinConfigDir);
        Directory.CreateDirectory(config.JellyfinCacheDir);
        Directory.CreateDirectory(config.JellyfinMediaDir);
        Directory.CreateDirectory(config.PublishDir);
    }

    private static IDockerClient CreateDockerClient() =>
        new DockerClientBuilder().Build();
}
