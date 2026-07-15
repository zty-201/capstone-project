using UnityEngine;

[CreateAssetMenu(fileName = "StageRegistry", menuName = "Kaizen Systems/Stage Registry")]
public class StageRegistry : ScriptableObject
{
    public StageData[] stages;

    public StageData GetByIndex(int index) => (index >= 0 && index < stages.Length) ? stages[index] : null;
}
