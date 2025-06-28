using System;
using System.Collections.Generic;
using DG.Tweening;
using PackageGame.Global;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BackpackInteraction : ObjectInteraction
{
    // 背包数据
    public List<GameObject> slotObj;
    public BackpackData backpackData;

    // 物理组件
    private PolygonCollider2D _cl;
    private Rigidbody2D _rb;

    // 输入控制
    private PlayerInputControl _bpPlayActions;

    // 层级引用
    public Transform originalTransform;
    public InventoryInitializer inventoryInitializer;

    // 车厢界面
    private Transform _rightPanelInventoryLayer;
    private Transform _leftPanelInventoryLayer;
    private Transform _rightPanelGridLayer;
    private RectTransform _rightPanelRect;
    private Transform _rightPanelDraggingLayer;

    private Transform _originalParent;

    // 形状数据
    private Vector2Int _lastGridPosition;

    // 暂存区界面
    private RectTransform _cabinetPanel;
    private Transform _cabinetPanelInventoryLayer;

    // 商店界面
    private RectTransform _storeLeftPanel;
    private RectTransform _storeSoldArea;

    // 用于背包信息显示的事件
    public static event Action<BackpackData> OnBpHovered;
    public static event Action OnBpUnhovered;

    // 物品管理器
    private InventoryManager _inventoryManager;
    private ItemManager _itemManager;

    // 音效和反馈
    private readonly string _pickupSound = "小世界_SE_拿起货箱";
    private readonly string _dropSound = "小世界_SE_放下货箱";

    protected override void Awake()
    {
        base.Awake();
        _bpPlayActions = new PlayerInputControl();
    }

    protected override void OnEnable()
    {
        _bpPlayActions?.ItemPlay.Enable();
        if (_bpPlayActions != null)
            _bpPlayActions.ItemPlay.ScrollRotate.performed += OnScroll;
    }

    protected override void OnDisable()
    {
        _bpPlayActions?.ItemPlay.Disable();
        if (_bpPlayActions != null)
            _bpPlayActions.ItemPlay.ScrollRotate.performed -= OnScroll;
    }

    private void Update()
    {
        if (LevelStateController.Instance.IsAnyDragging() && _inventoryManager.pickingBp == gameObject)
        {
            if (_bpPlayActions.ItemPlay.Rotate.WasPerformedThisFrame())
            {
                HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticFeedbackStyle.Light);
                rotateCount += 1;
                RotateObject();
                HighlightCells();
            }
        }
    }

    protected override void OnScroll(InputAction.CallbackContext context)
    {
        if (LevelStateController.Instance.IsAnyDragging() && _inventoryManager.pickingBp == gameObject)
        {
            var scrollValue = context.ReadValue<Vector2>().y; // 获取 y 轴滚动值
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
    }

    public void Initialize(Backpack bp, GameObject obj, Vector2Int position, InventoryInitializer initializer)
    {
        backpackData = new BackpackData(bp, this, obj, position);
        inventoryInitializer = initializer;
        _inventoryManager = LevelStateController.Instance.GetService<InventoryManager>();
        _itemManager = LevelStateController.Instance.GetService<ItemManager>();
        originalTransform = inventoryInitializer.transform.Find("InventoryLayer");
        _lastGridPosition = backpackData.position;

        rotatedShapes = new List<Vector2Int[]>();
    }

    public void Initialize(Backpack bp, GameObject obj, Vector2Int position, InventoryManager manager)
    {
        backpackData = new BackpackData(bp, this, obj, position);
        inventoryInitializer = manager.defaultInitializer;
        _inventoryManager = LevelStateController.Instance.GetService<InventoryManager>();
        _itemManager = LevelStateController.Instance.GetService<ItemManager>();
        originalTransform = inventoryInitializer.transform.Find("InventoryLayer");
        _lastGridPosition = backpackData.position;

        rotatedShapes = new List<Vector2Int[]>();
    }

    public void SetBackpack()
    {
        originalPosition = rectTransform.anchoredPosition;
        floatOffset = new Vector2(-rectTransform.rect.width / 2, rectTransform.rect.height / 2);

        _rb = GetComponent<Rigidbody2D>();
        _cl = GetComponent<PolygonCollider2D>();

        // 获取各个面板引用
        _rightPanelInventoryLayer = inventoryInitializer.transform.Find("InventoryLayer");
        _leftPanelInventoryLayer = inventoryInitializer.oppositeInitializer.transform.Find("InventoryLayer");
        _rightPanelGridLayer = inventoryInitializer.transform.Find("GridLayer");
        _rightPanelDraggingLayer = inventoryInitializer.transform.Find("DraggingLayer");
        _rightPanelRect = _rightPanelInventoryLayer.GetComponent<RectTransform>();
        
        // 初始化旋转形状数据
        var tempShape = RectUtils.RotateShape(backpackData.shape);
        rotatedShapes = new List<Vector2Int[]>();
        for (var i = 0; i < 4; i++)
        {
            rotatedShapes.Add(tempShape);
            tempShape = RectUtils.RotateShape(tempShape);
        }

        rotateCount = 0;
        backpackData.shape = rotatedShapes[3];
        currentShape = rotatedShapes[3];

        SetColliderAndRigidbody(false);
    }

    #region 重写基类方法

    /// <summary>
    ///     高亮目标单元格
    /// </summary>
    protected override void HighlightCells()
    {
        // 清除之前的高亮显示
        ClearHighlight();

        if (!RectUtils.IsRectangleInside(_rightPanelRect, rectTransform)) return;

        var nearestSlot = FindNearestSlot();
        if (nearestSlot != null)
        {
            var startPosition = nearestSlot.gridPosition;
            var canPlace = CanPlaceBp(nearestSlot);

            foreach (var offset in currentShape)
            {
                var cellX = startPosition.x + offset.x;
                var cellY = startPosition.y + offset.y;

                if (inventoryInitializer.IsValidGrid(cellX, cellY))
                {
                    var cell = inventoryInitializer.cells[cellX, cellY];
                    if (cell.gridObj != null)
                    {
                        _inventoryManager.highlightedCells.Add(cell);
                        var cellImage = cell.gridObj.GetComponent<Image>();
                        if (cellImage != null)
                            cellImage.color = canPlace ? _inventoryManager.highlightGreen : _inventoryManager.highlightRed;
                    }
                }
            }
        }
    }

    /// <summary>
    ///     清除高亮显示
    /// </summary>
    protected override void ClearHighlight()
    {
        foreach (var cell in _inventoryManager.highlightedCells)
        {
            if (cell.gridObj != null)
            {
                var cellImage = cell.gridObj.GetComponent<Image>();
                if (cellImage != null) cellImage.color = _inventoryManager.gridColor;
            }
        }

        _inventoryManager.highlightedCells.Clear();
    }

    /// <summary>
    ///     旋转对象
    /// </summary>
    protected override void RotateObject()
    {
        switch (rotateCount)
        {
            case 1:
                rectTransform.Rotate(0, 0, 90);
                rectTransform.pivot = new Vector2(1, 1);
                currentShape = rotatedShapes[0];
                _cl.offset = new Vector2(-rectTransform.rect.width, 0);
                break;
            case 2:
                rectTransform.Rotate(0, 0, 90);
                rectTransform.pivot = new Vector2(1, 0);
                currentShape = rotatedShapes[1];
                _cl.offset = new Vector2(-rectTransform.rect.width, rectTransform.rect.height);
                break;
            case 3:
                rectTransform.Rotate(0, 0, -270);
                rectTransform.pivot = new Vector2(0, 0);
                currentShape = rotatedShapes[2];
                _cl.offset = new Vector2(0, rectTransform.rect.height);
                break;
            case 4:
                rectTransform.Rotate(0, 0, 90);
                rectTransform.pivot = new Vector2(0, 1);
                currentShape = rotatedShapes[3];
                _cl.offset = new Vector2(0, 0);
                rotateCount = 0;
                break;
        }
    }

    /// <summary>
    ///     反向旋转对象
    /// </summary>
    protected override void RotateObjectBackwards()
    {
        switch (rotateCount)
        {
            case 1:
                rectTransform.Rotate(0, 0, -90);
                rectTransform.pivot = new Vector2(1, 1);
                backpackData.shape = rotatedShapes[0];
                currentShape = rotatedShapes[0];
                _cl.offset = new Vector2(-rectTransform.rect.width, 0);
                break;
            case 2:
                rectTransform.Rotate(0, 0, -90);
                rectTransform.pivot = new Vector2(1, 0);
                backpackData.shape = rotatedShapes[1];
                currentShape = rotatedShapes[1];
                _cl.offset = new Vector2(-rectTransform.rect.width, rectTransform.rect.height);
                break;
            case 3:
                rectTransform.Rotate(0, 0, 270);
                rectTransform.pivot = new Vector2(0, 0);
                backpackData.shape = rotatedShapes[2];
                currentShape = rotatedShapes[2];
                _cl.offset = new Vector2(0, rectTransform.rect.height);
                break;
            case 0:
                rectTransform.Rotate(0, 0, -90);
                rectTransform.pivot = new Vector2(0, 1);
                backpackData.shape = rotatedShapes[3];
                currentShape = rotatedShapes[3];
                _cl.offset = new Vector2(0, 0);
                break;
        }
    }

    #endregion

    #region 接口实现

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (!LevelStateController.Instance.IsAnyDragging())
        {
            HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticFeedbackStyle.Medium);
            AudioManager.PlaySound(_pickupSound, AudioType.SE, false);

            // 检测货箱中是否有东西
            if (backpackData.holdingItems.Count == 0 && backpackData.holdingProps.Count == 0 && backpackData.holdingBuffs.Count == 0)
            {
                LevelViewController.Instance.ShowRotateReminder();

                CursorController.Instance.SetOnDragCursor();

                _inventoryManager.pickingBp = gameObject;
                _inventoryManager.ShowDraggingInfo(backpackData);

                originalPosition = rectTransform.anchoredPosition;
                originalRotateCount = rotateCount;
                _originalParent = rectTransform.parent;

                // 停止可能的动画，避免位置不同步
                rectTransform.DOKill();
                canvasGroup.DOKill();

                canvasGroup.blocksRaycasts = false; // 使物品可穿透
                canvasGroup.DOFade(0.6f, 0.3f); // 透明
                rectTransform.DOScale(Vector3.one * 1.05f, 0.3f).SetEase(Ease.OutQuad); // 放大

                _rightPanelGridLayer.gameObject.SetActive(true);

                SetColliderAndRigidbody(false);
                
                rectTransform.SetParent(_rightPanelDraggingLayer);
                UpdateCells(_lastGridPosition, currentShape, false);
                // ObjUtils.SetSiblingOrder(_rightPanelInventoryLayer.parent.gameObject, _leftPanelInventoryLayer.parent.gameObject);
                
                // FloatAboveMouse(eventData);
                // 此时还不启动拖拽动画，而是在 OnDrag 中检查距离
            }
            else
                Debug.LogWarning("背包中存在货物，无法移动背包！");
        }
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (_inventoryManager.pickingBp == gameObject)
        {
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position,
                eventData.pressEventCamera, out pos);
            Vector2 rotatedOffset = transform.rotation * mouseOffset;
            transform.position = canvas.transform.TransformPoint(pos + floatOffset + rotatedOffset);

            HighlightCells();
        }
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (_inventoryManager.pickingBp == gameObject)
        {
            CursorController.Instance.SetNormalCursor();
            LevelViewController.Instance.HideRotateReminder();

            AudioManager.PlaySound(_dropSound, AudioType.SE, false);
            _inventoryManager.pickingBp = null;
            _inventoryManager.HideDraggingInfo();

            // 停止可能的动画，避免位置不同步
            rectTransform.DOKill();
            canvasGroup.DOKill();
            rectTransform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBounce); // 弹跳缓动效果，模拟沉重物体的惯性
            canvasGroup.DOFade(1f, 0.5f); // 延长透明度恢复时间
            canvasGroup.blocksRaycasts = true;

            _rightPanelGridLayer.gameObject.SetActive(false);
            
            if (_originalParent == _rightPanelInventoryLayer) HandleRightPanelDropInTask();

            HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticFeedbackStyle.Light);

            // 清除高亮显示
            ClearHighlight();

            _itemManager.UpdateInnerItem();
        }

    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!LevelStateController.Instance.IsAnyDragging())
        {
            if (backpackData.holdingItems.Count == 0 && backpackData.holdingProps.Count == 0 && backpackData.holdingBuffs.Count == 0)
                ShowHoverUI();
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (!LevelStateController.Instance.IsAnyDragging())
        {
            if (backpackData.holdingItems.Count == 0 && backpackData.holdingProps.Count == 0 && backpackData.holdingBuffs.Count == 0)
                HideHoverUI();
        }
    }

    protected override void ShowHoverUI()
    {
        CursorController.Instance.SetReadyToDragCursor();
        OnBpHovered?.Invoke(backpackData);
    }

    protected override void HideHoverUI()
    {
        CursorController.Instance.SetNormalCursor();
        OnBpUnhovered?.Invoke();
    }

    #endregion

    private void SetCollider(bool isActive)
    {
        _cl.enabled = isActive;
    }

    private void SetRigidbody(bool isActive)
    {
        if (isActive)
            _rb.bodyType = RigidbodyType2D.Dynamic;
        else
            _rb.bodyType = RigidbodyType2D.Static;
    }

    private void SetColliderAndRigidbody(bool status)
    {
        // SetCollider(status);
        SetRigidbody(status);
    }

    #region 辅助方法

    /// <summary>
    ///     更新旋转后的槽位
    /// </summary>
    private void UpdateRotatedSlot(CellData cell, Vector2Int offset)
    {
        var tolerance = 50f;
        foreach (var slot in slotObj)
        {
            if (Vector3.Distance(slot.transform.position, cell.gridObj.transform.position) < tolerance)
            {
                slot.name = $"Slot_{offset.x}_{offset.y}";
                cell.slotObj = slot;
            }
        }
    }

    public void UpdateInnerCells()
    {
        foreach (var offset in currentShape)
        {
            var cellX = backpackData.position.x + offset.x;
            var cellY = backpackData.position.y + offset.y;

            inventoryInitializer.cells[cellX, cellY].UpdateCellState();
        }
    }

    /// <summary>
    ///     更新单元格状态
    /// </summary>
    private void UpdateCells(Vector2Int gridPosition, Vector2Int[] shape, bool isPackable)
    {
        // 按照形状更新对应网格的状态
        foreach (var offset in shape)
        {
            var cellX = gridPosition.x + offset.x;
            var cellY = gridPosition.y + offset.y;
            if (inventoryInitializer.IsValidGrid(cellX, cellY))
            {
                var cell = inventoryInitializer.cells[cellX, cellY];
                if (isPackable)
                {
                    cell.innerBp = backpackData;
                    UpdateRotatedSlot(cell, offset);
                    cell.UpdateCellState();
                    backpackData.isPlaced = true;
                    if (!_inventoryManager.backpacks.Contains(backpackData)) _inventoryManager.backpacks.Add(backpackData);
                }
                else
                {
                    cell.slotObj = null;
                    cell.innerBp = null;
                    cell.UpdateCellState();
                    backpackData.isPlaced = false;
                    if (_inventoryManager.backpacks.Contains(backpackData)) _inventoryManager.backpacks.Remove(backpackData);
                }
            }
        }
    }

    /// <summary>
    ///     找到最近的槽位
    /// </summary>
    private CellData FindNearestSlot()
    {
        CellData nearestSlot = null;
        var minDistance = float.MaxValue;

        Vector3 bpPosition = rectTransform.anchoredPosition;

        for (var y = 0; y < inventoryInitializer.cells.GetLength(1); y++)
        {
            for (var x = 0; x < inventoryInitializer.cells.GetLength(0); x++)
            {
                var cell = inventoryInitializer.cells[x, y];
                var distance = Vector3.Distance(bpPosition, cell.worldPosition);
                if (distance < minDistance && cell.gridObj)
                {
                    minDistance = distance;
                    nearestSlot = cell;
                }
            }
        }

        return nearestSlot;
    }

    /// <summary>
    ///     判断是否可以放置背包
    /// </summary>
    private bool CanPlaceBp(CellData slot)
    {
        var startPosition = slot.gridPosition;

        foreach (var offset in currentShape)
        {
            var cellX = startPosition.x + offset.x;
            var cellY = startPosition.y + offset.y;
            if (!inventoryInitializer.IsValidGrid(cellX, cellY) ||
                inventoryInitializer.cells[cellX, cellY].GetCellState() != CellData.State.Empty ||
                !inventoryInitializer.cells[cellX, cellY].gridObj) return false;
        }

        return true;
    }

    #endregion

    #region 暂存区交互逻辑

    private void HandleRightPanelDropInPlan()
    {
        // 车厢区 => 车厢区
        // 1. 有空位，放置成功
        // 2. 无空位，回到原位
        if (RectUtils.IsRectangleInside(_rightPanelRect, rectTransform))
        {
            var nearestSlot = FindNearestSlot();
            if (nearestSlot != null && CanPlaceBp(nearestSlot))
            {
                SetColliderAndRigidbody(false);
                rectTransform.SetParent(_rightPanelInventoryLayer);

                // Debug.Log("更新状态位置：" + nearestSlot.gridPosition);
                _lastGridPosition = nearestSlot.gridPosition;
                // 将物品放置在相应位置
                rectTransform.anchoredPosition = nearestSlot.worldPosition;
                backpackData.position = nearestSlot.gridPosition;
                UpdateCells(nearestSlot.gridPosition, currentShape, true);
            }
            else
            {
                // 如果没有找到合适的槽，则恢复位置
                ResetPositionAtInventory();
            }
        }
        else
        {
            // 其他任何位置，回到原位
            if (rotateCount != originalRotateCount)
            {
                while (rotateCount != originalRotateCount)
                {
                    rotateCount += 1;
                    RotateObject();
                }
            }

            // 停止可能未完成的动画
            rectTransform.DOKill();
            rectTransform.SetParent(_rightPanelInventoryLayer);
            rectTransform.anchoredPosition = originalPosition;
            UpdateCells(_lastGridPosition, currentShape, true);
        }
    }

    #endregion

    #region 任务点交互逻辑

    private void HandleRightPanelDropInTask()
    {
        // 右 => 右
        // 1. 有空位，放置成功
        // 2. 无空位，回到原位
        if (RectUtils.IsRectangleInside(_rightPanelRect, rectTransform))
        {
            var nearestSlot = FindNearestSlot();
            if (nearestSlot != null && CanPlaceBp(nearestSlot))
            {
                SetColliderAndRigidbody(false);
                rectTransform.SetParent(_rightPanelInventoryLayer);

                // Debug.Log("更新状态位置：" + nearestSlot.gridPosition);
                _lastGridPosition = nearestSlot.gridPosition;
                // 将物品放置在相应位置
                rectTransform.anchoredPosition = nearestSlot.worldPosition;
                backpackData.position = nearestSlot.gridPosition;
                UpdateCells(nearestSlot.gridPosition, currentShape, true);
            }
            else
            {
                // 如果没有找到合适的槽，则恢复位置
                ResetPositionAtInventory();
            }
        }
        else
        {
            // 其他任何位置，回到原位
            if (rotateCount != originalRotateCount)
            {
                while (rotateCount != originalRotateCount)
                {
                    rotateCount += 1;
                    RotateObject();
                }
            }

            // 停止可能未完成的动画
            rectTransform.DOKill();
            rectTransform.SetParent(_rightPanelInventoryLayer);
            rectTransform.anchoredPosition = originalPosition;
            UpdateCells(_lastGridPosition, currentShape, true);
        }

        // // 右 => 其他任何非法位置
        // // 右 => 左
        // // 直接从指尖掉落
        // // 直接刷新掉落
        // else
        // {
        //     rectTransform.SetParent(leftPanelItemLayer);
        //     if (!RectUtils.IsRectangleInside(leftPanelRect, rectTransform))
        //     {
        //         // rectTransform.anchoredPosition = originalPosition;
        //         rectTransform.anchoredPosition = middlePosition;
        //     }
        //     SetColliderAndRigidbody(true);
        //     UpdateCells(lastGridPosition, currentShape, false);
        // }
    }

    #endregion
}