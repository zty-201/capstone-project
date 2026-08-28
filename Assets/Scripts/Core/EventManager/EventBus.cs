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
    public static event Action<int> OnDayCompleted;
    public static void RaiseDayCompleted(int day) => OnDayCompleted?.Invoke(day);

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

    /// <summary>
    /// Fired at each sub-stage transition within a mission's Do phase, for the Mission
    /// Directory HUD to track progress finer-grained than PDCAPhase/OnMissionCompleted allow.
    /// Each raiser is single-path by construction (e.g. PartCollectionSystem only ever runs
    /// as part of the Optimal path), so it passes its own path literally rather than the
    /// listener inferring it from a separate OnSolutionSelected — that would race against
    /// this event on the same frame, since subscriber order between the two isn't guaranteed.
    /// int: Mission ID
    /// SolutionType: which path this progress belongs to (selects trivialObjectives vs. optimalObjectives)
    /// int: index into that path's MissionData.trivialObjectives/optimalObjectives
    /// int: current count for a counted stage (e.g. parts collected so far), 0 if not applicable
    /// int: total count for a counted stage, 0 if not applicable
    /// </summary>
    public static event Action<int, SolutionType, int, int, int> OnObjectiveProgress;
    public static void RaiseObjectiveProgress(int missionID, SolutionType path, int stageIndex, int count, int total)
        => OnObjectiveProgress?.Invoke(missionID, path, stageIndex, count, total);

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
    public static event Action<Vector3> OnMapClicked;
    public static void RaiseMapClicked(Vector3 worldPos)
        => OnMapClicked?.Invoke(worldPos);

    public static event Action<Vector3> OnPuzzleClicked;
    public static void RaisePuzzleClicked(Vector3 worldPos)
        => OnPuzzleClicked?.Invoke(worldPos);

    public static event Action<int, SolutionType> OnSolutionSelected;
    public static void RaiseSolutionSelected(int missionID, SolutionType type)
        => OnSolutionSelected?.Invoke(missionID, type);

    // ==========================================
    // STAGE GATE EVENTS
    // ==========================================
    /// int[]: IDs of missions that resolved trivially and need to be redone.
    /// Missions not in this array (already optimal) are untouched.
    public static event Action<int[]> OnMissionsNeedReview;
    public static void RaiseMissionsNeedReview(int[] missionIDs)
        => OnMissionsNeedReview?.Invoke(missionIDs);

    // ==========================================
    // INVENTORY EVENTS
    // ==========================================
    public static event Action OnInventoryChanged;
    public static void RaiseInventoryChanged() => OnInventoryChanged?.Invoke();

    // ==========================================
    // NPC TRUST EVENTS
    // ==========================================
    /// int: Mission ID
    /// int: New trust value
    public static event Action<int, int> OnTrustChanged;
    public static void RaiseTrustChanged(int missionID, int newTrust)
        => OnTrustChanged?.Invoke(missionID, newTrust);

    // ==========================================
    // PDCA PHASE EVENTS
    // ==========================================
    public static event Action<PDCAPhase> OnPDCAPhaseChanged;
    public static void RaisePDCAPhaseChanged(PDCAPhase phase)
        => OnPDCAPhaseChanged?.Invoke(phase);
}
