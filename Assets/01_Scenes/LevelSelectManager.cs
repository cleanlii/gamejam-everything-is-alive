using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectManager : MonoBehaviour
{
    [Header("关卡按钮")]
    [SerializeField] private Button[] levelButtons; // 关卡按钮数组 [L1, L2, L3]
    [SerializeField] private Animator[] levelButtonAnimators; // 关卡按钮动画器数组
    [SerializeField] private CanvasGroup[] levelBlock; // 关卡遮罩


    private void OnEnable()
    {
        // 订阅GameManager的事件
        GameManager.OnPlayUnlockAnimation += PlayUnlockAnimation;
        GameManager.OnSetLevelUnlocked += SetLevelUnlockedDirectly;
        GameManager.OnPlayCompletionAnimation += PlayCompletionAnimation;
        GameManager.OnPlayEndingAnimation += PlayEndingAnimation;
    }

    private void OnDisable()
    {
        // 取消订阅事件
        GameManager.OnPlayUnlockAnimation -= PlayUnlockAnimation;
        GameManager.OnSetLevelUnlocked -= SetLevelUnlockedDirectly;
        GameManager.OnPlayCompletionAnimation -= PlayCompletionAnimation;
        GameManager.OnPlayEndingAnimation -= PlayEndingAnimation;
    }

    private void Start()
    {
        // 初始化所有关卡按钮的状态
        InitializeLevelButtons();

        // 检查并播放待播放的动画
        GameManager.Instance.CheckAndPlayPendingAnimations();
    }

    /// <summary>
    ///     初始化关卡按钮状态
    /// </summary>
    private void InitializeLevelButtons()
    {
        for (var i = 1; i <= 3; i++)
        {
            var buttonIndex = i - 1; // 数组索引从0开始
            var isUnlocked = GameManager.Instance.levelProgress.IsLevelUnlocked(i);
            var isCompleted = GameManager.Instance.levelProgress.IsLevelCompleted(i);

            if (isCompleted)
            {
                // 已完成的关卡
                SetButtonState(buttonIndex, ButtonState.Completed, false);
            }
            else if (isUnlocked)
            {
                // 已解锁但未完成的关卡
                SetButtonState(buttonIndex, ButtonState.Unlocked, false);
            }
            else
            {
                // 锁定的关卡
                SetButtonState(buttonIndex, ButtonState.Locked, false);
            }
        }
    }

    /// <summary>
    ///     播放解锁动画
    /// </summary>
    /// <param name="levelIndex">关卡索引</param>
    private void PlayUnlockAnimation(int levelIndex)
    {
        var buttonIndex = levelIndex - 1;
        if (buttonIndex >= 0 && buttonIndex < levelButtonAnimators.Length)
        {
            // 播放解锁动画
            levelButtonAnimators[buttonIndex].SetTrigger("Unlock");
            Debug.Log($"播放关卡 {levelIndex} 解锁动画");

            // 动画结束后设置为解锁状态
            StartCoroutine(SetButtonStateAfterAnimation(buttonIndex, ButtonState.Unlocked, 2f));
        }
    }

    /// <summary>
    ///     直接设置关卡为已解锁状态（跳过动画）
    /// </summary>
    /// <param name="levelIndex">关卡索引</param>
    private void SetLevelUnlockedDirectly(int levelIndex)
    {
        var buttonIndex = levelIndex - 1;
        var isCompleted = GameManager.Instance.levelProgress.IsLevelCompleted(levelIndex);

        if (isCompleted)
            SetButtonState(buttonIndex, ButtonState.Completed, false);
        else
            SetButtonState(buttonIndex, ButtonState.Unlocked, false);
    }

    /// <summary>
    ///     播放圆满动画
    /// </summary>
    /// <param name="levelIndex">关卡索引</param>
    private void PlayCompletionAnimation(int levelIndex)
    {
        var buttonIndex = levelIndex - 1;
        if (buttonIndex >= 0 && buttonIndex < levelButtonAnimators.Length)
        {
            // 播放圆满动画
            levelButtonAnimators[buttonIndex].SetTrigger("Complete");
            Debug.Log($"播放关卡 {levelIndex} 圆满动画");

            // 动画结束后设置为完成状态
            StartCoroutine(SetButtonStateAfterAnimation(buttonIndex, ButtonState.Completed, 2f));
        }
    }

    /// <summary>
    ///     播放结局动画
    /// </summary>
    private void PlayEndingAnimation()
    {
        Debug.Log("在LevelSelect场景中播放结局动画");
        // 这里可以播放全屏的结局动画，比如显示结局动画面板等
        // 实际的结局动画逻辑根据你的需求来实现
    }

    /// <summary>
    ///     设置按钮状态
    /// </summary>
    /// <param name="buttonIndex">按钮索引</param>
    /// <param name="state">按钮状态</param>
    /// <param name="playAnimation">是否播放动画</param>
    private void SetButtonState(int buttonIndex, ButtonState state, bool playAnimation = true)
    {
        if (buttonIndex < 0 || buttonIndex >= levelButtons.Length) return;

        var button = levelButtons[buttonIndex];
        var canvasGroup = levelBlock[buttonIndex];

        switch (state)
        {
            case ButtonState.Locked:
                button.interactable = false;
                break;
            case ButtonState.Unlocked:
                button.interactable = true;
                break;
            case ButtonState.Completed:
                button.interactable = true;
                canvasGroup.transform.gameObject.SetActive(false);
                // TODO: 圆满状态涂色
                break;
        }
    }

    /// <summary>
    ///     动画播放完成后设置按钮状态
    /// </summary>
    /// <param name="buttonIndex">按钮索引</param>
    /// <param name="state">目标状态</param>
    /// <param name="delay">延迟时间</param>
    private IEnumerator SetButtonStateAfterAnimation(int buttonIndex, ButtonState state, float delay)
    {
        yield return new WaitForSeconds(delay);
        SetButtonState(buttonIndex, state, false);
    }

    public enum ButtonState
    {
        Locked,
        Unlocked,
        Completed
    }
}