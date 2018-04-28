using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayAmbience : MonoBehaviour
{
    public AudioSource source;
    public AudioClip[] ambientClips;
    public float ambientVolume = 0.5f;

    public void PlayAmbientClip()
    {
        AudioClip clip = ambientClips[Random.Range(0, ambientClips.Length)];
        source.PlayOneShot(clip, ambientVolume);
    }
}
