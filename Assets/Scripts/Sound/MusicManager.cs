using UnityEngine;

/// <summary>
/// The music manager is responsible for handling loopable sounds.
/// If you want to manage a non-looped sound, use the SoundManager.
/// </summary>
public class MusicManager : MonoBehaviour
{
    // Singleton pattern
    [System.NonSerialized] public static MusicManager Instance = null;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void StartMusic(ILoopableSound music)
    {
        music.PlayLoopableSound(transform, false);
    }

    public void StartMusic(ILoopableSound music, Vector3 position)
    {
        music.PlayLoopableSound(position);
    }

    public void StartMusic(ILoopableSound music, Transform parent)
    {
        music.PlayLoopableSound(parent, true);
    }
}
