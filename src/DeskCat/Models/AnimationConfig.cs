namespace DeskCat.Models;

public sealed record AnimationConfig(
    PetState State,
    string FileName,
    int FrameCount,
    int FramesPerSecond,
    bool IsLooping = true,
    int FrameWidth = 128,
    int FrameHeight = 128)
{
    public static readonly IReadOnlyDictionary<PetState, AnimationConfig> Defaults =
        new Dictionary<PetState, AnimationConfig>
        {
            [PetState.Idle] = new(PetState.Idle, "idle.png", 4, 6),
            [PetState.Walk] = new(PetState.Walk, "walk.png", 6, 10),
            [PetState.Sit] = new(PetState.Sit, "sit.png", 2, 4),
            [PetState.Sleep] = new(PetState.Sleep, "sleep.png", 4, 4),
            [PetState.Eat] = new(PetState.Eat, "eat.png", 4, 7, IsLooping: false),
            [PetState.Happy] = new(PetState.Happy, "happy.png", 4, 8, IsLooping: false),
            [PetState.Angry] = new(PetState.Angry, "angry.png", 4, 8, IsLooping: false),
            [PetState.Curious] = new(PetState.Curious, "curious.png", 4, 7, IsLooping: false),
            [PetState.Drag] = new(PetState.Drag, "drag.png", 2, 5),
            [PetState.Fall] = new(PetState.Fall, "fall.png", 2, 6, IsLooping: false),
            [PetState.Pet] = new(PetState.Pet, "pet.png", 4, 8, IsLooping: false)
        };
}
