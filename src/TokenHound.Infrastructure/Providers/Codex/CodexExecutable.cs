namespace TokenHound.Infrastructure.Providers.Codex;

internal static class CodexExecutable
{
    private const string EXECUTABLE_NAME = "codex.exe";

    internal static string? Find(
        string? localAppDataDirectory,
        string? programFilesDirectory,
        string? userProfileDirectory,
        string? pathEnvironment)
    {

        var local = localAppDataDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programs = programFilesDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var profile = userProfileDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new[]
        {
            Path.Combine(local, Path.Combine("Programs", "ChatGPT", "resources"), EXECUTABLE_NAME),
            Path.Combine(programs, Path.Combine("ChatGPT", "resources"), EXECUTABLE_NAME),
            Path.Combine(profile, Path.Combine(".codex", "bin"), EXECUTABLE_NAME)
        };

        return candidates.FirstOrDefault(File.Exists)
            ?? FindOnPath(pathEnvironment ?? Environment.GetEnvironmentVariable("PATH"));
    }

    private static string? FindOnPath(string? path)
    {

        if (string.IsNullOrWhiteSpace(path))
            return null;

        return path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(static directory => Path.Combine(directory, EXECUTABLE_NAME))
            .FirstOrDefault(File.Exists);
    }
}
