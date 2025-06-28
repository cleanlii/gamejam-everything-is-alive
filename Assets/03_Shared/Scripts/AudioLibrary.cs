using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "Library/Audio")]
public class AudioLibrary : ScriptableObject
{
    public List<AudioEntry> audioEntries;
}

[Serializable]
public class AudioEntry
{
    public string name;
    public AudioClip clip;
    public AudioType type;
    [Range(0f, 3f)]
    public float volume = 1.0f; // 默认音量为1.0
}

/// <summary>
///     音频类型枚举，表示不同类别的游戏音频
/// </summary>
public enum AudioType
{
    /// <summary>
    ///     背景音乐（Background Music）
    ///     主要用于游戏场景的主旋律音乐，通常循环播放，
    ///     用于营造游戏氛围，例如城镇、战斗或探索场景的音乐。
    /// </summary>
    BGM,

    /// <summary>
    ///     背景音效（Background Sound）
    ///     代表游戏环境的持续性声音，例如风声、流水声、鸟鸣等，
    ///     通常音量较低，可与 BGM 一同播放，以增强沉浸感。
    /// </summary>
    BGS,

    /// <summary>
    ///     音乐特效（Music Effect）
    ///     通常是短暂的音乐片段，用于特殊事件，如任务完成、游戏胜利或关卡通关。
    ///     这类音效通常不会循环播放，播放时会暂停 BGM（视实现而定）。
    /// </summary>
    ME,

    /// <summary>
    ///     音效（Sound Effect）
    ///     代表短促的音效，如按钮点击声、攻击命中声、脚步声等，
    ///     主要用于反馈玩家的操作或表现游戏内的动作。
    /// </summary>
    SE
}