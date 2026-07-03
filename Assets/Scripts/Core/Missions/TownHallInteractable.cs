using UnityEngine;

public class TownHallInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private int currentDay = 1;

    public void Interact() => EventBus.RaiseDayCompleted(currentDay);
}
