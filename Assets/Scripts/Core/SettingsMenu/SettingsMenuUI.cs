using UnityEngine;
using UnityEngine.UI;

// The in-game settings overlay — currently just BGM/SFX volume sliders. Unlike MissionBoard/
// InfoBoard, this isn't triggered by walking up to a world IInteractable — OnOpenButtonPressed
// is wired to a persistent HUD button instead, since a settings menu needs to be reachable
// anytime, not from one specific spot in the village. Closing has two paths: ESC via
// SettingsMenuState (desktop) and OnCloseButtonPressed below (works everywhere, including
// Android, which has no ESC/back-key equivalent reaching Keyboard.current).
public class SettingsMenuUI : MonoBehaviour
{
    public static SettingsMenuUI Instance { get; private set; }

    [Header("UI References")]
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (musicVolumeSlider == null) Debug.LogError($"[{name}] musicVolumeSlider is not assigned!", this);
        if (sfxVolumeSlider == null) Debug.LogError($"[{name}] sfxVolumeSlider is not assigned!", this);

        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    // Wired to the persistent HUD settings button's OnClick in the Inspector.
    public void OnOpenButtonPressed()
    {
        Show();
        GameManager.Instance.StateManager.ChangeState(GameStateType.SettingsMenu);
    }

    public void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Reflect AudioManager's actual current volume rather than resetting the sliders to a
        // fixed default every time the menu opens. SetValueWithoutNotify so this doesn't loop
        // back into OnMusicVolumeChanged/OnSFXVolumeChanged and rewrite the value it just read.
        if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
        if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.SFXVolume);
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    // Wired to musicVolumeSlider's OnValueChanged in the Inspector.
    public void OnMusicVolumeChanged(float volume) => AudioManager.Instance.SetMusicVolume(volume);

    // Wired to sfxVolumeSlider's OnValueChanged in the Inspector.
    public void OnSFXVolumeChanged(float volume) => AudioManager.Instance.SetSFXVolume(volume);

    // Wired to a Close button's OnClick in the Inspector — needed on top of SettingsMenuState's
    // ESC handling, not instead of it: this game targets Android too, which has no ESC/back-key
    // equivalent reaching Keyboard.current, so ESC alone would leave no way to close the menu at
    // all on that platform.
    public void OnCloseButtonPressed()
        => GameManager.Instance.StateManager.ChangeState(GameStateType.Exploration);
}
