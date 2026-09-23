using System.Diagnostics;
using TokenHound.Infrastructure.Providers.Claude;

var profileCount = args.Length > 0 ? int.Parse(args[0]) : 3;
var root = Path.Combine(AppContext.BaseDirectory, $"fixture-{profileCount}");
Directory.CreateDirectory(root);
const int WARMUP_COUNT = 100;
const int SAMPLE_COUNT = 1000;
var credential = "{\"claudeAiOauth\":{\"accessToken\":\"sk-ant-oat01-dummy\",\"expiresAt\":4102444800000},\"padding\":\"" + new string('x', 800) + "\"}";

for (var index = 0; index < profileCount; index++)
{
    var name = index == 0 ? ".claude" : $".claude-work{index:00}";
    var directory = Path.Combine(root, name);
    Directory.CreateDirectory(directory);
    File.WriteAllText(Path.Combine(directory, ".credentials.json"), credential);
}

var discovery = new ClaudeProfileDiscovery(root);
var firstStart = Stopwatch.GetTimestamp();
var firstProfiles = discovery.DiscoverProfiles(onlyActive: true);
var firstMs = Stopwatch.GetElapsedTime(firstStart).TotalMilliseconds;
if (firstProfiles.Count != profileCount)
    throw new InvalidOperationException($"Expected {profileCount} profiles, found {firstProfiles.Count}.");

for (var index = 0; index < WARMUP_COUNT; index++)
    discovery.DiscoverProfiles(onlyActive: true);

var samples = new double[SAMPLE_COUNT];
for (var index = 0; index < SAMPLE_COUNT; index++)
{
    var start = Stopwatch.GetTimestamp();
    var profiles = discovery.DiscoverProfiles(onlyActive: true);
    samples[index] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    if (profiles.Count != profileCount)
        throw new InvalidOperationException($"Iteration {index} returned {profiles.Count} profiles.");
}
Array.Sort(samples);
Console.WriteLine($"profiles={profileCount}; bytes-per-credential={new FileInfo(Path.Combine(root, ".claude", ".credentials.json")).Length}; warmups={WARMUP_COUNT}; samples={SAMPLE_COUNT}");
Console.WriteLine($"first={firstMs:F3}ms; median={samples[SAMPLE_COUNT / 2]:F3}ms; p95={samples[(int)(SAMPLE_COUNT * 0.95)]:F3}ms; p99={samples[(int)(SAMPLE_COUNT * 0.99)]:F3}ms; max={samples[^1]:F3}ms");



