using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CustomTaskScrollView : MonoBehaviour
{
    [Header("Front Layer (ScrollView)")]
    public ScrollRect frontScrollRect;
    public RectTransform frontContentPanel;

    [Header("Back Layer")]
    public RectTransform backPanel;
    public GameObject backTaskPrefab;

    [Header("Task Settings")]
    public float taskHeight = 100f;
    public int visibleFrontTasks = 3;

    [Header("Back Layer Visual Settings")]
    [Range(0.1f, 1f)]
    public float minBackScale = 0.5f;
    [Range(0f, 1f)]
    public float minBackAlpha = 0.2f;

    private List<RectTransform> frontTasks = new List<RectTransform>();
    private List<RectTransform> backTasks = new List<RectTransform>();
    private float lastContentPosition;

    private void Start()
    {
        if (frontScrollRect != null)
        {
            frontScrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }

        lastContentPosition = frontContentPanel.anchoredPosition.y;
    }

    public void UpdateScrollView()
    {
        // 获取前排任务
        frontTasks.Clear();
        foreach (RectTransform child in frontContentPanel)
        {
            frontTasks.Add(child);
        }

        // 当前排任务数量大于3时显示后排
        if (frontTasks.Count > 5)
        {
            InitializeBackTasks();
        }
        else
        {
            ClearBackTasks();
        }

        UpdateBackTaskVisuals(0);
    }

    private void InitializeBackTasks()
    {
        ClearBackTasks();

        float backPanelHeight = visibleFrontTasks * taskHeight;
        backPanel.sizeDelta = new Vector2(backPanel.sizeDelta.x, backPanelHeight);

        for (int i = 0; i < 5; i++) // 固定生成5个后排任务
        {
            GameObject task = Instantiate(backTaskPrefab, backPanel);
            RectTransform taskRect = task.GetComponent<RectTransform>();
            taskRect.sizeDelta = new Vector2(taskRect.sizeDelta.x, taskHeight);
            backTasks.Add(taskRect);
        }

        PositionBackTasks();
        backPanel.gameObject.SetActive(true);
    }

    private void ClearBackTasks()
    {
        foreach (RectTransform task in backTasks)
        {
            if (task != null)
            {
                Destroy(task.gameObject);
            }
        }
        backTasks.Clear();
        backPanel.gameObject.SetActive(false);
    }

    private void PositionBackTasks()
    {
        float backPanelHeight = backPanel.rect.height;

        for (int i = 0; i < backTasks.Count; i++)
        {
            float normalizedPos = (float)i / (backTasks.Count - 1);
            float yPos = normalizedPos * (backPanelHeight - taskHeight);
            backTasks[i].anchoredPosition = new Vector2(0, -yPos);
        }
    }

    private void OnScrollValueChanged(Vector2 value)
    {
        float currentContentPosition = frontContentPanel.anchoredPosition.y;
        float delta = currentContentPosition - lastContentPosition;
        lastContentPosition = currentContentPosition;
        UpdateBackTaskVisuals(delta);
    }

    private void UpdateBackTaskVisuals(float moveDelta)
    {
        if (backTasks.Count == 0) return;

        float backPanelHeight = backPanel.rect.height;

        for (int i = 0; i < backTasks.Count; i++)
        {
            RectTransform taskRect = backTasks[i];

            // 移动任务（反向）
            float newY = taskRect.anchoredPosition.y - moveDelta;
            newY = Mathf.Repeat(newY, backPanelHeight) - backPanelHeight;
            taskRect.anchoredPosition = new Vector2(taskRect.anchoredPosition.x, newY);

            // 计算视觉效果
            float normalizedPos = (-newY) / backPanelHeight;
            float t = Mathf.Abs(normalizedPos - 0.5f) * 2; // 0 at center, 1 at edges
            float scale = Mathf.Lerp(minBackScale, 1f, t);
            float alpha = Mathf.Lerp(minBackAlpha, 1f, t);

            // 应用视觉效果
            taskRect.localScale = new Vector3(scale, scale, 1f);
            SetAlpha(taskRect, alpha);
        }
    }

    private void SetAlpha(RectTransform taskRect, float alpha)
    {
        CanvasGroup canvasGroup = taskRect.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = taskRect.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = alpha;
    }
}