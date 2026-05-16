using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DeskCat.Models;

namespace DeskCat.Services;

public sealed class AnimationService
{
    private readonly DispatcherTimer _timer;
    private readonly Dictionary<string, BitmapSource> _spriteCache = new();
    private AnimationConfig _currentConfig = AnimationConfig.Defaults[PetState.Idle];
    private int _frameIndex;

    public event EventHandler<ImageSource>? FrameChanged;
    public event EventHandler<PetState>? AnimationCompleted;

    public AnimationService()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / _currentConfig.FramesPerSecond)
        };
        _timer.Tick += OnTick;
    }

    public void Play(PetState state)
    {
        _currentConfig = AnimationConfig.Defaults.TryGetValue(state, out var config)
            ? config
            : AnimationConfig.Defaults[PetState.Idle];

        _frameIndex = 0;
        _timer.Interval = TimeSpan.FromMilliseconds(1000.0 / _currentConfig.FramesPerSecond);
        EmitFrame();

        _timer.Stop();
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_currentConfig.FrameCount <= 1)
        {
            return;
        }

        if (_frameIndex >= _currentConfig.FrameCount - 1)
        {
            if (!_currentConfig.IsLooping)
            {
                _timer.Stop();
                AnimationCompleted?.Invoke(this, _currentConfig.State);
                return;
            }

            _frameIndex = 0;
        }
        else
        {
            _frameIndex++;
        }

        EmitFrame();
    }

    private void EmitFrame()
    {
        var sheet = GetSpriteSheet(_currentConfig.FileName);
        var x = _frameIndex * _currentConfig.FrameWidth;
        var rect = new Int32Rect(x, 0, _currentConfig.FrameWidth, _currentConfig.FrameHeight);
        var frame = new CroppedBitmap(sheet, rect);
        frame.Freeze();
        FrameChanged?.Invoke(this, frame);
    }

    private BitmapSource GetSpriteSheet(string fileName)
    {
        if (_spriteCache.TryGetValue(fileName, out var cached))
        {
            return cached;
        }

        var uri = new Uri($"pack://application:,,,/Resources/Animations/{fileName}", UriKind.Absolute);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        bitmap.UriSource = uri;
        bitmap.EndInit();
        bitmap.Freeze();
        _spriteCache[fileName] = bitmap;
        return bitmap;
    }
}
