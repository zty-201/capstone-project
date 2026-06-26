using UnityEngine;

public class InteractionIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject indicator;

    [Header("Settings")]
    [SerializeField] private float showRange = 1.5f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmplitude = 0.1f;

    private Transform player;
    private Vector3 indicatorBaseLocalPos;
    private bool forcedHidden;

    private void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (indicator != null)
            indicatorBaseLocalPos = indicator.transform.localPosition;

        indicator.SetActive(false);
    }

    private void Update()
    {
        if (forcedHidden || player == null || indicator == null) return;

        bool inExploration = GameManager.Instance != null &&
            GameManager.Instance.StateManager.CurrentStateType == GameStateType.Exploration;

        float dist = Vector2.Distance(transform.position, player.position);
        bool shouldShow = inExploration && dist <= showRange;
        indicator.SetActive(shouldShow);

        if (shouldShow)
        {
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            indicator.transform.localPosition = indicatorBaseLocalPos + Vector3.up * bob;
        }
    }

    // Call this when the owning interactable has been permanently consumed
    public void Hide()
    {
        forcedHidden = true;
        if (indicator != null) indicator.SetActive(false);
    }
}
