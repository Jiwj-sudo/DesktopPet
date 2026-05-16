using System.Windows;

namespace DeskCat.Services;

public sealed class MovementService
{
    private readonly Random _random = new();
    private double _direction = 1;
    private double _speed = 42;

    public double FacingScale => _direction >= 0 ? 1 : -1;

    public void PickNewDirection()
    {
        _direction = _random.Next(2) == 0 ? -1 : 1;
        _speed = _random.Next(28, 68);
    }

    public System.Windows.Point Update(System.Windows.Point current, System.Windows.Size petSize, System.Windows.Rect bounds, TimeSpan elapsed)
    {
        var nextX = current.X + _direction * _speed * elapsed.TotalSeconds;
        var minX = bounds.Left;
        var maxX = Math.Max(bounds.Left, bounds.Right - petSize.Width);

        if (nextX <= minX)
        {
            nextX = minX;
            _direction = 1;
        }
        else if (nextX >= maxX)
        {
            nextX = maxX;
            _direction = -1;
        }

        var groundY = Math.Max(bounds.Top, bounds.Bottom - petSize.Height - 8);
        return new System.Windows.Point(nextX, groundY);
    }
}
