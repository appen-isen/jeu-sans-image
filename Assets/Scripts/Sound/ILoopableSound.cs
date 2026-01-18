using UnityEngine;

public abstract class ILoopableSound : MonoBehaviour
{
    protected AudioSource source;

    protected void CreateSource()
    {
        source = Instantiate(SoundManager.Instance.audioSourcePrefab).GetComponent<AudioSource>();
        source.loop = true;
    }

    protected void DestroySource()
    {
        if (source != null)
        {
            Destroy(source.gameObject);
        }
    }

    protected void OnDestroy()
    {
        DestroySource();
    }

    public void PlayLoopableSound(Vector3 position)
    {
        CreateSource();
        source.transform.position = position;
        PlayLoopableSound();
    }

    public void PlayLoopableSound(Transform parent)
    {
        CreateSource();
        source.transform.SetParent(parent, false);
        PlayLoopableSound();
    }

    protected virtual void PlayLoopableSound() { }

    protected virtual void StopLoopableSound()
    {
        DestroySource();
    }

    public bool IsPlaying { get { return source != null; } }
}