using System.IO;
using System.Text.Json;
using DeskCat.Models;

namespace DeskCat.Services;

public sealed class AttributeService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _savePath;

    public AttributeService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeskCat");
        Directory.CreateDirectory(folder);
        _savePath = Path.Combine(folder, "attributes.json");
    }

    public PetAttributes Load()
    {
        try
        {
            if (File.Exists(_savePath))
            {
                var json = File.ReadAllText(_savePath);
                var loaded = JsonSerializer.Deserialize<PetAttributes>(json);
                if (loaded is not null)
                {
                    ApplyOfflineDecay(loaded);
                    return loaded;
                }
            }
        }
        catch
        {
            // Corrupt saves should not prevent the pet from starting.
        }

        return new PetAttributes();
    }

    public void Update(PetAttributes attributes, TimeSpan elapsed, PetState state)
    {
        var seconds = elapsed.TotalSeconds;
        attributes.Satiety -= 5.0 / 3600.0 * seconds;
        attributes.Mood -= 3.0 / 3600.0 * seconds;

        if (state == PetState.Sleep)
        {
            attributes.Energy += 5.0 / 60.0 * seconds;
        }
        else if (state == PetState.Walk)
        {
            attributes.Energy -= 1.0 / 60.0 * seconds;
        }
        else
        {
            attributes.Energy += 2.0 / 3600.0 * seconds;
        }

        attributes.LastUpdatedAt = DateTime.Now;
        attributes.Clamp();
    }

    public void Save(PetAttributes attributes)
    {
        try
        {
            attributes.LastUpdatedAt = DateTime.Now;
            attributes.Clamp();
            var json = JsonSerializer.Serialize(attributes, JsonOptions);
            var tempPath = $"{_savePath}.tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _savePath, overwrite: true);
        }
        catch
        {
            // Saving is best-effort; a transient file error should not crash the pet.
        }
    }

    private static void ApplyOfflineDecay(PetAttributes attributes)
    {
        var elapsed = DateTime.Now - attributes.LastUpdatedAt;
        if (elapsed <= TimeSpan.Zero)
        {
            return;
        }

        attributes.Satiety -= 5.0 * elapsed.TotalHours;
        attributes.Mood -= 3.0 * elapsed.TotalHours;
        attributes.Energy += 2.0 * elapsed.TotalHours;
        attributes.LastUpdatedAt = DateTime.Now;
        attributes.Clamp();
    }
}
