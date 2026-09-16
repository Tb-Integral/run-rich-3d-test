using UnityEngine;

namespace RunRich
{
    [CreateAssetMenu(menuName = "Run Rich/Audio Settings")]
    public sealed class RunAudioSettings : ScriptableObject
    {
        [field: SerializeField, Range(0, 1)] public float MasterVolume { get; private set; } = 0.65f;
        [field: SerializeField, Range(0, 1)] public float StepVolume { get; private set; } = 0.25f;
        [field: SerializeField, Range(0, 1)] public float EffectVolume { get; private set; } = 0.6f;
        [field: SerializeField, Min(0)] public float PickupCooldown { get; private set; } = 0.06f;
        [field: SerializeField] public AudioClip[] Footsteps { get; private set; }
        [field: SerializeField] public AudioClip[] Heels { get; private set; }
        [field: SerializeField] public AudioClip Collect { get; private set; }
        [field: SerializeField] public AudioClip LoseMoney { get; private set; }
        [field: SerializeField] public AudioClip Upgrade { get; private set; }
        [field: SerializeField] public AudioClip Gate { get; private set; }
        [field: SerializeField] public AudioClip Victory { get; private set; }
        [field: SerializeField] public AudioClip Defeat { get; private set; }
        [field: SerializeField] public AudioClip Click { get; private set; }
        [field: SerializeField] public AudioClip Photograph { get; private set; }
    }
}
