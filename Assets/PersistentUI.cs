using UnityEngine;

// Attach to any UI prefab that should survive scene loads.
// Uses the GameObject name to detect and destroy duplicates.
public class PersistentUI : MonoBehaviour
{
    void Awake()
    {
        // Destroy duplicate if one with the same name already exists
        foreach (var other in FindObjectsByType<PersistentUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (other != this && other.gameObject.name == gameObject.name)
            {
                Destroy(gameObject);
                return;
            }
        }

        DontDestroyOnLoad(gameObject);
    }
}
