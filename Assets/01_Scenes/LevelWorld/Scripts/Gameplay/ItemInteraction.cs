using System;
using System.Collections.Generic;
using DG.Tweening;
using PackageGame.Global;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemInteraction : ObjectInteraction
{
    // 物品数据
    public ItemData itemData;
    private ItemFrame _itemFrame;

    // 物理组件
    private Image _itemImage;
    private CompositeCollider2D _cl;
    private Rigidbody2D _rb;

    // 输入控制
    private PlayerInputControl _itemPlayActions;

    // 层级引用
    public RectTransform leftPanelRect;
    public Transform leftPanelItemLayer;
    private Transform _rightPanelItemLayer;
    private RectTransform _rightPanelRect;
    private Transform _rightPanelDraggingLayer;
    private Transform _leftPanelDraggingLayer;

    // 位置数据
    private Vector2Int _lastGridPosition;

    // 暂存区界面
    private RectTransform _cabinetPanelRect;
    private Transform _cabinetPanelItemLayer;

    // 商店界面
    private RectTransform _storeLeftPanel;
    private RectTransform _storeBannedArea;

    // 用于物品信息显示的事件
    public static event Action<ItemData> OnItemHovered;
    public static event Action OnItemUnhovered;

    // 物品管理器
    private ItemManager _itemManager;
    private PropManager _propManager;

    // 初始化相关
    private InventoryInitializer _inventoryInitializer;
    private Vector2 _middlePosition;

    // 音效和反馈
    private string _pickupSound = "SE_拿起物品";
    private string _dropSound = "小世界_SE_放下货物";

    protected override void Awake()
    {
        base.Awake();
        _itemPlayActions = new PlayerInputControl();
        _itemFrame = GetComponent<ItemFrame>();
    }

    protected override void OnEnable()
    {
        _itemPlayActions?.ItemPlay.Enable();
        if (_itemPlayActions != null)
            _itemPlayActions.ItemPlay.ScrollRotate.performed += OnScroll;
    }

    protected override void OnDisable()
    {
        _itemPlayActions?.ItemPlay.Disable();
        if (_itemPlayActions != null)
            _itemPlayActions.ItemPlay.ScrollRotate.performed -= OnScroll;
    }

    private void OnDestroy()
    {
        CleanupBeforeDestroy();
    }

    private void Update()
    {
        if (LevelStateController.Instance.IsAnyDragging() && _itemManager.pickingItem == gameObject)
        {
            if (_itemPlayActions.ItemPlay.Rotate.WasPerformedThisFrame())
            {
                HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticFeedbackStyle.Light);
                rotateCount += 1;
                RotateObject();
                HighlightCells();
            }
        }
    }

    /// <summary>
    ///     初始化物品
    /// </summary>
    public void Initialize(Item itemTemplate, GameObject obj, Image img, Vector2Int position, InventoryInitializer initializer)
    {
        itemData = new ItemData(itemTemplate, this, obj, img, position);
        _inventoryInitializer = initializer;
        _itemManager = LevelStateController.Instance.GetService<ItemManager>();
        _propManager = LevelStateController.Instance.GetService<PropManager>();
        _itemImage = img;
        _lastGridPosition = position;

        rotatedShapes = new List<Vector2Int[]>();

        SetItem();
    }

    /// <summary>
    ///     设置物品属性
    /// </summary>
    private void SetItem()
    {
        originalPosition = rectTransform.anchoredPosition;
        _lastGridPosition = itemData.position;
        _middlePosition = _inventoryInitializer.cells[3, 1].worldPosition;

        _rb = GetComponent<Rigidbody2D>();
        _cl = GetComponent<CompositeCollider2D>();

        floatOffset = new Vector2(-rectTransform.rect.width / 2, rectTransform.rect.height / 2);

        // 获取各个面板引用
        leftPanelItemLayer = _inventoryInitializer.transform.Find("ItemLayer");
        _rightPanelItemLayer = _inventoryInitializer.oppositeInitializer.transform.Find("ItemLayer");
        _rightPanelDraggingLayer = _inventoryInitializer.oppositeInitializer.transform.Find("DraggingLayer");
        _leftPanelDraggingLayer = _inventoryInitializer.transform.Find("DraggingLayer");
        leftPanelRect = leftPanelItemLayer.GetComponent<RectTransform>();
        _rightPanelRect = _rightPanelItemLayer.GetComponent<RectTransform>();

        // 初始化旋转形状数据
        var tempShape = RectUtils.RotateShape(itemData.shape);
        for (var i = 0; i < 4; i++)
        {
            rotatedShapes.Add(tempShape);
            tempShape = RectUtils.RotateShape(tempShape);
        }

        rotateCount = 0;
        itemData.shape = rotatedShapes[3];
        currentShape = rotatedShapes[3];

        // 更换默认Initializer引用
        ChangePanel();

        _dropSound = _itemManager.GetDropAudioName(itemData);
        _pickupSound = _itemManager.GetPickUpAudioName(itemData);
    }

    private void ChangePanel()
    {
        // ReSharper disable once SwapViaDeconstruction
        var temp = _inventoryInitializer;
        _inventoryInitializer = _inventoryInitializer.oppositeInitializer;
        _inventoryInitializer.oppositeInitializer = temp;
    }

    /// <summary>
    ///     清理资源
    /// </summary>
    private void CleanupBeforeDestroy()
    {
        if (_itemPlayActions == null) return;
        _itemPlayActions.ItemPlay.Disable();
        _itemPlayActions = null;
    }

    #region 重写基类方法

    /// <summary>
    ///     高亮可放置的目标单元格
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
            // Debug.Log("最近网格为：" + startPosition);

            var canPlace = CanPlaceItem(nearestSlot);

            foreach (var offset in currentShape)
            {
                var cellX = startPosition.x + offset.x;
                var cellY = startPosition.y + offset.y;
                // Debug.Log("获取网格：" + new Vector2Int(cellX, cellY));
                if (_inventoryInitializer.IsValidGrid(cellX, cellY))
                {
                    var cell = _inventoryInitializer.cells[cellX, cellY];
                    if (cell.slotObj != null && cell.blockObj != null)
                    {
                        _itemManager.highlightedCells.Add(cell);
                        var cellImage = cell.blockObj.GetComponent<Image>();
                        if (cellImage != null)
                            cellImage.color = canPlace ? _itemManager.highlightGreen : _itemManager.highlightRed; // 根据是否能放置高亮显示为绿色或红色
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
        foreach (var cell in _itemManager.highlightedCells)
        {
            if (cell.slotObj != null && cell.blockObj != null)
            {
                var cellImage = cell.blockObj.GetComponent<Image>();
                if (cellImage != null) cellImage.color = new Color32(1, 1, 1, 0); // 恢复透明
            }
        }

        _itemManager.highlightedCells.Clear();
    }

    /// <summary>
    ///     旋转物品（顺时针）
    /// </summary>
    protected override void RotateObject()
    {
        switch (rotateCount)
        {
            case 1:
                rectTransform.Rotate(0, 0, 90);
                rectTransform.pivot = new Vector2(1, 1);
                currentShape = rotatedShapes[0];
                break;
            case 2:
                rectTransform.Rotate(0, 0, 90);
                rectTransform.pivot = new Vector2(1, 0);
                currentShape = rotatedShapes[1];
                break;
            case 3:
                rectTransform.Rotate(0, 0, -270);
                rectTransform.pivot = new Vector2(0, 0);
                currentShape = rotatedShapes[2];
                break;
            case 4:
                rectTransform.Rotate(0, 0, 90);
                rectTransform.pivot = new Vector2(0, 1);
                currentShape = rotatedShapes[3];
                rotateCount = 0;
                break;
        }
    }

    /// <summary>
    ///     旋转物品（逆时针）
    /// </summary>
    protected override void RotateObjectBackwards()
    {
        switch (rotateCount)
        {
            case 1:
                rectTransform.Rotate(0, 0, -90);
                rectTransform.pivot = new Vector2(1, 1);
                itemData.shape = rotatedShapes[0];
                currentShape = rotatedShapes[0];
                break;
            case 2:
                rectTransform.Rotate(0, 0, -90);
                rectTransform.pivot = new Vector2(1, 0);
                itemData.shape = rotatedShapes[1];
                currentShape = rotatedShapes[1];
                break;
            case 3:
                rectTransform.Rotate(0, 0, 270);
                rectTransform.pivot = new Vector2(0, 0);
                itemData.shape = rotatedShapes[2];
                currentShape = rotatedShapes[2];
                break;
            case 0:
                rectTransform.Rotate(0, 0, -90);
                rectTransform.pivot = new Vector2(0, 1);
                itemData.shape = rotatedShapes[3];
                currentShape = rotatedShapes[3];
                break;
        }
    }

    #endregion

    #region 拖拽接口实现

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (!LevelStateController.Instance.IsAnyDragging())
        {
            CursorController.Instance.SetOnDragCursor();

            LevelViewController.Instance.ShowRotateReminder();

            HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticFeedbackStyle.Medium);
            AudioManager.PlaySound(_pickupSound, AudioType.SE, false);
            _itemManager.HideItemTag(itemData);

            _itemManager.pickingItem = gameObject;
            _itemManager.ShowDraggingInfo(itemData);

            originalPosition = rectTransform.anchoredPosition;
            originalRotateCount = rotateCount;
            originalParent = rectTransform.parent;

            // 停止可能的动画，避免位置不同步
            rectTransform.DOKill();
            canvasGroup.DOKill();

            canvasGroup.blocksRaycasts = false; // 使物品可穿透
            canvasGroup.DOFade(0.6f, 0.3f); // 透明
            rectTransform.DOScale(Vector3.one * 1.1f, 0.3f).SetEase(Ease.OutElastic); // 放大

            SetRigidbody(false);

            if (originalParent == _rightPanelItemLayer)
            {
                UpdateCells(_lastGridPosition, currentShape, false);
                ObjUtils.SetParentAndLayer(gameObject, _rightPanelDraggingLayer, InventoryType.Truck);
            }
            else if (originalParent == leftPanelItemLayer)
            {
                // 吸附物品的旋转角度
                SnapObjectRotation();
                ObjUtils.SetParentAndLayer(gameObject, _rightPanelDraggingLayer, InventoryType.Truck);
            }
        }
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (_itemManager.pickingItem == gameObject)
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
        if (_itemManager.pickingItem == gameObject)
        {
            CursorController.Instance.SetNormalCursor();
            LevelViewController.Instance.HideRotateReminder();
            _itemFrame.HideInstant();

            AudioManager.PlaySound(_dropSound, AudioType.SE, false);
            _itemManager.pickingItem = null;
            _itemManager.HideDraggingInfo();

            // 停止可能的动画，避免位置不同步
            rectTransform.DOKill();
            canvasGroup.DOKill();
            rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutElastic);
            canvasGroup.DOFade(1f, 0.3f);
            canvasGroup.blocksRaycasts = true;

            if (originalParent == leftPanelItemLayer) HandleLeftPanelDropInTask();
            if (originalParent == _rightPanelItemLayer) HandleRightPanelDropInTask();

            HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticFeedbackStyle.Light);

            // 清除高亮显示
            ClearHighlight();

            _itemManager.HighlightUntaggedItem(itemData);
            _itemManager.CheckItemRelationships();
        }
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!LevelStateController.Instance.IsAnyDragging()) ShowHoverUI();
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (!LevelStateController.Instance.IsAnyDragging()) HideHoverUI();
    }

    protected override void ShowHoverUI()
    {
        OnItemHovered?.Invoke(itemData);
        CursorController.Instance.SetReadyToDragCursor();
        _itemFrame.ShowFrame();
    }

    protected override void HideHoverUI()
    {
        OnItemUnhovered?.Invoke();
        CursorController.Instance.SetNormalCursor();
        _itemFrame.HideFrame();
    }

    #endregion

    #region 滚轮旋转处理

    /// <summary>
    ///     处理滚轮旋转事件
    /// </summary>
    protected override void OnScroll(InputAction.CallbackContext context)
    {
        if (LevelStateController.Instance.IsAnyDragging() && _itemManager.pickingItem == gameObject)
        {
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
    }

    #endregion

    #region 物理设置

    private void SetCollider(bool isActive)
    {
        // cl.enabled = isActive;
        _cl.isTrigger = isActive;
        // if (isActive) cl.GenerateGeometry();
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
        // SetRigidbody(status);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     找到最近的放置槽
    /// </summary>
    private CellData FindNearestSlot()
    {
        CellData nearestSlot = null;
        var minDistance = float.MaxValue;

        Vector3 itemPosition = rectTransform.anchoredPosition;

        for (var y = 0; y < _inventoryInitializer.cells.GetLength(1); y++)
        {
            for (var x = 0; x < _inventoryInitializer.cells.GetLength(0); x++)
            {
                var cell = _inventoryInitializer.cells[x, y];
                var distance = Vector3.Distance(itemPosition, cell.worldPosition);
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
    ///     判断是否可以放置物品
    /// </summary>
    private bool CanPlaceItem(CellData slot)
    {
        var startPosition = slot.gridPosition;

        foreach (var offset in currentShape)
        {
            var cellX = startPosition.x + offset.x;
            var cellY = startPosition.y + offset.y;
            if (!_inventoryInitializer.IsValidGrid(cellX, cellY) ||
                _inventoryInitializer.cells[cellX, cellY].GetCellState() != CellData.State.Packable ||
                !_inventoryInitializer.cells[cellX, cellY].slotObj) return false;
        }

        return true;
    }

    /// <summary>
    ///     更新单元格状态
    /// </summary>
    private void UpdateCells(Vector2Int gridPosition, Vector2Int[] shape, bool isOccupied)
    {
        // 按照形状更新对应网格的状态
        foreach (var offset in shape)
        {
            var cellX = gridPosition.x + offset.x;
            var cellY = gridPosition.y + offset.y;
            if (_inventoryInitializer.IsValidGrid(cellX, cellY))
            {
                var cell = _inventoryInitializer.cells[cellX, cellY];

                if (isOccupied)
                {
                    cell.InnerItem = itemData;
                    cell.UpdateCellState();
                    itemData.isPlaced = true;

                    if (cell.innerBp.holdingItems.Contains(itemData)) continue;

                    cell.innerBp.holdingItems.Add(itemData);
                    _itemManager.StartSingleItemPutDown(gameObject);
                    Debug.Log("已放置货物：" + itemData.name + "，放置背包：" + cell.innerBp.name);
                }
                else
                {
                    cell.InnerItem = null;
                    cell.UpdateCellState();
                    itemData.isPlaced = false;
                    if (cell.innerBp.holdingItems != null && cell.innerBp.holdingItems.Contains(itemData)) cell.innerBp.holdingItems.Remove(itemData);
                    Debug.Log("已取消放置货物：" + itemData.name);
                    _itemManager.ResetItemRelationshipEffects(itemData);
                }
            }
        }
    }

    #endregion

    #region 任务点内交互逻辑

    private void HandleLeftPanelDropInTask()
    {
        // 左 => 右
        // 1. 有空位，放置成功
        // 2. 无空位，刷新掉落 -> 返回原位
        if (RectUtils.IsRectangleInside(_rightPanelRect, rectTransform))
        {
            var nearestSlot = FindNearestSlot();
            if (nearestSlot != null && CanPlaceItem(nearestSlot))
            {
                // 放入
                SetColliderAndRigidbody(false);
                // rectTransform.SetParent(_rightPanelItemLayer);
                ObjUtils.SetParentAndLayer(gameObject, _rightPanelItemLayer, InventoryType.Truck);

                Debug.Log("更新状态位置：" + nearestSlot.gridPosition);
                _lastGridPosition = nearestSlot.gridPosition;
                // 将物品放置在相应位置
                rectTransform.anchoredPosition = nearestSlot.worldPosition;
                itemData.position = nearestSlot.gridPosition;
                UpdateCells(nearestSlot.gridPosition, currentShape, true);
            }
            else
            {
                // 无法放入
                // 来自左面板，刷新掉落 -> 返回原位
                SetColliderAndRigidbody(true);
                ObjUtils.SetParentAndLayer(gameObject, leftPanelItemLayer, InventoryType.Pool);
                rectTransform.anchoredPosition = originalPosition;
            }
        }

        // 左 => 左
        // 左 => 其他任何非法位置 -> 返回原位
        else
        {
            SetColliderAndRigidbody(true);
            ObjUtils.SetParentAndLayer(gameObject, leftPanelItemLayer, InventoryType.Pool);
            if (!RectUtils.IsRectangleInside(leftPanelRect, rectTransform))
            {
                rectTransform.anchoredPosition = originalPosition;
                // rectTransform.anchoredPosition = _middlePosition;
            }
        }
    }

    private void HandleRightPanelDropInTask()
    {
        // 右 => 右
        // 1. 有空位，放置成功
        // 2. 无空位，回到原位
        if (RectUtils.IsRectangleInside(_rightPanelRect, rectTransform))
        {
            var nearestSlot = FindNearestSlot();
            if (nearestSlot != null && CanPlaceItem(nearestSlot))
            {
                SetColliderAndRigidbody(false);
                // rectTransform.SetParent(_rightPanelItemLayer);
                ObjUtils.SetParentAndLayer(gameObject, _rightPanelItemLayer, InventoryType.Truck);

                // Debug.Log("更新状态位置：" + nearestSlot.gridPosition);
                _lastGridPosition = nearestSlot.gridPosition;
                // 将物品放置在相应位置
                rectTransform.anchoredPosition = nearestSlot.worldPosition;
                itemData.position = nearestSlot.gridPosition;
                UpdateCells(nearestSlot.gridPosition, currentShape, true);
            }
            else
            {
                // 如果没有找到合适的槽，则恢复位置
                ResetPositionAtInventory();
            }
        }

        // 右 => 其他任何非法位置
        // 右 => 左
        // 直接从指尖掉落
        // 直接刷新掉落
        else
        {
            ObjUtils.SetParentAndLayer(gameObject, leftPanelItemLayer, InventoryType.Pool);
            if (!RectUtils.IsRectangleInside(leftPanelRect, rectTransform))
            {
                // rectTransform.anchoredPosition = originalPosition;
                if (!_itemManager.isBackpackMoved) rectTransform.anchoredPosition = itemData.spawnPoint.GetPosition();
                else
                {
                    var sizeX = _inventoryInitializer.inventoryInfo.size.x;
                    var sizeY = _inventoryInitializer.inventoryInfo.size.y;
                    rectTransform.anchoredPosition = _inventoryInitializer.cells[sizeX - 1, sizeY - 1].worldPosition + new Vector3(450f, 300f, 0f);
                }
            }

            SetColliderAndRigidbody(true);
            UpdateCells(_lastGridPosition, currentShape, false);
        }
    }

    public override void ResetPositionAtInventory()
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

        // 2. 来自右面板，回到原位即可
        // rectTransform.SetParent(_rightPanelItemLayer);
        ObjUtils.SetParentAndLayer(gameObject, _rightPanelItemLayer, InventoryType.Truck);
        rectTransform.DOKill();
        rectTransform.localScale = Vector3.one;
        rectTransform.anchoredPosition = originalPosition;
        gameObject.SetActive(true);
        UpdateCells(_lastGridPosition, currentShape, true);
    }

    #endregion
}