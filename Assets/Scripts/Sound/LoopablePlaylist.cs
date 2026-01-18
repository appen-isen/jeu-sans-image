using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoopablePlaylist : ILoopableSound
{
    [SerializeField] List<AudioClip> playlist;
    Coroutine playlistCoroutine = null;

    protected override void PlayLoopableSound()
    {
        base.PlayLoopableSound();

        if (playlist.Count == 0)
        {
            Debug.LogWarning("Playlist is empty");
            return;
        }
        source.loop = false;
        playlistCoroutine = StartCoroutine(PlaylistCoroutine());
        source.clip = playlist[0];
        source.Play();
    }

    protected override void StopLoopableSound()
    {
        StopCoroutine(playlistCoroutine);
        source.Stop();
        base.StopLoopableSound();
    }

    protected IEnumerator PlaylistCoroutine()
    {
        int playlistIndex = 0;
        while (true)
        {
            source.clip = playlist[playlistIndex];
            source.Play();
            yield return new WaitForSeconds(playlist[playlistIndex].length);
            playlistIndex = (playlistIndex + 1) % playlist.Count;
        }
    }
}
