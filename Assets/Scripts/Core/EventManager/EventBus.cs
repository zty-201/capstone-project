using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventBus
{
    // ==========================================
    // GAME STATE & TIME EVENTS
    // ==========================================
    public static event Action OnGameInitialized;
    public static event Action<int> OnDayEnded;
    public static event Action OnNextDayStarted;

    // ==========================================
    // MISSION & PDCA EVENTS
    // ==========================================
    public static event Action<int> OnMissionStarted;

    /// <summary>
    /// int: Mission ID
    /// bool: True if optimal solution, False if trivial
    /// </summary>
    public static event Action<int, bool> OnMissionCompleted;
    public static void RaiseMissionCompleted(int missionID, bool wasOptimal)
        => OnMissionCompleted?.Invoke(missionID, wasOptimal);

    // ==========================================
    // PLAYER & MOVEMENT EVENTS
    // ==========================================
    public static event Action<Vector2Int> OnPlayerMoved;
    public static void RaisePlayerMoved(Vector2Int gridPos)
        => OnPlayerMoved?.Invoke(gridPos);

    public static event Action<Vector2Int, Vector2Int> OnPathRequested;
    public static void RaisePathRequested(Vector2Int start, Vector2Int end)
        => OnPathRequested?.Invoke(start, end);

    public static event Action<List<GridNode>> OnPathGenerated;
    public static void RaisePathGenerated(List<GridNode> path)
        => OnPathGenerated?.Invoke(path);

    // ==========================================
    // KAIZEN / UI EVENTS
    // ==========================================
    public static event Action<int> OnEfficiencyScoreUpdated;

    public static event Action<Vector3> OnMapClicked;
    public static void RaiseMapClicked(Vector3 worldPos)
        => OnMapClicked?.Invoke(worldPos);

    public static event Action<Vector3> OnPuzzleClicked;
    public static void RaisePuzzleClicked(Vector3 worldPos)
        => OnPuzzleClicked?.Invoke(worldPos);

    public static event Action<int, SolutionType> OnSolutionSelected;
    public static void RaiseSolutionSelected(int missionID, SolutionType type)
        => OnSolutionSelected?.Invoke(missionID, type);

    public static event Action<Vector3> OnWellClicked;
    public static void RaiseWellClicked(Vector3 worldPos)
        => OnWellClicked?.Invoke(worldPos);
}
