using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectInfoController : MonoBehaviour
{
    [Header("当前交互状态")]
    [Space(5)]
    [SerializeField] private bool isDragging;
    [SerializeField] private bool isHovering;
    [SerializeField] private bool isVisible; // 信息框是否可见

    [Header("浮窗界面_物品信息")]
    [Space(5)]
    [SerializeField] private RectTransform objInfoBanner; // 悬浮查看信息框
    [SerializeField] private CanvasGroup canvasGroup; // 控制透明度
    [SerializeField] private TextMeshProUGUI nameTMP; // 显示名称
    [SerializeField] private TextMeshProUGUI sizeTMP; // 显示尺寸
    [SerializeField] private TextMeshProUGUI valueTitleTMP; // 价值类型
    // [SerializeField] private LocalizedString valueTitleTMP; // 价值类型
    [SerializeField] private TextMeshProUGUI valueTMP; // 显示价值
    [SerializeField] private TextMeshProUGUI desTMP; // 显示描述

    [Header("浮窗界面_动画参数")]
    [Space(5)]
    [SerializeField] private CanvasScaler canvasScaler; // 用于动态调整偏移量
    [SerializeField] private Vector3 offset = new(10, -10, 0); // 信息框偏移量
    private Tween _fadeTween; // DoTween 动画缓存

    private Vector3 _adjustedOffset;

    private void Start()
    {
        var scaleFactor = canvasScaler.scaleFactor;
        _adjustedOffset = offset * scaleFactor;
    }

    private void Update()
    {
        if (isHovering && isVisible) UpdatePosition(Input.mousePosition);
    }

    private void OnEnable()
    {
        ItemInteraction.OnItemHovered += ProcessMouseEnter;
        ItemInteraction.OnItemUnhovered += ProcessMouseExit;
        BackpackInteraction.OnBpHovered += ProcessMouseEnter;
        BackpackInteraction.OnBpUnhovered += ProcessMouseExit;
        DragReminderController.OnObjectDragged += ProcessMouseDrag;
        DragReminderController.OnObjectUndragged += ProcessMouseEndDragged;

        LevelStateController.InteruptionEvent += HideBannerInstant;
    }

    private void OnDisable()
    {
        ItemInteraction.OnItemHovered -= ProcessMouseEnter;
        ItemInteraction.OnItemUnhovered -= ProcessMouseExit;
        BackpackInteraction.OnBpHovered -= ProcessMouseEnter;
        BackpackInteraction.OnBpUnhovered -= ProcessMouseExit;
        DragReminderController.OnObjectDragged -= ProcessMouseDrag;
        DragReminderController.OnObjectUndragged -= ProcessMouseEndDragged;

        LevelStateController.InteruptionEvent += HideBannerInstant;
    }

    #region 悬浮信息

    private void ProcessMouseEnter(ItemData itemData)
    {
        if (isDragging) return;

        if (itemData != null && !isHovering)
        {
            isHovering = true;
            var objInfo = new ItemInformation(itemData);
            UpdateBanner(objInfo);
            ShowBanner();
        }
        else if (itemData == null && isHovering)
        {
            isHovering = false;
            HideBanner();
        }
    }

    private void ProcessMouseEnter(BackpackData bpData)
    {
        if (isDragging) return;

        if (bpData != null && !isHovering)
        {
            isHovering = true;
            var objInfo = new BackpackInformation(bpData);
            UpdateBanner(objInfo);
            ShowBanner();
        }
        else if (bpData == null && isHovering)
        {
            isHovering = false;
            HideBanner();
        }
    }

    private void ProcessMouseExit()
    {
        if (isDragging) return;

        HideBanner();
        isHovering = false;
    }

    private void ProcessMouseDrag()
    {
        isDragging = true;
        HideBannerInstant();
    }

    private void ProcessMouseEndDragged()
    {
        isDragging = false;
    }

    private void UpdateBanner(ItemInformation itemInfo)
    {
        nameTMP.text = itemInfo.name;
        sizeTMP.text = $"{itemInfo.size.ToString()}格";
        valueTMP.text = itemInfo.value.ToString();
        desTMP.text = itemInfo.description;

        // TODO: 本地化配置
        valueTitleTMP.text = "配送收入：";
    }

    private void UpdateBanner(BackpackInformation bpInfo)
    {
        nameTMP.text = bpInfo.name;
        sizeTMP.text = $"{bpInfo.size.ToString()}格";
        desTMP.text = bpInfo.description;

        // TODO: 本地化配置
        switch (bpInfo.state)
        {
            case ObjectState.Awaiting:
                valueTitleTMP.text = "买入价格：";
                valueTMP.text = bpInfo.price.ToString();
                break;
            case ObjectState.Resellable:
                valueTitleTMP.text = "卖出价格：";
                valueTMP.text = bpInfo.value.ToString();
                break;
        }
    }

    #endregion

    #region 浮窗显示

    /// <summary>
    ///     显示信息框并设置描述
    /// </summary>
    private void ShowBanner()
    {
        isVisible = true; // 设置为可见状态
        _fadeTween?.Kill(); // 停止当前动画
        canvasGroup.alpha = 0;
        _fadeTween = canvasGroup.DOFade(1f, 0.3f); // 使用 DoTween 淡入
    }

    /// <summary>
    ///     隐藏信息框
    /// </summary>
    private void HideBanner()
    {
        _fadeTween?.Kill();
        _fadeTween = canvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        });
        isVisible = false;
    }

    /// <summary>
    ///     立即隐藏信息框
    /// </summary>
    private void HideBannerInstant()
    {
        if (!isVisible) return;

        _fadeTween?.Kill();
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        isVisible = false;
    }

    /// <summary>
    ///     更新信息框位置
    /// </summary>
    private void UpdatePosition(Vector3 mousePosition)
    {
        if (!isVisible) return;

        // 直接使用 anchoredPosition 设置 UI 位置
        Vector2 newPosition = mousePosition + _adjustedOffset;
        objInfoBanner.anchoredPosition = newPosition;

        // 防止超出屏幕
        ClampToScreen();
    }

    /// <summary>
    ///     防止信息框超出屏幕
    /// </summary>
    private void ClampToScreen()
    {
        var corners = new Vector3[4];
        objInfoBanner.GetWorldCorners(corners);
        var adjustedPosition = objInfoBanner.anchoredPosition;

        // 获取屏幕宽高
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // 计算 UI 组件的宽高
        var bannerWidth = objInfoBanner.rect.width * objInfoBanner.lossyScale.x;
        var bannerHeight = objInfoBanner.rect.height * objInfoBanner.lossyScale.y;

        // 右侧超出 → 移动到鼠标左侧
        if (corners[2].x > screenWidth)
            adjustedPosition.x -= bannerWidth + _adjustedOffset.x;

        // 左侧超出 → 移动到鼠标右侧
        if (corners[0].x < 0)
            adjustedPosition.x += bannerWidth + _adjustedOffset.x;

        // 底部超出 → 移动到鼠标上方
        if (corners[0].y < 0)
            adjustedPosition.y += bannerHeight + _adjustedOffset.y;

        // 顶部超出 → 移动到鼠标下方
        if (corners[1].y > screenHeight)
            adjustedPosition.y -= bannerHeight + _adjustedOffset.y;

        objInfoBanner.anchoredPosition = adjustedPosition;
    }

    #endregion
}