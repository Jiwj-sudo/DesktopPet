namespace DeskCat.Models;

public sealed class PetAttributes
{
    public double Satiety { get; set; } = 80;
    public double Mood { get; set; } = 70;
    public double Energy { get; set; } = 85;
    public DateTime LastUpdatedAt { get; set; } = DateTime.Now;

    // 保存的位置信息（NaN 表示未保存）
    public double SavedLeft { get; set; } = double.NaN;
    public double SavedTop { get; set; } = double.NaN;

    public bool HasSavedPosition => !double.IsNaN(SavedLeft) && !double.IsNaN(SavedTop);

    public void Clamp()
    {
        Satiety = Math.Clamp(Satiety, 0, 100);
        Mood = Math.Clamp(Mood, 0, 100);
        Energy = Math.Clamp(Energy, 0, 100);
    }
}
