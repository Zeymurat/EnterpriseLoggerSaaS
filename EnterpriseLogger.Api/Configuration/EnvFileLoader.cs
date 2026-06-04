namespace EnterpriseLogger.Api.Configuration;

/// <summary>
/// Walks up from the current directory to find the solution-root .env file.
/// </summary>
public static class EnvFileLoader
{
    public static string? FindEnvFilePath()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        return null;
    }

    public static void LoadIfPresent()
    {
        var envPath = FindEnvFilePath();
        if (envPath is not null)
            DotNetEnv.Env.Load(envPath);
    }
}
