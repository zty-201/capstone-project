using UnityEngine;

[CreateAssetMenu(fileName = "NewStage", menuName = "Kaizen Systems/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Stage Overview")]
    public int stageNumber;
    public string stageName;

    [Header("Missions In This Stage")]
    public int[] missionIDs;
}
