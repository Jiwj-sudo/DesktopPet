namespace DeskCat.Models;

public sealed class PetAttributes
{
    public double Satiety { get; set; } = 80;
    public double Mood { get; set; } = 70;
    public double Energy { get; set; } = 85;
    public DateTime LastUpdatedAt { get; set; } = DateTime.Now;

    public void Clamp()
    {
        Satiety = Math.Clamp(Satiety, 0, 100);
        Mood = Math.Clamp(Mood, 0, 100);
        Energy = Math.Clamp(Energy, 0, 100);
    }
}
