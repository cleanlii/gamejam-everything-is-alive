using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndingAnimationController : MonoBehaviour
{
    [Header("结局动画组件")]
    public Animator endingAnimator;
    public GameObject endingCanvasGroup;
    public Text endingTitleText;
    public Text endingDescriptionText;

    [Header("音频设置")]
    public AudioClip endingBGM;
    public AudioClip endingSFX;

    [Header("动画序列设置")]
    public float titleFadeInDelay = 1f;
    public float descriptionFadeInDelay = 3f;
    public float finalFadeOutDelay = 6f;

    private void Start()
    {
        // 订阅结局动画事件
        GameManager.OnPlayEndingAnimation += PlayEndingAnimation;

        // 初始隐藏结局UI
        if (endingCanvasGroup != null)
            endingCanvasGroup.SetActive(false);
    }

    private void OnDestroy()
    {
        GameManager.OnPlayEndingAnimation -= PlayEndingAnimation;
    }

    /// <summary>
    ///     播放结局动画
    /// </summary>
    private void PlayEndingAnimation()
    {
        StartCoroutine(EndingAnimationSequence());
    }

    /// <summary>
    ///     结局动画序列
    /// </summary>
    private IEnumerator EndingAnimationSequence()
    {
        Debug.Log("开始播放结局动画序列");

        // 显示结局画面
        if (endingCanvasGroup != null)
            endingCanvasGroup.SetActive(true);

        // 播放结局BGM
        // if (endingBGM != null)
        //     AudioManager.Current?.PlaySound(endingBGM.name, AudioType.BGM);

        // 触发结局动画
        if (endingAnimator != null)
            endingAnimator.SetTrigger("PlayEnding");

        // 延迟显示标题
        yield return new WaitForSeconds(titleFadeInDelay);
        if (endingTitleText != null)
        {
            endingTitleText.gameObject.SetActive(true);
            // 可以添加淡入动画
        }

        // 延迟显示描述
        yield return new WaitForSeconds(descriptionFadeInDelay - titleFadeInDelay);
        if (endingDescriptionText != null)
        {
            endingDescriptionText.gameObject.SetActive(true);
            // 可以添加淡入动画
        }

        // 等待最终淡出
        yield return new WaitForSeconds(finalFadeOutDelay - descriptionFadeInDelay);

        // 播放结局音效
        // if (endingSFX != null)
        //     AudioManager.Current?.PlaySound(endingSFX.name, AudioType.SFX);

        Debug.Log("结局动画序列播放完成");
    }
}