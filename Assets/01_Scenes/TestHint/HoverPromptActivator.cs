using UnityEngine;
using UnityEngine.UI;  // 如果是 Image 类型 UI 图像，确保使用这个命名空间

public class HoverPromptActivator : MonoBehaviour
{
    [Header("倒计时时长 (秒)")]
    public float countdownTime = 10f;

    [Header("鼠标悬停时激活的目标物体")]
    public GameObject targetObject;

    private float timer;
    private bool countdownFinished = false;
    private CanvasGroup canvasGroup;  // 控制自身图像显示
    private bool isMouseOver = false;

    void Start()
    {
        timer = countdownTime;

        // 初始化图像为隐藏状态
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f; // 完全透明
        canvasGroup.blocksRaycasts = true; // 允许接收鼠标事件
    }

    void Update()
    {
        if (!countdownFinished)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                countdownFinished = true;
                canvasGroup.alpha = 1f; // 显示图像
            }
        }
    }

    void OnMouseEnter()
    {
        if (countdownFinished && targetObject != null)
        {
            targetObject.SetActive(true);
        }
    }

    void OnMouseExit()
    {
        if (countdownFinished && targetObject != null)
        {
            targetObject.SetActive(false);
        }
    }
}
