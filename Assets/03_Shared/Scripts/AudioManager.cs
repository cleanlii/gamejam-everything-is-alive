using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Current;

    [Header("Audio Library")]
    public AudioLibrary audioLibrary; // 引用AudioLibrary ScriptableObject

    // 四条音轨
    private AudioSource _bgmSource;
    private AudioSource _bgsSource;
    private AudioSource _meSource;
    private AudioSource _seSource;

    // 爆炸音效的记录
    private List<AudioEntry> _explosionSounds;
    private int _lastExplosionIndex = -1;
    private int _repeatCount;

    private void Awake()
    {
        if (Current != null)
        {
            Destroy(gameObject);
            return;
        }

        Current = this;
        DontDestroyOnLoad(gameObject);

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgsSource = gameObject.AddComponent<AudioSource>();
        _meSource = gameObject.AddComponent<AudioSource>();
        _seSource = gameObject.AddComponent<AudioSource>();

        _explosionSounds = Current.audioLibrary.audioEntries.FindAll(x => x.type == AudioType.SE && x.name.Contains("货物爆炸"));

        // PlaySound("登录界面_BGM_MainTitle", "BGM", true);
        PlaySound("大世界_BGM_欢乐曲", AudioType.BGM, true);
    }

    private static AudioSource GetAudioSource(AudioType type)
    {
        switch (type)
        {
            case AudioType.BGM:
                return Current._bgmSource;
            case AudioType.BGS:
                return Current._bgsSource;
            case AudioType.ME:
                return Current._meSource;
            case AudioType.SE:
                return Current._seSource;
            default:
                return null;
        }
    }

    public static AudioType StringToAudioType(string typeName)
    {
        return (AudioType)Enum.Parse(typeof(AudioType), typeName.ToUpper());
    }

    public static void PlaySound(string name, AudioType type, bool loop)
    {
        var entry = Current.audioLibrary.audioEntries.Find(x => x.name == name && x.type == type);
        if (entry != null)
        {
            var source = GetAudioSource(entry.type);
            if (source != null)
            {
                source.clip = entry.clip;
                source.volume = entry.volume;
                source.loop = loop;
                source.Play();
            }
        }
        else
            Debug.LogWarning($"未找到声音： {name} ，类型： {type}！");
    }

    public static void StopAllSounds(float fadeOutDuration)
    {
        Current.StartCoroutine(Current.FadeOutAllSounds(fadeOutDuration));
    }

    public static void StopSound(AudioType type)
    {
        var source = GetAudioSource(type);
        source.Stop();
    }

    public static void PauseSound(AudioType type)
    {
        var source = GetAudioSource(type);
        source.Pause();
    }

    public static void ResumeSound(AudioType type)
    {
        var source = GetAudioSource(type);
        if (!source.isPlaying) source.UnPause();
    }

    /// <summary>
    ///     随机播放爆炸音效，防止连续播放同样的音效
    /// </summary>
    public static void PlayRandomExplosionSound()
    {
        // 检查爆炸音效是否已初始化
        if (Current._explosionSounds == null || Current._explosionSounds.Count == 0)
        {
            Debug.LogWarning("未找到任何爆炸音效！");
            return;
        }

        int randomIndex;

        // 防止连续播放同一个音效超过两次
        do
        {
            randomIndex = Random.Range(0, Current._explosionSounds.Count);
        } while (randomIndex == Current._lastExplosionIndex && Current._repeatCount >= 2);

        // 如果与上次相同，增加重复计数；否则重置计数
        if (randomIndex == Current._lastExplosionIndex)
            Current._repeatCount++;
        else
        {
            Current._repeatCount = 1;
            Current._lastExplosionIndex = randomIndex;
        }

        var selectedSound = Current._explosionSounds[randomIndex];
        var source = GetAudioSource(AudioType.SE);

        if (source != null)
        {
            source.clip = selectedSound.clip;
            source.volume = selectedSound.volume;
            source.Play();
        }
    }

    public IEnumerator FadeOutAllSounds(float duration)
    {
        float currentTime = 0;
        var startVolumeBGM = _bgmSource.volume;
        var startVolumeBGS = _bgsSource.volume;
        var startVolumeME = _meSource.volume;
        var startVolumeSE = _seSource.volume;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            var alpha = Mathf.Clamp01(currentTime / duration);
            _bgmSource.volume = Mathf.Lerp(startVolumeBGM, 0, alpha);
            _bgsSource.volume = Mathf.Lerp(startVolumeBGS, 0, alpha);
            _meSource.volume = Mathf.Lerp(startVolumeME, 0, alpha);
            _seSource.volume = Mathf.Lerp(startVolumeSE, 0, alpha);
            yield return null;
        }

        _bgmSource.Stop();
        _bgsSource.Stop();
        _meSource.Stop();
        _seSource.Stop();
        _bgmSource.volume = startVolumeBGM;
        _bgsSource.volume = startVolumeBGS;
        _meSource.volume = startVolumeME;
        _seSource.volume = startVolumeSE;
    }

    public IEnumerator FadeInAndPlaySound(string clipName, AudioType type, bool loop, float fadeInTime)
    {
        var entry = Current.audioLibrary.audioEntries.Find(x => x.name == clipName && x.type == type);

        if (entry != null)
        {
            var source = GetAudioSource(entry.type);
            if (source != null)
            {
                source.clip = entry.clip;
                source.volume = 0;
                source.loop = loop;
                source.Play();

                var elapsedTime = 0f;
                while (elapsedTime < fadeInTime)
                {
                    elapsedTime += Time.deltaTime;
                    source.volume = Mathf.Clamp01(elapsedTime / fadeInTime) * entry.volume;
                    yield return null;
                }

                source.volume = entry.volume;
            }
        }
        else
            Debug.LogWarning($"未找到声音： {clipName} ，类型： {type}！");
    }
}