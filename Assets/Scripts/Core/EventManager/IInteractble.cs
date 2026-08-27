using UnityEngine;

public interface IInteractable
{
    // Every interactable object must define what happens when tapped
    void Interact();

    // Sfx InputManager plays (via AudioManager) the moment this interactable is clicked —
    // distinct per interactable type, so a null clip on any given implementer just stays silent.
    AudioClip InteractSfx { get; }
}