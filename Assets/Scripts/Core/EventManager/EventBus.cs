using System;
using UnityEngine;
using System.Collections.Generic;

public static class EventBus
{
    // ==========================================
    // GAME STATE & TIME EVENTS
    // ==========================================
    public static Action OnGameInitialized;
    public static Action<int> OnDayEnded; // Passes the current day number
    public static Action OnNextDayStarted;

    // ==========================================
    // MISSION & PDCA EVENTS
    // ==========================================
    public static Action<int> OnMissionStarted; // Passes the Mission ID

    /// <summary>
    /// Triggered when an issue is solved.
    /// int: Mission ID
    /// bool: True if optimal solution (Proceed to next stage), False if trivial (Needs Check/Act tomorrow)
    /// </summary>
    public static Action<int, bool> OnMissionCompleted;

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
    public static Action<int> OnEfficiencyScoreUpdated; // For Advanced Mission 2

    // NEW Input Events
    public static Action<Vector3> OnMapClicked;
    public static Action<Vector3> OnPuzzleClicked;
}