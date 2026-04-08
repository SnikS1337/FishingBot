using Newtonsoft.Json;

namespace FishingBot.Core.Config;

public sealed class ConfigService
{
    public BotConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            return new BotConfig();
        }

        var json = File.ReadAllText(path);
        var loaded = JsonConvert.DeserializeObject<BotConfig>(json);
        return loaded ?? new BotConfig();
    }

    public void Save(string path, BotConfig config)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText(path, json);
    }
}
