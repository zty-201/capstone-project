using System;
using System.Collections.Generic;
using UnityEngine;
using static MissionData;

public static class EventBus
{
    // ==========================================
    // GAME STATE & TIME EVENTS
    // ==========================================
    public static event Action OnGameInitialized;
    public static event Action<int> OnDayEnded; // Passes the current day number
    public static event Action OnNextDayStarted;

    // ==========================================
    // MISSION & PDCA EVENTS
    // ==========================================
    public static event Action<int> OnMissionStarted; // Passes the Mission ID

    /// <summary>
    /// Triggered when an issue is solved.
    /// int: Mission ID
    /// bool: True if optimal solution (Proceed to next stage), False if trivial (Needs Check/Act tomorrow)
    /// </summary>
    public static event Action<int, bool> OnMissionCompleted;
    public static void RaiseMissionCompleted(int missionID, bool wasOptimal)
    => OnMissionCompleted?.Invoke(missionID, wasOptimal);

    // ==========================================
    // PLAYER & MOVEMENT EVENTS
    // ==========================================
    /// <summary>
    /// Triggered when the backend grid validates a touch movement.
    /// Vector2Int: The validated target grid coordinate.
    /// </summary>
    public static Action<Vector2Int> OnPlayerMoved;
    public static Action<Vector2Int, Vector2Int> OnPathRequested; // Passes: Start Coordinate, Target Coordinate
    public static Action<List<GridNode>> OnPathGenerated; // Passes: The calculated route

    // ==========================================
    // KAIZEN / UI EVENTS
    // ==========================================
    public static event Action<int> OnEfficiencyScoreUpdated; // For Advanced Mission 2

    // NEW Input Events
    public static Action<Vector3> OnMapClicked;
    public static Action<Vector3> OnPuzzleClicked;
    public static event Action<int, SolutionType> OnSolutionSelected;
    public static void RaiseSolutionSelected(int missionID, SolutionType type)
    => OnSolutionSelected?.Invoke(missionID, type);
}