using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SoundManager : MonoBehaviour
{

    public static SoundManager instance { get; set; } = null;
    public float bgmVolume { get; set; } = 1;
    public float seVolume { get; set; } = 1;


    private Dictionary<string, AudioClip> bgmClip = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> seClip = new Dictionary<string, AudioClip>();
    public AudioSource bgmSource { get; private set; }
    private AudioSource[] seSource;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }

        bgmSource = transform.Find("Bgm").GetComponent<AudioSource>();
        // seSource = transform.Find("Se").GetComponent<AudioSource>();
        seSource = new AudioSource[10];
        for (int i = 0; i < 10; i++)
        {
            GameObject tempSet = new GameObject("Se_" + i);
            tempSet.transform.parent = transform;
            seSource[i] = tempSet.AddComponent<AudioSource>();
        }
    
        AudioClip[] Clip = Resources.LoadAll<AudioClip>("Sound/1.Bgm");

        foreach(AudioClip oneClip in Clip)
        {
            bgmClip.Add(oneClip.name, oneClip);
        }

        Clip = Resources.LoadAll<AudioClip>("Sound/2.Se");

        foreach (AudioClip oneClip in Clip)
        {
            seClip.Add(oneClip.name, oneClip);
        }
    }
    public void SePlay(string seName, float pitch) { SePlayOneShot(seName, seVolume, pitch); }
    public void SePlayVolume(string seName, float volume) { SePlayOneShot(seName, volume, 1); }
    public void SePlay(string seName) { SePlayOneShot(seName, seVolume,1); }
    private void SePlayOneShot(string seName,float volume, float pitch)
    {
        if (seClip.ContainsKey(seName) == false)
        {
            Debug.LogError("해당 클립이 존재하지 않음");
            return;
        }

        for (int i = 0; i < 10; i++)
        {
            if(!seSource[i].isPlaying)
            {
                seSource[i].pitch = pitch;
                seSource[i].PlayOneShot(seClip[seName], volume);
                break;
            }
        }
    }

    public void BgmPlay(string seName) { BgmPlayLoop(seName, bgmVolume); }
    private void BgmPlayLoop(string bgmName, float volume)
    {
        if(bgmClip.ContainsKey(bgmName) == false)
        {
            Debug.LogError("해당 클립이 존재하지 않음");
            return;
        }
        bgmSource.loop = true;

        bgmSource.volume = volume;
        bgmSource.clip = bgmClip[bgmName];
        bgmSource.Play();
    }
}
