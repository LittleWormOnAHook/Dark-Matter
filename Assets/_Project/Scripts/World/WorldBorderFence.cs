using UnityEngine;

/// <summary>
/// Editor-only world border visual. Disabled as soon as play starts.
/// </summary>
[DisallowMultipleComponent]
public class WorldBorderFence : MonoBehaviour
{
    void Awake()
    {
        if (Application.isPlaying)
            gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (Application.isPlaying)
            gameObject.SetActive(false);
    }
}
