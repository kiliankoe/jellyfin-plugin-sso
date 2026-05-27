namespace Jellyfin.Plugin.SSO_Auth.TestEnv;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            return Usage();
        }

        try
        {
            var config = EnvConfig.Default();
            var orchestrator = new Orchestrator(config);

            switch (args[0])
            {
                case "up":
                    await orchestrator.UpAsync();
                    return 0;
                case "down":
                    var wipe = args.Skip(1).Any(a => a is "-v" or "--volumes");
                    var unknown = args.Skip(1).FirstOrDefault(a => a is not ("-v" or "--volumes"));
                    if (unknown is not null)
                    {
                        throw new OrchestrationException($"Unknown argument: {unknown} (use --volumes to wipe state)");
                    }

                    await orchestrator.DownAsync(wipeVolumes: wipe);
                    return 0;
                case "reload":
                    await orchestrator.ReloadAsync();
                    return 0;
                case "provision":
                    await orchestrator.ProvisionAsync();
                    return 0;
                default:
                    Console.Error.WriteLine($"Unknown subcommand: {args[0]}");
                    return Usage();
            }
        }
        catch (OrchestrationException ex)
        {
            Console.Error.WriteLine($"[!] {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected: {ex}");
            return 2;
        }
    }

    private static int Usage()
    {
        Console.Error.WriteLine(
            "Usage: SSO-Auth.TestEnv <up|down|reload|provision> [options]\n" +
            "  up                    Publish plugin, restore snapshot if needed, start containers, provision.\n" +
            "  down [--volumes|-v]   Stop containers; --volumes also wipes .data/ and .publish/.\n" +
            "  reload                Republish plugin and restart Jellyfin only.\n" +
            "  provision             Re-register the SSO provider against the running stack.");
        return 1;
    }
}
