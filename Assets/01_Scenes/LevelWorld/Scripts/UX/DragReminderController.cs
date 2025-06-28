using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DragReminderController : MonoBehaviour
{
    public static event Action OnObjectDragged;
    public static event Action OnObjectUndragged;

    [Header("当前交互状态")]
    [Space(5)]
    [SerializeField] private bool isVisible; // 信息框是否可见

    [Header("浮窗界面_拖拽提示")]
    [Space(5)]
    [SerializeField] private RectTransform dragReminderBanner; // 拖拽时悬浮提示
    [SerializeField] private CanvasGroup canvasGroup; // 控制透明度的 CanvasGroup
    [SerializeField] private SpriteAnimator spriteAnimator;

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
        if (isVisible) UpdatePosition(Input.mousePosition);
    }

    private void OnEnable()
    {
        LevelStateController.InteruptionEvent += HideBannerInstant;
    }

    private void OnDisable()
    {
        LevelStateController.InteruptionEvent -= HideBannerInstant;
    }

    #region 拖拽提示信息

    public void ProcessMouseDrag()
    {
        if (isVisible) return;
        ShowBanner();
        OnObjectDragged?.Invoke();
    }

    public void ProcessMouseDrop()
    {
        if (!isVisible) return;
        HideBanner();
        OnObjectUndragged?.Invoke();
    }

    /// <summary>
    ///     显示信息框
    /// </summary>
    private void ShowBanner()
    {
        isVisible = true; // 设置为可见状态
        _fadeTween?.Kill(); // 停止当前动画
        canvasGroup.alpha = 0;
        _fadeTween = canvasGroup.DOFade(1f, 0.3f).OnComplete(() => { spriteAnimator.isAnimating = true; }); // 使用 DoTween 淡入
    }

    /// <summary>
    ///     隐藏信息框
    /// </summary>
    private void HideBanner()
    {
        _fadeTween?.Kill(); // 停止当前动画
        _fadeTween = canvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            spriteAnimator.isAnimating = false;
        });
        isVisible = false;
    }

    /// <summary>
    ///     立即隐藏信息框
    /// </summary>
    private void HideBannerInstant()
    {
        if (!isVisible) return;

        _fadeTween?.Kill(); // 停止当前动画
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        isVisible = false;
    }

    /// <summary>
    ///     更新信息框位置
    /// </summary>
    /// <param name="mousePosition">鼠标位置 (屏幕坐标)</param>
    private void UpdatePosition(Vector3 mousePosition)
    {
        if (!isVisible) return; // 信息框不可见时，不更新位置

        // 动态调整 offset 基于 Canvas Scaler
        var scaleFactor = canvasScaler.scaleFactor;
        var adjustedOffset = offset * scaleFactor;

        // 直接使用 anchoredPosition 设置 UI 位置
        Vector2 newPosition = mousePosition + adjustedOffset;
        dragReminderBanner.anchoredPosition = newPosition;

        // 防止信息框超出屏幕
        ClampToScreen();
    }

    /// <summary>
    ///     防止信息框超出屏幕
    /// </summary>
    private void ClampToScreen()
    {
        var corners = new Vector3[4];
        dragReminderBanner.GetWorldCorners(corners);
        var adjustedPosition = dragReminderBanner.anchoredPosition;

        // 获取屏幕宽高
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // 计算 UI 组件的宽高
        var bannerWidth = dragReminderBanner.rect.width * dragReminderBanner.lossyScale.x;
        var bannerHeight = dragReminderBanner.rect.height * dragReminderBanner.lossyScale.y;

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

        dragReminderBanner.anchoredPosition = adjustedPosition;
    }

    #endregion
}