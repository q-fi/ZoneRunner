using UnityEngine;

public class DebugAddXPButton : MonoBehaviour
{
    public void Add500XP()
    {
        if (PlayerProgression.Instance == null)
        {
            Debug.LogWarning("PlayerProgression.Instance is null");
            return;
        }

        PlayerProgression.Instance.AddExperience(500);
        Debug.Log("DEBUG: Added 500 XP");
    }
}