using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The sound manager is responsible for handling short lived sounds.
/// Those are sounds that should not loop.
/// If you want to manage a looped sound, use the MusicManager or place
/// it in the scene "as-is" instead.
/// </summary>
public class SoundManager : MonoBehaviour
{
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private GameObject audioSourcePrefab;
    
    private List<AudioSource> audioPool;
    private Queue<AudioSource> availableAudios;

    // Singleton pattern
    [System.NonSerialized]  // Do not show it in editor and do not save its value
    public static SoundManager Instance = null;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (audioSourcePrefab == null)
        {
            Debug.LogWarning("Cannot create a pool of audio sources: audio source prefab not set");
        }

        audioPool = new List<AudioSource>(initialPoolSize);
        availableAudios = new Queue<AudioSource>(initialPoolSize);
        
        // For each initially pooled audio
        for (int i=0; i<initialPoolSize; i++)
        {
            // Create a new pooled audio and mark it as available
            availableAudios.Enqueue(ExtendPool());
        }
    }

    private AudioSource ExtendPool()
    {
        AudioSource source = Instantiate(
            audioSourcePrefab,
            transform
        ).GetComponent<AudioSource>();
        audioPool.Add(source);
        return source;
    }

    private IEnumerator DelayedBackToPoolCoroutine(AudioSource source)
    {
        yield return new WaitForSeconds(source.clip.length);
        availableAudios.Enqueue(source);
    }

    public void PlaySoundAt(Vector3 position, AudioClip sound)
    {
        // Take audio from pool if possible, extend pool otherwise
        AudioSource source = Instance.availableAudios.Count == 0 ?
                             Instance.ExtendPool() :
                             Instance.availableAudios.Dequeue();

        source.transform.position = position;
        source.clip = sound;
        source.loop = false;
        source.Play();
        StartCoroutine(Instance.DelayedBackToPoolCoroutine(source));
    }
}
