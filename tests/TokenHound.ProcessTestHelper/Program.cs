using System.Diagnostics;
using System.Text.Json;
using TokenHound.Infrastructure.Processes;

switch (args[0])
{
    case "wait":
        Console.WriteLine("ready");
        await Task.Delay(Timeout.InfiniteTimeSpan);
        break;
    case "eof":
        Console.WriteLine("ready");

        while (await Console.In.ReadLineAsync() is not null)
        {
        }

        File.WriteAllText(args[1], "graceful");
        break;
    case "stderr":
        await Console.Error.WriteAsync(new string('x', 1024 * 1024));
        Console.WriteLine("ready");
        await Console.In.ReadLineAsync();
        break;
    case "tree":
    case "orphan":
        using (var child = Process.Start(new ProcessStartInfo(Environment.ProcessPath!, "wait")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        })!)
        {
            await child.StandardOutput.ReadLineAsync();
            Console.WriteLine(child.Id);

            if (string.Equals(args[0], "tree", StringComparison.OrdinalIgnoreCase))
                await Task.Delay(Timeout.InfiniteTimeSpan);
        }

        break;
    case "owner":
        await using (var owned = OwnedProcess.Start(Environment.ProcessPath!, "tree"))
        {
            var descendant = await owned.StandardOutput.ReadLineAsync();
            Console.WriteLine($"{owned.Id},{descendant}");
            await Task.Delay(Timeout.InfiniteTimeSpan);
        }

        break;
    case "rpc":
    case "rpc-hang":
        while (await Console.In.ReadLineAsync() is { } line)
        {
            using var message = JsonDocument.Parse(line);

            if (!message.RootElement.TryGetProperty("id", out var id))
                continue;

            if (string.Equals(args[0], "rpc-hang", StringComparison.OrdinalIgnoreCase) && id.GetInt32() != 1)
                continue;

            var result = id.GetInt32() == 1 ? "{}" : "{\"rateLimits\":{\"planType\":\"plus\",\"primary\":{\"usedPercent\":42}}}";
            Console.WriteLine($"{{\"id\":{id},\"result\":{result}}}");
        }

        break;
}
