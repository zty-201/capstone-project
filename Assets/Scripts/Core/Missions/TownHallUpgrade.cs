using UnityEngine;

public class TownHallUpgrade : MonoBehaviour
{
    // Index 0 = default, 1 = Day 1 upgrade, 2 = Day 2 upgrade
    [SerializeField] private GameObject[] stages;

    private void OnEnable() => EventBus.OnDayCompleted += HandleDayCompleted;
    private void OnDisable() => EventBus.OnDayCompleted -= HandleDayCompleted;

    private void HandleDayCompleted(int day)
    {
        if (day >= stages.Length) return;

        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i] != null) stages[i].SetActive(i == day);
        }
    }
}
