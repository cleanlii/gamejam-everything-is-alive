using UnityEngine;
using UnityEngine.UI;

public class LevelCompleteTrigger : MonoBehaviour
{
    [Header("关卡设置")]
    public int levelIndex = 1; // 当前关卡索引

    [Header("UI显示")]
    public GameObject completionUI; // 完成UI面板
    public Text completionText; // 完成文本
    public Text timeText; // 时间显示

    private bool hasCompleted;
    private float startTime;

    private void Start()
    {
        startTime = Time.time;
    }

    /// <summary>
    ///     完成当前关卡
    /// </summary>
    public void CompleteLevel()
    {
        if (hasCompleted) return;

        hasCompleted = true;
        var completionTime = Time.time - startTime;

        Debug.Log($"关卡 {levelIndex} 完成！用时: {completionTime:F2}秒");

        // 显示完成UI
        // ShowCompletionUI(completionTime);

        // 播放完成音效
        // AudioManager.Current?.PlaySound("关卡完成", AudioType.SFX);

        // 通知GameManager关卡完成（会自动处理跳转和动画）
        GameManager.Instance.CompleteCurrentLevel(levelIndex, completionTime);
    }

    /// <summary>
    ///     显示完成UI
    /// </summary>
    private void ShowCompletionUI(float completionTime)
    {
        if (completionUI != null)
        {
            completionUI.SetActive(true);

            if (completionText != null) completionText.text = $"关卡 {levelIndex} 完成！";

            if (timeText != null) timeText.text = $"用时: {completionTime:F2}秒";
        }
    }
}