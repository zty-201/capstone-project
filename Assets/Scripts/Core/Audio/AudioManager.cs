using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource exclusiveSource;

    [Header("Mission Stingers")]
    [SerializeField] private AudioClip missionCompletedOptimalClip;
    [SerializeField] private AudioClip missionCompletedTrivialClip;
    [SerializeField] private AudioClip dayCompletedClip;

    [Header("UI")]
    [SerializeField] private AudioClip uiClickClip;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (musicSource == null) Debug.LogError($"[{name}] musicSource is not assigned!", this);
        if (sfxSource == null) Debug.LogError($"[{name}] sfxSource is not assigned!", this);
        if (exclusiveSource == null) Debug.LogError($"[{name}] exclusiveSource is not assigned!", this);
    }

    private void OnEnable()
    {
        EventBus.OnMissionCompleted += HandleMissionCompleted;
        EventBus.OnDayCompleted += HandleDayCompleted;
    }

    private void OnDisable()
    {
        EventBus.OnMissionCompleted -= HandleMissionCompleted;
        EventBus.OnDayCompleted -= HandleDayCompleted;
    }

    private void HandleMissionCompleted(int missionID, bool wasOptimal)
        => PlaySFX(wasOptimal ? missionCompletedOptimalClip : missionCompletedTrivialClip);

    private void HandleDayCompleted(int day) => PlaySFX(dayCompletedClip);

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null || musicSource.clip == clip) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic() => musicSource.Stop();

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    // No-argument wrapper so Button.OnClick() can wire it directly in the Inspector.
    public void PlayUIClick() => PlaySFX(uiClickClip);

    /// <summary>
    /// For sounds that must never overlap themselves (e.g. footsteps): restarts
    /// playback on the same dedicated source instead of layering via PlayOneShot,
    /// so rapid repeated calls cut the previous instance off rather than stacking.
    /// </summary>
    public void PlaySFXExclusive(AudioClip clip)
    {
        if (clip == null) return;
        exclusiveSource.clip = clip;
        exclusiveSource.Play();
    }
}
