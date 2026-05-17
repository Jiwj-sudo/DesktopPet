using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using DeskCat.Models;
using DeskCat.Services;

namespace DeskCat.ViewModels;

public sealed class PetViewModel : BindableBase, IDisposable
{
    private readonly AnimationService _animationService = new();
    private readonly AttributeService _attributeService = new();
    private readonly MovementService _movementService = new();
    private readonly StateMachine _stateMachine = new();
    private readonly DispatcherTimer _updateTimer;
    private readonly Random _random = new();
    private readonly TimeSpan _saveInterval = TimeSpan.FromSeconds(20);
    private DateTime _lastUpdate = DateTime.Now;
    private DateTime _lastSave = DateTime.Now;
    private DateTime _nextAutoTransitionAt = DateTime.Now;
    private ImageSource? _currentFrame;
    private PetState _state = PetState.Idle;
    private double _left;
    private double _top;
    private double _facingScale = 1;
    private bool _isTopmost = true;
    private bool _allowWalk = true;
    private bool _isDragging;

    public PetViewModel()
    {
        Attributes = _attributeService.Load();

        _stateMachine.StateChanged += (_, state) =>
        {
            State = state;
            _animationService.Play(state);
            ScheduleNextAutoTransition(state);
        };
        _animationService.FrameChanged += (_, frame) => CurrentFrame = frame;
        _animationService.AnimationCompleted += (_, state) => OnAnimationCompleted(state);

        _animationService.Play(PetState.Idle);
        ScheduleNextAutoTransition(PetState.Idle);

        _updateTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _updateTimer.Tick += (_, _) => Update();
        _updateTimer.Start();
    }

    public void InitializePosition()
    {
        var workArea = SystemParameters.WorkArea;
        // 让猫的实际内容右下角对齐屏幕右下角
        Left = workArea.Right - ContentMarginLeft - ContentWidth;
        Top = workArea.Bottom - ContentMarginTop - ContentHeight;
    }

    public PetAttributes Attributes { get; }
    public double PetSize { get; } = 256;

    // 猫实际内容在窗口中的边距（透明区域大小）
    public double ContentMarginLeft { get; } = 70;   // 左边透明约70
    public double ContentMarginTop { get; } = 0;     // 上边无透明（猫头顶贴边）
    public double ContentMarginRight { get; } = 70;   // 右边透明约70
    public double ContentMarginBottom { get; } = 130; // 下边透明约130（爪子下方空间大）

    // 猫实际内容大小
    public double ContentWidth => PetSize - ContentMarginLeft - ContentMarginRight;  // ~116
    public double ContentHeight => PetSize - ContentMarginTop - ContentMarginBottom; // ~126

    public ImageSource? CurrentFrame
    {
        get => _currentFrame;
        private set => SetProperty(ref _currentFrame, value);
    }

    public PetState State
    {
        get => _state;
        private set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public string StatusText => $"饱食 {Attributes.Satiety:0}  心情 {Attributes.Mood:0}  精力 {Attributes.Energy:0}";

    public double Left
    {
        get => _left;
        set => SetProperty(ref _left, value);
    }

    public double Top
    {
        get => _top;
        set => SetProperty(ref _top, value);
    }

    public double FacingScale
    {
        get => _facingScale;
        private set => SetProperty(ref _facingScale, value);
    }

    public bool IsTopmost
    {
        get => _isTopmost;
        set => SetProperty(ref _isTopmost, value);
    }

    public bool AllowWalk
    {
        get => _allowWalk;
        set
        {
            if (SetProperty(ref _allowWalk, value) && !value && State == PetState.Walk)
            {
                TransitionTo(PetState.Idle);
            }
        }
    }

    public bool BeginDrag()
    {
        if (Attributes.Energy < 10)
        {
            TransitionTo(PetState.Angry, force: true);
            return false;
        }

        _isDragging = true;
        TransitionTo(PetState.Drag, force: true);
        return true;
    }

    public void MoveDragged(double left, double top)
    {
        var workArea = SystemParameters.WorkArea;
        // 约束的是猫的实际内容边界，不是窗口边界
        Left = Math.Clamp(left,
            workArea.Left - ContentMarginLeft,
            workArea.Right - ContentMarginLeft - ContentWidth);
        Top = Math.Clamp(top,
            workArea.Top - ContentMarginTop,
            workArea.Bottom - ContentMarginTop - ContentHeight);
    }

    public void ClampToWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        Left = Math.Clamp(Left,
            workArea.Left - ContentMarginLeft,
            workArea.Right - ContentMarginLeft - ContentWidth);
        Top = Math.Clamp(Top,
            workArea.Top - ContentMarginTop,
            workArea.Bottom - ContentMarginTop - ContentHeight);
    }

    public void EndDrag()
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        Attributes.Mood -= 2;
        Attributes.Clamp();
        TransitionTo(PetState.Fall, force: true);
    }

    public void Click()
    {
        if (_isDragging)
        {
            return;
        }

        if (State == PetState.Sleep)
        {
            Attributes.Mood -= 3;
            Attributes.Clamp();
            TransitionTo(PetState.Angry, force: true);
            return;
        }

        Pet();
    }

    public void DoubleClick()
    {
        if (_isDragging)
        {
            return;
        }

        Attributes.Mood += 2;
        Attributes.Clamp();
        TransitionTo(PetState.Curious, force: true);
    }

    public void Feed()
    {
        Attributes.Satiety += 20;
        Attributes.Mood += 2;
        Attributes.Clamp();
        TransitionTo(PetState.Eat, force: true);
    }

    public void Play()
    {
        Attributes.Mood += 10;
        Attributes.Energy -= 5;
        Attributes.Clamp();
        TransitionTo(PetState.Happy, force: true);
    }

    public void Pet()
    {
        Attributes.Mood += 5;
        Attributes.Clamp();
        TransitionTo(PetState.Pet, force: true);
    }

    public void ToggleTopmost()
    {
        IsTopmost = !IsTopmost;
    }

    public void ToggleAllowWalk()
    {
        AllowWalk = !AllowWalk;
    }

    public void Save()
    {
        _attributeService.Save(Attributes);
    }

    public void Dispose()
    {
        _updateTimer.Stop();
        _animationService.Stop();
        Save();
    }

    private void Update()
    {
        var now = DateTime.Now;
        var elapsed = now - _lastUpdate;
        _lastUpdate = now;

        _attributeService.Update(Attributes, elapsed, State);
        OnPropertyChanged(nameof(StatusText));

        if (State == PetState.Walk && AllowWalk)
        {
            // 行走时传入内容大小和边距，让 MovementService 能正确计算
            var contentBounds = new System.Windows.Rect(
                GetCurrentWorkArea().Left + ContentMarginLeft,
                GetCurrentWorkArea().Top + ContentMarginTop,
                ContentWidth,
                ContentHeight);
            var position = _movementService.Update(
                new System.Windows.Point(Left + ContentMarginLeft, Top + ContentMarginTop),
                new System.Windows.Size(ContentWidth, ContentHeight),
                contentBounds,
                elapsed);
            Left = position.X - ContentMarginLeft;
            Top = position.Y - ContentMarginTop;
            FacingScale = _movementService.FacingScale;
            ClampToWorkArea();
        }

        if (!_isDragging)
        {
            ApplyAttributeDrivenState();
            if (DateTime.Now >= _nextAutoTransitionAt)
            {
                AutoTransition();
            }
        }

        if (now - _lastSave >= _saveInterval)
        {
            Save();
            _lastSave = now;
        }
    }

    private void ApplyAttributeDrivenState()
    {
        if (IsTemporaryState(State))
        {
            return;
        }

        if (Attributes.Satiety <= 1 && State != PetState.Angry)
        {
            TransitionTo(PetState.Angry);
        }
        else if (Attributes.Energy < 20 && State != PetState.Sleep)
        {
            TransitionTo(PetState.Sleep);
        }
        else if (Attributes.Mood > 85 && State is PetState.Idle or PetState.Sit)
        {
            Attributes.Mood -= 8;
            TransitionTo(PetState.Happy);
        }
    }

    private void AutoTransition()
    {
        if (IsTemporaryState(State) || _isDragging)
        {
            return;
        }

        var next = State switch
        {
            PetState.Idle => ChooseIdleExit(),
            PetState.Walk => _random.NextDouble() < 0.55 ? PetState.Idle : PetState.Sit,
            PetState.Sit => ChooseSitExit(),
            PetState.Sleep => PetState.Idle,
            _ => PetState.Idle
        };

        TransitionTo(next);
    }

    private PetState ChooseIdleExit()
    {
        if (!AllowWalk)
        {
            return PetState.Sit;
        }

        if (Attributes.Satiety < 20 || Attributes.Mood < 30)
        {
            return _random.NextDouble() < 0.7 ? PetState.Sit : PetState.Walk;
        }

        return _random.NextDouble() < 0.65 ? PetState.Walk : PetState.Sit;
    }

    private PetState ChooseSitExit()
    {
        if (Attributes.Energy < 35 || _random.NextDouble() < 0.35)
        {
            return PetState.Sleep;
        }

        return PetState.Idle;
    }

    private void OnAnimationCompleted(PetState state)
    {
        if (state == PetState.Fall)
        {
            SnapToGround();
        }

        if (IsTemporaryState(state))
        {
            TransitionTo(PetState.Idle);
        }
    }

    private void TransitionTo(PetState state, bool force = false)
    {
        if (state == PetState.Walk)
        {
            _movementService.PickNewDirection();
            FacingScale = _movementService.FacingScale;
        }

        _stateMachine.TransitionTo(state, force);
    }

    private void ScheduleNextAutoTransition(PetState state)
    {
        var seconds = state switch
        {
            PetState.Idle => _random.Next(3, 9),
            PetState.Walk => _random.Next(5, 16),
            PetState.Sit => _random.Next(5, 11),
            PetState.Sleep => _random.Next(30, 121),
            _ => 9999
        };
        _nextAutoTransitionAt = DateTime.Now.AddSeconds(seconds);
    }

    private static bool IsTemporaryState(PetState state)
    {
        return state is PetState.Eat or PetState.Happy or PetState.Angry
            or PetState.Curious or PetState.Drag or PetState.Fall or PetState.Pet;
    }

    private void SnapToGround()
    {
        ClampToWorkArea();
    }

    private System.Windows.Rect GetCurrentWorkArea()
    {
        return SystemParameters.WorkArea;
    }

    private static System.Windows.Rect GetPrimaryWorkArea()
    {
        return SystemParameters.WorkArea;
    }
}
