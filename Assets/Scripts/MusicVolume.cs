using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MusicVolume : MonoBehaviour
{
    public AudioMixer audioMixer;

    //ustawianie głośności muzyki
    public void SetVolume(float volume)
    {
            if (volume == -40f)
                audioMixer.SetFloat("volume", -80);
            else
                audioMixer.SetFloat("volume", volume);            
    }


}
