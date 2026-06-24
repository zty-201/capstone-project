using System.Collections.Generic;
using UnityEngine;

public class DayProgressTracker : MonoBehaviour
{
    [SerializeField] private int day = 1;
    [SerializeField] private int[] requiredOptimalMissions = { 1, 2 };

    private readonly HashSet<int> optimallyCompleted = new HashSet<int>();

    private void OnEnable() => EventBus.OnMissionCompleted += HandleMissionCompleted;
    private void OnDisable() => EventBus.OnMissionCompleted -= HandleMissionCompleted;

    private void HandleMissionCompleted(int missionID, bool wasOptimal)
    {
        if (!wasOptimal) return;

        foreach (int id in requiredOptimalMissions)
        {
            if (id == missionID) { optimallyCompleted.Add(missionID); break; }
        }

        foreach (int id in requiredOptimalMissions)
        {
            if (!optimallyCompleted.Contains(id)) return;
        }

        EventBus.RaiseDayCompleted(day);
    }
}
