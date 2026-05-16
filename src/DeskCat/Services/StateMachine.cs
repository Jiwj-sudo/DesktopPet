using DeskCat.Models;

namespace DeskCat.Services;

public sealed class StateMachine
{
    public event EventHandler<PetState>? StateChanged;

    public PetState CurrentState { get; private set; } = PetState.Idle;
    public DateTime EnteredAt { get; private set; } = DateTime.Now;

    public void TransitionTo(PetState next, bool force = false)
    {
        if (CurrentState == next && !force)
        {
            return;
        }

        CurrentState = next;
        EnteredAt = DateTime.Now;
        StateChanged?.Invoke(this, next);
    }

    public TimeSpan TimeInState => DateTime.Now - EnteredAt;
}
