using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectManager : MonoBehaviour
{
    [Header("关卡按钮")]
    public Button level1Button;
    public Button level2Button;
    public Button level3Button;

    [Header("关卡锁定图标")]
    public GameObject level2LockIcon;
    public GameObject level3LockIcon;

    [Header("解锁动画")]
    public Animator level2UnlockAnimator;
    public Animator level3UnlockAnimator;

    // [Header("关卡信息显示")]
    // public Text level1InfoText;
    // public Text level2InfoText;
    // public Text level3InfoText;

    private void Start()
    {
        // 订阅解锁动画事件
        GameManager.OnPlayUnlockAnimation += PlayUnlockAnimation;

        // 初始化UI状态
        UpdateLevelButtonStates();
        // UpdateLevelInfoTexts();

        // 检查是否有待播放的解锁动画
        StartCoroutine(CheckUnlockAnimationsCoroutine());
    }

    private void OnDestroy()
    {
        // 取消订阅
        GameManager.OnPlayUnlockAnimation -= PlayUnlockAnimation;
    }

    private IEnumerator CheckUnlockAnimationsCoroutine()
    {
        // 等待一帧确保场景完全加载
        yield return null;

        // 检查并播放解锁动画
        GameManager.Instance.CheckAndPlayUnlockAnimations();
    }

    /// <summary>
    ///     更新关卡按钮状态
    /// </summary>
    private void UpdateLevelButtonStates()
    {
        var progress = GameManager.Instance.levelProgress;

        // L1 始终可用
        level1Button.interactable = true;

        // L2 状态
        var level2Unlocked = progress.IsLevelUnlocked(2);
        level2Button.interactable = level2Unlocked;
        if (level2LockIcon != null)
            level2LockIcon.SetActive(!level2Unlocked);

        // L3 状态
        var level3Unlocked = progress.IsLevelUnlocked(3);
        level3Button.interactable = level3Unlocked;
        if (level3LockIcon != null)
            level3LockIcon.SetActive(!level3Unlocked);
    }

    /// <summary>
    ///     更新关卡信息文本
    /// </summary>
    // private void UpdateLevelInfoTexts()
    // {
    //     var progress = GameManager.Instance.levelProgress;
    //
    //     // 更新L1信息
    //     if (level1InfoText != null)
    //     {
    //         var status = progress.IsLevelCompleted(1) ? "已完成" : "未完成";
    //         var bestTime = progress.GetBestTime(1);
    //         var playCount = progress.GetPlayCount(1);
    //
    //         level1InfoText.text = $"状态: {status}\n游玩次数: {playCount}";
    //         if (bestTime > 0)
    //             level1InfoText.text += $"\n最佳时间: {bestTime:F2}秒";
    //     }
    //
    //     // 更新L2信息
    //     if (level2InfoText != null)
    //     {
    //         if (progress.IsLevelUnlocked(2))
    //         {
    //             var status = progress.IsLevelCompleted(2) ? "已完成" : "未完成";
    //             var bestTime = progress.GetBestTime(2);
    //             var playCount = progress.GetPlayCount(2);
    //
    //             level2InfoText.text = $"状态: {status}\n游玩次数: {playCount}";
    //             if (bestTime > 0)
    //                 level2InfoText.text += $"\n最佳时间: {bestTime:F2}秒";
    //         }
    //         else
    //             level2InfoText.text = "完成关卡1解锁";
    //     }
    //
    //     // 更新L3信息
    //     if (level3InfoText != null)
    //     {
    //         if (progress.IsLevelUnlocked(3))
    //         {
    //             var status = progress.IsLevelCompleted(3) ? "已完成" : "未完成";
    //             var bestTime = progress.GetBestTime(3);
    //             var playCount = progress.GetPlayCount(3);
    //
    //             level3InfoText.text = $"状态: {status}\n游玩次数: {playCount}";
    //             if (bestTime > 0)
    //                 level3InfoText.text += $"\n最佳时间: {bestTime:F2}秒";
    //         }
    //         else
    //             level3InfoText.text = "完成关卡2解锁";
    //     }
    // }

    /// <summary>
    ///     播放解锁动画
    /// </summary>
    /// <param name="levelIndex">解锁的关卡索引</param>
    private void PlayUnlockAnimation(int levelIndex)
    {
        StartCoroutine(PlayUnlockAnimationCoroutine(levelIndex));
    }

    private IEnumerator PlayUnlockAnimationCoroutine(int levelIndex)
    {
        Animator targetAnimator = null;
        GameObject lockIcon = null;

        switch (levelIndex)
        {
            case 2:
                targetAnimator = level2UnlockAnimator;
                lockIcon = level2LockIcon;
                break;
            case 3:
                targetAnimator = level3UnlockAnimator;
                lockIcon = level3LockIcon;
                break;
            default:
                Debug.LogWarning($"不支持的关卡解锁动画索引: {levelIndex}");
                yield break;
        }

        if (targetAnimator != null)
        {
            // 播放解锁动画
            targetAnimator.SetTrigger("Unlock");

            // 可以添加音效
            // AudioManager.Current?.PlaySound("关卡解锁", AudioType.SFX);

            // 等待动画播放完成（根据你的动画长度调整）
            yield return new WaitForSeconds(2f);
        }

        // 更新UI状态
        UpdateLevelButtonStates();
        // UpdateLevelInfoTexts();

        Debug.Log($"关卡 {levelIndex} 解锁动画播放完成");
    }

    /// <summary>
    ///     UI按钮点击事件
    /// </summary>
    public void OnLevel1ButtonClick()
    {
        GameManager.Instance.LoadLevel1();
    }

    public void OnLevel2ButtonClick()
    {
        GameManager.Instance.LoadLevel2();
    }

    public void OnLevel3ButtonClick()
    {
        GameManager.Instance.LoadLevel3();
    }
}