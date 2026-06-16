using UnityEngine;

[CreateAssetMenu(fileName = "MissionRegistry", menuName = "Kaizen Systems/Mission Registry")]
public class MissionRegistry : ScriptableObject
{
    public MissionData[] missions;

    public MissionData GetByID(int id) => System.Array.Find(missions, m => m.missionID == id);
}
