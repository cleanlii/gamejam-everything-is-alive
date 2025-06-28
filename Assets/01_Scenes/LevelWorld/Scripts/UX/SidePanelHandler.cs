using System;
using DG.Tweening;
using UnityEngine;

public class SidePanelHandler : MonoBehaviour
{
    [SerializeField] private SidePanelAnimator panelAnimator;
    public SidePanelState panelState;
    [SerializeField] private SlideDirection slideDirection;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Vector2 hiddenOffset;
    [SerializeField] private bool DoNotFade;
    private RectTransform _rectTransform;
    private Vector2 _originalPosition;
    private CanvasGroup _canvasGroup;
    private Tween _currentTween; // 保存当前正在执行的Tween

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        panelAnimator = GetComponent<SidePanelAnimator>();

        _originalPosition = _rectTransform.anchoredPosition;
        SetHiddenPosition(); // 初始化为隐藏位置
        panelState = SidePanelState.Hiding;

        if (Mathf.Approximately(_canvasGroup.alpha, 1)) DoNotFade = true;
    }

    private void SetHiddenPosition()
    {
        var hiddenPosition = _originalPosition;

        switch (slideDirection)
        {
            case SlideDirection.Up:
                hiddenPosition.y += hiddenOffset.y;
                break;
            case SlideDirection.Down:
                hiddenPosition.y -= hiddenOffset.y;
                break;
            case SlideDirection.Left:
                hiddenPosition.x -= hiddenOffset.x;
                break;
            case SlideDirection.Right:
                hiddenPosition.x += hiddenOffset.x;
                break;
        }

        _rectTransform.anchoredPosition = hiddenPosition;
    }

    public void Show(Action onComplete = null)
    {
        // 重置位置
        SetHiddenPosition();
        _canvasGroup.alpha = 1;
        FreezeContent(true);

        // 如果有当前动画，取消它
        _currentTween?.Kill();

        // 执行Show动画
        _currentTween = _rectTransform.DOAnchorPos(_originalPosition, animationDuration).OnComplete(() =>
        {
            _currentTween = null; // 动画完成后清空引用
            FreezeContent(false);
            panelState = SidePanelState.Showing;
            StartAnimation();
            onComplete?.Invoke();
        });
    }

    public void Hide(Action onComplete = null)
    {
        FreezeContent(true);

        // 取消当前动画
        _currentTween?.Kill();

        // 计算隐藏位置并执行新的隐藏动画
        var hiddenPosition = _originalPosition;

        switch (slideDirection)
        {
            case SlideDirection.Up:
                hiddenPosition.y += hiddenOffset.y;
                break;
            case SlideDirection.Down:
                hiddenPosition.y -= hiddenOffset.y;
                break;
            case SlideDirection.Left:
                hiddenPosition.x -= hiddenOffset.x;
                break;
            case SlideDirection.Right:
                hiddenPosition.x += hiddenOffset.x;
                break;
        }

        _currentTween = _rectTransform.DOAnchorPos(hiddenPosition, animationDuration)
            .OnComplete(
                () =>
                {
                    _currentTween = null;
                    if (!DoNotFade) _canvasGroup.alpha = 0;
                    FreezeContent(false);
                    panelState = SidePanelState.Hiding;
                    StopAnimation();
                    onComplete?.Invoke();
                }
            );
    }

    /// <summary>
    ///     （临时）Button调用用
    /// </summary>
    public void Hide()
    {
        FreezeContent(true);

        // 取消当前动画
        _currentTween?.Kill();

        // 计算隐藏位置并执行新的隐藏动画
        var hiddenPosition = _originalPosition;

        switch (slideDirection)
        {
            case SlideDirection.Up:
                hiddenPosition.y += hiddenOffset.y;
                break;
            case SlideDirection.Down:
                hiddenPosition.y -= hiddenOffset.y;
                break;
            case SlideDirection.Left:
                hiddenPosition.x -= hiddenOffset.x;
                break;
            case SlideDirection.Right:
                hiddenPosition.x += hiddenOffset.x;
                break;
        }

        _currentTween = _rectTransform.DOAnchorPos(hiddenPosition, animationDuration)
            .OnComplete(
                () =>
                {
                    _currentTween = null;
                    if (!DoNotFade) _canvasGroup.alpha = 0;
                    FreezeContent(false);
                    panelState = SidePanelState.Hiding;
                    StopAnimation();
                }
            );
    }

    public void ShowInstant(Action onComplete = null)
    {
        FreezeContent(true);

        // 如果有当前动画，取消它
        _currentTween?.Kill();

        _rectTransform.anchoredPosition = _originalPosition;

        // 执行Show动画
        _currentTween = _canvasGroup.DOFade(1f, 0.1f).OnComplete(() =>
        {
            _currentTween = null; // 动画完成后清空引用
            // FreezeContent(false);
            panelState = SidePanelState.Showing;
            StartAnimation();
            onComplete?.Invoke();
        });
    }

    public void HideInstant(Action onComplete = null)
    {
        FreezeContent(true);

        // 取消当前动画
        _currentTween?.Kill();

        // 计算隐藏位置并执行新的隐藏动画
        var hiddenPosition = _originalPosition;

        switch (slideDirection)
        {
            case SlideDirection.Up:
                hiddenPosition.y += hiddenOffset.y;
                break;
            case SlideDirection.Down:
                hiddenPosition.y -= hiddenOffset.y;
                break;
            case SlideDirection.Left:
                hiddenPosition.x -= hiddenOffset.x;
                break;
            case SlideDirection.Right:
                hiddenPosition.x += hiddenOffset.x;
                break;
        }

        _currentTween = _canvasGroup.DOFade(0f, 0.1f)
            .OnComplete(
                () =>
                {
                    _rectTransform.anchoredPosition = hiddenPosition;
                    _currentTween = null;
                    FreezeContent(false);
                    panelState = SidePanelState.Hiding;
                    StopAnimation();
                    onComplete?.Invoke();
                }
            );
    }

    public void SetVisibility(bool visible, bool instant = false)
    {
        if (visible)
        {
            if (instant)
                ShowInstant();
            else
                Show();
        }
        else
        {
            if (instant)
                HideInstant();
            else
                Hide();
        }
    }

    /// <summary>
    ///     禁用射线检测、禁用重力碰撞等
    /// </summary>
    /// <param name="freeze"></param>
    private void FreezeContent(bool freeze)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = !freeze; // 禁用交互
            _canvasGroup.interactable = !freeze; // 禁用按钮等
        }

        foreach (var col in GetComponentsInChildren<BoxCollider2D>())
        {
            col.enabled = !freeze;
        }

        foreach (var rb in GetComponentsInChildren<Rigidbody2D>())
        {
            if (rb.bodyType == RigidbodyType2D.Dynamic) rb.isKinematic = freeze;
            if (rb.isKinematic) rb.isKinematic = freeze;

            if (freeze && rb.bodyType is not RigidbodyType2D.Static)
            {
                rb.velocity = Vector3.zero; // 重置速度
                rb.angularVelocity = 0f; // 重置角速度
            }
        }
    }

    private void StartAnimation()
    {
        if (panelAnimator != null) panelAnimator.StartAnimation();
    }

    private void StopAnimation()
    {
        if (panelAnimator != null) panelAnimator.StopAnimation();
    }
}

public enum SidePanelState
{
    Showing,
    Hiding,
    Locked
}