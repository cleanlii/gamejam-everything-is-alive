using System.Collections.Generic;
using DG.Tweening;
using PackageGame.Global;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
///     可交互物体的基类，负责处理通用的拖拽、悬停等交互行为
/// </summary>
public abstract class ObjectInteraction : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform rectTransform;
    protected Canvas canvas;
    protected CanvasGroup canvasGroup;
    protected Vector2 originalPosition;
    protected Transform originalParent;
    protected Vector2 floatOffset;
    protected int rotateCount;
    protected int originalRotateCount;
    protected Vector2 mouseOffset;

    // 当前物体的类型与状态
    protected bool isDragging;
    protected Vector2Int[] currentShape;
    protected List<Vector2Int[]> rotatedShapes;

    // 动画参数
    protected float fadeInDuration = 0.3f;
    protected float fadeOutDuration = 0.3f;
    protected float scaleFactor = 1.1f;

    protected virtual void Awake()
    {
        // 获取基础组件
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = FindInParent<Canvas>(gameObject);
    }

    protected virtual void OnEnable()
    {
        // 子类可以在这里初始化输入事件等
    }

    protected virtual void OnDisable()
    {
        // 子类可以在这里取消订阅输入事件等
    }

    #region 接口实现

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (LevelStateController.Instance.IsAnyDragging())
            return;

        // 记录原始状态
        originalPosition = rectTransform.anchoredPosition;
        originalRotateCount = rotateCount;
        originalParent = rectTransform.parent;

        // 基础拖拽设置
        CursorController.Instance.SetOnDragCursor();
        LevelViewController.Instance.ShowRotateReminder();
        HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticFeedbackStyle.Medium);

        // 停止可能的动画，避免位置不同步
        rectTransform.DOKill();
        canvasGroup.DOKill();

        // 设置拖拽状态
        canvasGroup.blocksRaycasts = false;
        canvasGroup.DOFade(0.6f, fadeInDuration);
        rectTransform.DOScale(Vector3.one * scaleFactor, fadeInDuration).SetEase(Ease.OutQuad);

        // 标记为拖拽中
        isDragging = true;
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            // 实时更新物体位置跟随鼠标
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position,
                eventData.pressEventCamera, out pos);
            Vector2 rotatedOffset = transform.rotation * mouseOffset;
            transform.position = canvas.transform.TransformPoint(pos + floatOffset + rotatedOffset);

            // 高亮显示相关
            HighlightCells();
        }
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        // 重置状态
        CursorController.Instance.SetNormalCursor();
        LevelViewController.Instance.HideRotateReminder();

        // 停止可能的动画，避免位置不同步
        rectTransform.DOKill();
        canvasGroup.DOKill();
        rectTransform.DOScale(Vector3.one, fadeOutDuration).SetEase(Ease.OutElastic);
        canvasGroup.DOFade(1f, fadeOutDuration);
        canvasGroup.blocksRaycasts = true;

        // 根据当前拖放位置处理逻辑
        // HandleDropAction(eventData);

        // 清理高亮
        ClearHighlight();

        // 重置拖拽状态
        isDragging = false;

        // 触发触觉反馈
        HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticFeedbackStyle.Light);
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (!LevelStateController.Instance.IsAnyDragging())
        {
            CursorController.Instance.SetReadyToDragCursor();
            ShowHoverUI();
        }
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (!LevelStateController.Instance.IsAnyDragging())
        {
            CursorController.Instance.SetNormalCursor();
            HideHoverUI();
        }
    }

    #endregion

    #region 抽象方法

    /// <summary>
    ///     高亮可放置的目标格子
    /// </summary>
    protected abstract void HighlightCells();

    /// <summary>
    ///     清除高亮状态
    /// </summary>
    protected abstract void ClearHighlight();

    /// <summary>
    ///     显示悬停UI
    /// </summary>
    protected abstract void ShowHoverUI();

    /// <summary>
    ///     隐藏悬停UI
    /// </summary>
    protected abstract void HideHoverUI();

    #endregion

    #region 辅助方法

    /// <summary>
    ///     售出
    /// </summary>
    public virtual void Sold()
    {
    }

    /// <summary>
    ///     购买
    /// </summary>
    public virtual void Purchase(bool confirm)
    {
    }

    /// <summary>
    ///     查找父级组件
    /// </summary>
    protected T FindInParent<T>(GameObject child) where T : Component
    {
        var t = child.transform;
        while (t != null)
        {
            var component = t.GetComponent<T>();
            if (component != null) return component;
            t = t.parent;
        }

        return null;
    }

    public virtual void ResetPositionAtInventory()
    {
        // 如果没有找到合适的槽，则恢复位置
        // 1. 重置旋转状态
        if (rotateCount != originalRotateCount)
        {
            while (rotateCount != originalRotateCount)
            {
                rotateCount += 1;
                RotateObject();
            }
        }

        // 2. 回到原位
        // rectTransform.SetParent(rightPanelObjectLayer);
        // ObjUtils.SetParentAndLayer(gameObject, rightPanelObjectLayer, InventoryType.Truck);
        // rectTransform.anchoredPosition = originalPosition;
        // UpdateCells(lastGridPosition, currentShape, true);
    }

    public virtual void ResetPositionAtShop()
    {
    }

    /// <summary>
    ///     处理滚轮旋转
    /// </summary>
    protected virtual void OnScroll(InputAction.CallbackContext context)
    {
        if (!isDragging) return;

        var scrollValue = context.ReadValue<Vector2>().y;
        if (scrollValue > 0)
        {
            rotateCount += 1;
            RotateObject();
            HighlightCells();
        }
        else if (scrollValue < 0)
        {
            rotateCount -= 1;
            if (rotateCount < 0) rotateCount = 3;
            RotateObjectBackwards();
            HighlightCells();
        }
    }

    /// <summary>
    ///     旋转对象 - 顺时针
    /// </summary>
    protected virtual void RotateObject()
    {
        // 子类需要实现具体的旋转逻辑
    }

    /// <summary>
    ///     旋转对象 - 逆时针
    /// </summary>
    protected virtual void RotateObjectBackwards()
    {
        // 子类需要实现具体的旋转逻辑
    }

    /// <summary>
    ///     调整物品旋转角度
    /// </summary>
    protected void SnapObjectRotation()
    {
        var zRotation = rectTransform.eulerAngles.z;

        // 将角度转换到-180到180度范围内
        if (zRotation > 180)
            zRotation -= 360;

        // 自定义逻辑捕捉到0度，90度，180度，和-90度
        float snappedRotation;
        if (zRotation >= -45 && zRotation < 45)
            snappedRotation = 0;
        else if (zRotation >= 45 && zRotation < 135)
            snappedRotation = 90;
        else if (zRotation >= 135 || zRotation < -135)
            snappedRotation = 180;
        else
            snappedRotation = -90;

        Debug.Log("近似角度为：" + snappedRotation);

        switch (snappedRotation)
        {
            case 90:
                rotateCount = 1;
                rectTransform.rotation = Quaternion.Euler(0, 0, 90);
                rectTransform.pivot = new Vector2(1, 1);
                currentShape = rotatedShapes[0];
                break;
            case 180:
                rotateCount = 2;
                rectTransform.rotation = Quaternion.Euler(0, 0, 180);
                rectTransform.pivot = new Vector2(1, 0);
                currentShape = rotatedShapes[1];
                break;
            case -90:
                rotateCount = 3;
                rectTransform.rotation = Quaternion.Euler(0, 0, -90);
                rectTransform.pivot = new Vector2(0, 0);
                currentShape = rotatedShapes[2];
                break;
            case 0:
                rotateCount = 0;
                rectTransform.rotation = Quaternion.Euler(0, 0, 0);
                rectTransform.pivot = new Vector2(0, 1);
                currentShape = rotatedShapes[3];
                break;
        }
    }

    #endregion
}