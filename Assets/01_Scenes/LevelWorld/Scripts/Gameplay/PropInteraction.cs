using System.Collections.Generic;
using cfg.level;
using DG.Tweening;
using PackageGame.Global;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PropInteraction : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler,
    IPointerExitHandler
{
    public PropData propData;

    private PlayerInputControl _propPlayActions;

    private ItemManager _itemManager;
    private PropManager _propManager;
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    private Vector2 _originalPosition;
    private Vector2 _middlePosition;
    private Transform _originalTransform;
    private InventoryInitializer _inventoryInitializer;
    private Vector2Int[] _currentShape;
    private PropShapeDictionary _currentDic;
    private Vector2Int _lastGridPosition;
    private Vector2 _floatOffset;
    private int _rotateCount;
    private int _originalRotateCount;

    private Vector2 _mouseOffset;
    private Rigidbody2D _rb;
    private PolygonCollider2D _cl;
    private Material _material;

    // 暂存区界面
    private RectTransform _cabinetPanelRect;
    private Transform _cabinetPanelItemLayer;

    // 收货界面
    private Transform _leftPanelItemLayer;
    private Transform _rightPanelItemLayer;
    private RectTransform _leftPanelRect;
    private RectTransform _rightPanelRect; // 本体车厢
    private Transform _leftPanelDraggingLayer;
    private Transform _rightPanelDraggingLayer;

    // 商店界面
    private RectTransform _storeLeftPanel;
    private RectTransform _storeSoldArea;

    private Transform _originalParent;

    private Coroutine _floatCoroutine;
    private List<Vector2Int[]> _rotatedShapes;
    private List<PropShapeDictionary> _rotatedDictionaries;

    // private void Awake()
    // {
    //     _rotatedShapes = new List<Vector2Int[]>();
    //     _rotatedDictionaries = new List<PropShapeDictionary>();
    //     _propPlayActions = new PlayerInputControl();
    // }

    private void Update()
    {
        // if (propPlayActions.ItemPlay.ScrollRotate.WasPerformedThisFrame())
        // {
        //     Debug.Log("检测到滚轮！");
        // }

        if (LevelStateController.Instance.IsAnyDragging() && _itemManager.pickingItem == gameObject)
        {
            if (_propPlayActions.ItemPlay.Rotate.WasPerformedThisFrame())
            {
                HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticFeedbackStyle.Light);
                _rotateCount += 1;
                RotateProp();
                HighlightCells();
            }
        }
    }

    private void OnScroll(InputAction.CallbackContext context)
    {
        if (LevelStateController.Instance.IsAnyDragging() && _itemManager.pickingItem == gameObject)
        {
            var scrollValue = context.ReadValue<Vector2>().y; // 获取 y 轴滚动值
            if (scrollValue > 0)
            {
                // Debug.Log("Scroll Up (Button Pressed)");
                _rotateCount += 1;
                RotateProp();
                HighlightCells();
                // 模拟按下上方向按钮
            }
            else if (scrollValue < 0)
            {
                // Debug.Log("Scroll Down (Button Pressed)");
                _rotateCount -= 1;
                if (_rotateCount < 0) _rotateCount = 3;
                RotatePropBackwards();
                HighlightCells();
                // 模拟按下下方向按钮
            }
        }
    }

    private void OnEnable()
    {
        _propPlayActions?.ItemPlay.Enable();
        if (_propPlayActions != null) _propPlayActions.ItemPlay.ScrollRotate.performed += OnScroll;
    }

    private void OnDisable()
    {
        _propPlayActions?.ItemPlay.Disable();
        if (_propPlayActions != null) _propPlayActions.ItemPlay.ScrollRotate.performed -= OnScroll;
    }

    private void OnDestroy()
    {
        CleanupBeforeDestroy();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!LevelStateController.Instance.IsAnyDragging())
            CursorController.Instance.SetReadyToDragCursor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!LevelStateController.Instance.IsAnyDragging()) CursorController.Instance.SetNormalCursor();
    }

    public void SetPropItem()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvas = FindInParent<Canvas>(gameObject);
        _originalPosition = _rectTransform.anchoredPosition;

        // 默认初始位置总是为商店
        _propManager = LevelStateController.Instance.GetService<PropManager>();
        _inventoryInitializer = LevelStateController.Instance.GetDefaultInventoryInit();
        _middlePosition = _inventoryInitializer.oppositeInitializer.cells[3, 0].worldPosition;

        // _floatOffset = new Vector2(-_rectTransform.rect.width / 2, _rectTransform.rect.height + _inventoryInitializer.gridSize / 2);
        _floatOffset = new Vector2(-_rectTransform.rect.width / 2, _rectTransform.rect.height / 2);

        _rb = GetComponent<Rigidbody2D>();
        _cl = GetComponent<PolygonCollider2D>();
        _material = GetComponent<Image>().material;

        // 获取收货界面
        // 默认propItem附属于rightpanel
        _leftPanelItemLayer = _inventoryInitializer.oppositeInitializer.transform.Find("ItemLayer");
        _leftPanelDraggingLayer = _inventoryInitializer.oppositeInitializer.transform.Find("DraggingLayer");
        _rightPanelItemLayer = _inventoryInitializer.transform.Find("ItemLayer");
        _rightPanelDraggingLayer = _inventoryInitializer.transform.Find("DraggingLayer");
        _leftPanelRect = _leftPanelItemLayer.GetComponent<RectTransform>();
        _rightPanelRect = _rightPanelItemLayer.GetComponent<RectTransform>();

        var tempShape
            = RectUtils.RotateShape(propData.shape);

        for (var i = 0; i < 4; i++)
        {
            _rotatedShapes.Add(tempShape);
            tempShape = RectUtils.RotateShape(tempShape);
        }

        propData.mapping = InitializeDictionary(propData.mapping);

        for (var i = 0; i < 4; i++)
        {
            _rotatedDictionaries.Add(CreateRotatedDictionary(propData.mapping, _rotatedShapes[i]));
        }

        _rotateCount = 0;
        propData.mapping = _rotatedDictionaries[3];
        _currentDic = _rotatedDictionaries[3];
        propData.shape = _rotatedShapes[3];
        _currentShape = _rotatedShapes[3];
    }

    public void Initialize(Prop propTemplate, GameObject obj, ItemManager manager)
    {
        propData = new PropData(propTemplate, obj);
        _itemManager = manager;
        _lastGridPosition = propData.position;

        _rotatedShapes = new List<Vector2Int[]>();
        _rotatedDictionaries = new List<PropShapeDictionary>();
        _propPlayActions = new PlayerInputControl();

        SetPropItem();
    }

    public void CleanupBeforeDestroy()
    {
        if (_propPlayActions != null)
        {
            _propPlayActions.ItemPlay.Disable();
            _propPlayActions = null;
        }
    }

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

                var currentBoundaryOffets = _propManager.CalculateSingleCellBoundary(offset, _currentShape);

                _currentDic.TryGetValue(offset, out var tag);

                // Debug.Log("物品形状：" + offset);
                // Debug.Log("起始坐标：" + gridPosition);
                // Debug.Log("绝对坐标：" + new Vector2Int(cellX, cellY));

                if (isOccupied)
                {
                    cell.InnerProp = propData;
                    cell.UpdateCellState();
                    propData.isPlaced = true;

                    if (tag != ItemTag.None && tag != ItemTag.Ordinary)
                    {
                        LevelStateController.Instance.AddTag(tag);

                        foreach (var boundaryOffset in currentBoundaryOffets)
                        {
                            var boundaryCellX = gridPosition.x + boundaryOffset.x;
                            var boundaryCellY = gridPosition.y + boundaryOffset.y;
                            if (_inventoryInitializer.IsValidGrid(boundaryCellX, boundaryCellY))
                            {
                                var boundaryCell = _inventoryInitializer.cells[boundaryCellX, boundaryCellY];
                                // if (!propManager.taggedCells.ContainsKey(tag))
                                // {
                                //     propManager.taggedCells[tag] = new List<CellData>();
                                // }
                                if (boundaryCell.GetCellState() == CellData.State.Packable || boundaryCell.GetCellState() == CellData.State.Occupied)
                                    _propManager.taggedCells[tag].Add(boundaryCell);
                            }
                        }
                    }

                    if (cell.innerBp.holdingProps.Contains(propData))
                    {
                    }
                    else
                        cell.innerBp.holdingProps.Add(propData);
                }
                else
                {
                    cell.InnerProp = null;
                    if (cell.innerBp.holdingProps != null && cell.innerBp.holdingProps.Contains(propData)) cell.innerBp.holdingProps.Remove(propData);
                    cell.UpdateCellState();
                    propData.isPlaced = false;

                    if (tag != ItemTag.None && tag != ItemTag.Ordinary)
                    {
                        LevelStateController.Instance.RemoveTag(tag);

                        foreach (var boundaryOffset in currentBoundaryOffets)
                        {
                            var boundaryCellX = gridPosition.x + boundaryOffset.x;
                            var boundaryCellY = gridPosition.y + boundaryOffset.y;
                            if (_inventoryInitializer.IsValidGrid(boundaryCellX, boundaryCellY))
                            {
                                var boundaryCell = _inventoryInitializer.cells[boundaryCellX, boundaryCellY];
                                // if (!propManager.taggedCells.ContainsKey(tag))
                                // {
                                //     propManager.taggedCells[tag] = new List<CellData>();
                                // }
                                if (boundaryCell.GetCellState() == CellData.State.Packable || boundaryCell.GetCellState() == CellData.State.Occupied)
                                    _propManager.taggedCells[tag].Remove(boundaryCell);
                            }
                        }
                    }
                }
            }
            // inventoryInitializer.cells[cellX, cellY].isOccupied = isOccupied;
            // Debug.Log("网格" + inventoryInitializer.cells[cellX, cellY].gridPosition + "状态更新为：" + inventoryInitializer.cells[cellX, cellY].GetCellState());
        }
    }

    private void HighlightTags()
    {
        // 处理Tag类高亮

        ClearHighlightedTags();

        if (!RectUtils.IsRectangleInside(_rightPanelRect, _rectTransform)) return;

        var nearestSlot = FindNearestSlot();
        if (nearestSlot != null)
        {
            var startPosition = nearestSlot.gridPosition;
            // Debug.Log("最近网格为：" + startPosition);

            foreach (var offset in _currentShape)
            {
                // 计算对应的绝对网格坐标
                var cellX = startPosition.x + offset.x;
                var cellY = startPosition.y + offset.y;

                var currentBoundaryOffets = _propManager.CalculateSingleCellBoundary(offset, _currentShape);

                _currentDic.TryGetValue(offset, out var tag);

                // Debug.Log("开始检测坐标：" + offset + "TAG: " + tag);

                if (_inventoryInitializer.IsValidGrid(cellX, cellY))
                {
                    var cell = _inventoryInitializer.cells[cellX, cellY];

                    if (cell.blockObj != null && cell.GetCellState() == CellData.State.Packable)
                    {
                        switch (tag)
                        {
                            case ItemTag.Ordinary:
                            case ItemTag.None:
                                // 无功能无变化
                                break;
                            case ItemTag.CoolI:
                            case ItemTag.CoolII:
                            case ItemTag.CoolIII:
                                // 对每一个boundary网格的sprite做变化
                                DisplayTagIcon(startPosition, tag, currentBoundaryOffets);
                                break;
                            case ItemTag.NaturalI:
                            case ItemTag.NaturalII:
                            case ItemTag.NaturalIII:
                                // 对每一个boundary网格的sprite做变化
                                DisplayTagIcon(startPosition, tag, currentBoundaryOffets);
                                break;
                            case ItemTag.FragileI:
                            case ItemTag.FragileII:
                            case ItemTag.FragileIII:
                                // 对每一个boundary网格的sprite做变化
                                DisplayTagIcon(startPosition, tag, currentBoundaryOffets);
                                break;
                        }
                    }
                }
            }
        }
    }

    private void HighlightCells()
    {
        // 处理物品自身占位高亮

        // 清除之前的高亮显示
        ClearHighlight();

        if (!RectUtils.IsRectangleInside(_rightPanelRect, _rectTransform)) return;

        var nearestSlot = FindNearestSlot();
        if (nearestSlot != null)
        {
            var startPosition = nearestSlot.gridPosition;
            // Debug.Log("最近网格为：" + startPosition);

            var canPlace = CanPlaceItem(nearestSlot);

            foreach (var offset in _currentShape)
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

    private void RotateProp()
    {
        switch (_rotateCount)
        {
            case 1:
                _rectTransform.Rotate(0, 0, 90);
                _rectTransform.pivot = new Vector2(1, 1);
                _currentShape = _rotatedShapes[0];
                _currentDic = _rotatedDictionaries[0];
                _cl.offset = new Vector2(-_rectTransform.rect.width, 0);
                break;
            case 2:
                _rectTransform.Rotate(0, 0, 90);
                _rectTransform.pivot = new Vector2(1, 0);
                _currentShape = _rotatedShapes[1];
                _currentDic = _rotatedDictionaries[1];
                _cl.offset = new Vector2(-_rectTransform.rect.width, _rectTransform.rect.height);
                break;
            case 3:
                _rectTransform.Rotate(0, 0, -270);
                _rectTransform.pivot = new Vector2(0, 0);
                _currentShape = _rotatedShapes[2];
                _currentDic = _rotatedDictionaries[2];
                _cl.offset = new Vector2(0, _rectTransform.rect.height);
                break;
            case 4:
                _rectTransform.Rotate(0, 0, 90);
                _rectTransform.pivot = new Vector2(0, 1);
                _currentShape = _rotatedShapes[3];
                _currentDic = _rotatedDictionaries[3];
                _cl.offset = new Vector2(0, 0);
                _rotateCount = 0;
                break;
        }
    }

    private void RotatePropBackwards()
    {
        switch (_rotateCount)
        {
            case 1:
                _rectTransform.Rotate(0, 0, -90);
                _rectTransform.pivot = new Vector2(1, 1);
                propData.shape = _rotatedShapes[0];
                _currentShape = _rotatedShapes[0];
                _cl.offset = new Vector2(-_rectTransform.rect.width, 0);
                break;
            case 2:
                _rectTransform.Rotate(0, 0, -90);
                _rectTransform.pivot = new Vector2(1, 0);
                propData.shape = _rotatedShapes[1];
                _currentShape = _rotatedShapes[1];
                _cl.offset = new Vector2(-_rectTransform.rect.width, _rectTransform.rect.height);
                break;
            case 3:
                _rectTransform.Rotate(0, 0, 270);
                _rectTransform.pivot = new Vector2(0, 0);
                propData.shape = _rotatedShapes[2];
                _currentShape = _rotatedShapes[2];
                _cl.offset = new Vector2(0, _rectTransform.rect.height);
                break;
            case 0:
                _rectTransform.Rotate(0, 0, -90);
                _rectTransform.pivot = new Vector2(0, 1);
                propData.shape = _rotatedShapes[3];
                _currentShape = _rotatedShapes[3];
                _cl.offset = new Vector2(0, 0);
                break;
        }
    }

    private CellData FindNearestSlot()
    {
        CellData nearestSlot = null;
        var minDistance = float.MaxValue;

        Vector3 itemPosition = _rectTransform.anchoredPosition;

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

    private void SnapItemRotation()
    {
        var zRotation = _rectTransform.eulerAngles.z;

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

        // Debug.Log("近似角度为：" + snappedRotation);

        switch (snappedRotation)
        {
            case 90:
                _rotateCount = 1;
                _rectTransform.rotation = Quaternion.Euler(0, 0, 90);
                _rectTransform.pivot = new Vector2(1, 1);
                _currentShape = _rotatedShapes[0];
                _currentDic = _rotatedDictionaries[0];
                _cl.offset = new Vector2(-_rectTransform.rect.width, 0);
                break;
            case 180:
                _rotateCount = 2;
                _rectTransform.rotation = Quaternion.Euler(0, 0, 180);
                _rectTransform.pivot = new Vector2(1, 0);
                _currentShape = _rotatedShapes[1];
                _currentDic = _rotatedDictionaries[1];
                _cl.offset = new Vector2(-_rectTransform.rect.width, _rectTransform.rect.height);
                break;
            case -90:
                _rotateCount = 3;
                _rectTransform.rotation = Quaternion.Euler(0, 0, -90);
                _rectTransform.pivot = new Vector2(0, 0);
                _currentShape = _rotatedShapes[2];
                _currentDic = _rotatedDictionaries[2];
                _cl.offset = new Vector2(0, _rectTransform.rect.height);
                break;
            case 0:
                _rotateCount = 0;
                _rectTransform.rotation = Quaternion.Euler(0, 0, 0);
                _rectTransform.pivot = new Vector2(0, 1);
                _currentShape = _rotatedShapes[3];
                _currentDic = _rotatedDictionaries[3];
                _cl.offset = new Vector2(0, 0);
                break;
        }
    }

    private bool CanPlaceItem(CellData slot)
    {
        var startPosition = slot.gridPosition;

        foreach (var offset in _currentShape)
        {
            var cellX = startPosition.x + offset.x;
            var cellY = startPosition.y + offset.y;
            if (!_inventoryInitializer.IsValidGrid(cellX, cellY) ||
                _inventoryInitializer.cells[cellX, cellY].GetCellState() != CellData.State.Packable ||
                !_inventoryInitializer.cells[cellX, cellY].slotObj) return false;
        }

        return true;
    }

    private bool CanActivateProp(CellData slot, ItemTag tag)
    {
        // 判断某网格内是否满足Tag触发条件
        if (slot.InnerItem != null && slot.GetCellState() == CellData.State.Occupied)
        {
            // 找到目标tag对应的ItemEffect
            var targetEffect = FindItemEffectForTag(tag);

            if (targetEffect.HasValue)
            {
                foreach (var itemTag in slot.InnerItem.itemTagDic.Keys)
                {
                    var itemEffect = FindItemEffectForTag(itemTag);

                    if (itemEffect.HasValue && itemEffect == targetEffect) return true;
                }
            }

            return false;
        }

        // Debug.Log("目标网格无货物或相关物品，无法判断Tag情况");
        return false;
    }

    private ItemEffect? FindItemEffectForTag(ItemTag tag)
    {
        foreach (var entry in ItemManager.UpgradePaths)
        {
            if (entry.Value.Contains(tag)) return entry.Key;
        }

        return null; // 如果没有找到匹配的ItemEffect，则返回null
    }

    private void ClearHighlightedTags()
    {
        if (_propManager.highlightedCells.Count > 0)
        {
            foreach (var cell in _propManager.highlightedCells)
            {
                if (cell.blockObj != null)
                {
                    var cellImage = cell.blockObj.GetComponent<Image>();
                    if (cellImage != null)
                    {
                        cellImage.sprite = _propManager.originalBlock; // 恢复原格图片
                        cellImage.color = new Color32(1, 1, 1, 0);
                    }
                }
            }

            _propManager.highlightedCells.Clear();
        }
    }

    private void ClearHighlight()
    {
        if (_itemManager.highlightedCells.Count > 0)
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
    }

    private void DisplayTagIcon(Vector2Int startPosition, ItemTag tag, Vector2Int[] currentBoundaryOffets)
    {
        foreach (var boundaryOffset in currentBoundaryOffets)
        {
            var boundaryCellX = startPosition.x + boundaryOffset.x;
            var boundaryCellY = startPosition.y + boundaryOffset.y;
            if (_inventoryInitializer.IsValidGrid(boundaryCellX, boundaryCellY))
            {
                var boundaryCell = _inventoryInitializer.cells[boundaryCellX, boundaryCellY];
                _propManager.highlightedCells.Add(boundaryCell);
                var cellImage = boundaryCell.blockObj.GetComponent<Image>();
                if (cellImage != null)
                {
                    switch (tag)
                    {
                        case ItemTag.CoolI:
                        case ItemTag.CoolII:
                        case ItemTag.CoolIII:
                            cellImage.sprite = CanActivateProp(boundaryCell, tag) ? _propManager.enabledCool : _propManager.disabledCool;
                            cellImage.color = new Color(255, 255, 255, 1);
                            break;
                        case ItemTag.NaturalI:
                        case ItemTag.NaturalII:
                        case ItemTag.NaturalIII:
                            cellImage.sprite = CanActivateProp(boundaryCell, tag) ? _propManager.enabledNatural : _propManager.disabledNatural;
                            cellImage.color = new Color(255, 255, 255, 1);
                            break;
                        case ItemTag.FragileI:
                        case ItemTag.FragileII:
                        case ItemTag.FragileIII:
                            cellImage.sprite = CanActivateProp(boundaryCell, tag) ? _propManager.enabledFragile : _propManager.disabledFragile;
                            cellImage.color = new Color(255, 255, 255, 1);
                            break;
                    }
                }
            }
        }
    }


    private void SetCollider(bool isActive)
    {
        // _cl.enabled = isActive;
        _cl.isTrigger = isActive;
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

    private PropShapeDictionary InitializeDictionary(PropShapeDictionary originalDict)
    {
        var newDic = new PropShapeDictionary();

        foreach (var kvp in originalDict)
        {
            var originalKey = kvp.Key;
            var tag = kvp.Value;

            // 找到旋转后对应的坐标

            // i表示旋转次数，j表示
            for (var i = 0; i < propData.shape.Length; i++)
            {
                // 遍历找到对应的key-value组合
                // rotatedShapes[3]/rotatedDictionaries[3]表示初始状态
                if (_rotatedShapes[3][i] == originalKey)
                {
                    newDic[_rotatedShapes[3][i]] = tag;
                    break;
                }
            }
        }

        return newDic;
    }

    private PropShapeDictionary CreateRotatedDictionary(PropShapeDictionary originalDict, Vector2Int[] rotatedShape)
    {
        var rotatedDict = new PropShapeDictionary();

        var originalKeys = new Vector2Int[originalDict.Count];
        originalDict.Keys.CopyTo(originalKeys, 0);

        for (var i = 0; i < originalKeys.Length; i++)
        {
            rotatedDict[rotatedShape[i]] = originalDict[originalKeys[i]];
        }

        return rotatedDict;
    }

    private T FindInParent<T>(GameObject child) where T : Component
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

    private void FloatAboveMouse(PointerEventData eventData)
    {
        // 设置动画时长
        var duration = 0.5f;

        // 获取当前鼠标位置，计算目标位置
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera,
            out pos);
        Vector2 rotatedOffset = transform.rotation * _mouseOffset;
        Vector2 initialPosition = transform.position;
        Vector2 targetPosition = _canvas.transform.TransformPoint(pos + _floatOffset + rotatedOffset);

        // 使用 DoTween 实现平滑移动，并实时更新位置防止瞬移
        // transform.DOKill();
        transform.DOMove(targetPosition, duration)
            .SetEase(Ease.OutBack) // 使用弹性缓动效果
            .OnUpdate(() =>
            {
                // 在动画执行期间持续更新鼠标位置，防止瞬移
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, eventData.position,
                    eventData.pressEventCamera, out pos);
                targetPosition = _canvas.transform.TransformPoint(pos + _floatOffset + rotatedOffset);
                transform.position = targetPosition;
            })
            .OnComplete(() =>
            {
                // 当动画完成时，可以执行其他操作
            });
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!LevelStateController.Instance.IsAnyDragging())
        {
            CursorController.Instance.SetOnDragCursor();

            LevelViewController.Instance.ShowRotateReminder();

            HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticFeedbackStyle.Medium);
            AudioManager.PlaySound("小世界_SE_拿起货物", AudioType.SE, false);

            _itemManager.pickingItem = gameObject;
            _itemManager.ShowDraggingInfo(propData);

            _originalPosition = _rectTransform.anchoredPosition;
            _originalRotateCount = _rotateCount;
            _originalParent = _rectTransform.parent;

            // 停止可能的动画，避免位置不同步
            _rectTransform.DOKill();
            _canvasGroup.DOKill();

            _canvasGroup.blocksRaycasts = false; // 使物品可穿透
            _canvasGroup.DOFade(0.6f, 0.1f); // 透明
            _rectTransform.DOScale(Vector3.one * 1.1f, 0.1f).SetEase(Ease.OutQuad); // 放大

            SetColliderAndRigidbody(false);
            
            if (_originalParent == _rightPanelItemLayer)
            {
                UpdateCells(_lastGridPosition, _currentShape, false);
                ObjUtils.SetParentAndLayer(gameObject, _rightPanelDraggingLayer, InventoryType.Truck);
                // ObjUtils.SetSiblingOrder(_rightPanelItemLayer.parent.gameObject, leftPanelItemLayer.parent.gameObject);
            }
            else if (_originalParent == _leftPanelItemLayer)
            {
                // 吸附物品的旋转角度
                SnapItemRotation();
                ObjUtils.SetParentAndLayer(gameObject, _rightPanelDraggingLayer, InventoryType.Truck);
                // ObjUtils.SetSiblingOrder(_rightPanelItemLayer.parent.gameObject, leftPanelItemLayer.parent.gameObject);
            }

            FloatAboveMouse(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_itemManager.pickingItem == gameObject)
        {
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, eventData.position,
                eventData.pressEventCamera, out pos);
            Vector2 rotatedOffset = transform.rotation * _mouseOffset;
            transform.position = _canvas.transform.TransformPoint(pos + _floatOffset + rotatedOffset);

            HighlightCells();
            HighlightTags();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_itemManager.pickingItem == gameObject)
        {
            CursorController.Instance.SetNormalCursor();
            LevelViewController.Instance.HideRotateReminder();

            AudioManager.PlaySound("小世界_SE_放下货物", AudioType.SE, false);
            _itemManager.pickingItem = null;
            _itemManager.HideDraggingInfo();

            // 停止可能的动画，避免位置不同步
            _rectTransform.DOKill();
            _canvasGroup.DOKill();
            _rectTransform.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutQuad); // 恢复原始大小
            _canvasGroup.DOFade(1f, 0.1f);
            _canvasGroup.blocksRaycasts = true;
            
            if (_originalParent == _leftPanelItemLayer) HandleLeftPanelDropInTask();
            if (_originalParent == _rightPanelItemLayer) HandleRightPanelDropInTask();
            
            HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticFeedbackStyle.Light);

            // 清除高亮显示
            ClearHighlight();
            ClearHighlightedTags();

            // itemManager.PrintHighlightCells();

            _itemManager.UpdateInnerItem();
            // propManager.PrintTaggedCells();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_itemManager.pickingItem == null) InfoPanelController.Instance.OpenInfoPanel(propData);
    }

    #region 常驻暂存区交互

    private void HandleCabinetPanelDropInPlanning()
    {
        // 暂存区 => 车厢区
        // 1. 有空位，放置成功
        // 2. 无空位，刷新掉落
        if (RectUtils.IsRectangleInside(_rightPanelRect, _rectTransform))
        {
            var nearestSlot = FindNearestSlot();
            if (nearestSlot != null && CanPlaceItem(nearestSlot))
            {
                // 放入
                SetColliderAndRigidbody(false);
                ObjUtils.SetParentAndLayer(gameObject, _rightPanelItemLayer, InventoryType.Truck);

                // Debug.Log("更新状态位置：" + nearestSlot.gridPosition);
                _lastGridPosition = nearestSlot.gridPosition;
                // 将物品放置在相应位置
                _rectTransform.anchoredPosition = nearestSlot.worldPosition;
                propData.position = nearestSlot.gridPosition;
                UpdateCells(nearestSlot.gridPosition, _currentShape, true);
            }
            else
            {
                // 无法放入
                // 来自暂存区，刷新掉落
                SetColliderAndRigidbody(true);
                ObjUtils.SetParentAndLayer(gameObject, _cabinetPanelItemLayer, InventoryType.Cabinet);
                _rectTransform.anchoredPosition = _middlePosition;
            }
        }

        // 暂存区 => 暂存区
        // 暂存区 => 其他任何非法位置
        else
        {
            SetColliderAndRigidbody(true);
            ObjUtils.SetParentAndLayer(gameObject, _cabinetPanelItemLayer, InventoryType.Cabinet);
            if (!RectUtils.IsRectangleInside(_leftPanelRect, _rectTransform))
            {
                // rectTransform.anchoredPosition = originalPosition;
                _rectTransform.anchoredPosition = _middlePosition;
            }
        }
    }

    private void HandleRightPanelDropInPlanning()
    {
        // 车厢区 => 车厢区
        // 1. 有空位，放置成功
        // 2. 无空位，回到原位
        if (RectUtils.IsRectangleInside(_rightPanelRect, _rectTransform))
        {
            var nearestSlot = FindNearestSlot();
            if (nearestSlot != null && CanPlaceItem(nearestSlot))
            {
                SetColliderAndRigidbody(false);
                ObjUtils.SetParentAndLayer(gameObject, _rightPanelItemLayer, InventoryType.Truck);

                // Debug.Log("更新状态位置：" + nearestSlot.gridPosition);
                _lastGridPosition = nearestSlot.gridPosition;
                // 将物品放置在相应位置
                _rectTransform.anchoredPosition = nearestSlot.worldPosition;
                propData.position = nearestSlot.gridPosition;
                UpdateCells(nearestSlot.gridPosition, _currentShape, true);
            }
            else
            {
                // 如果没有找到合适的槽，则恢复位置
                // 1. 重置旋转状态
                if (_rotateCount != _originalRotateCount)
                {
                    while (_rotateCount != _originalRotateCount)
                    {
                        _rotateCount += 1;
                        RotateProp();
                    }
                }

                // 来自右面板，回到原位即可
                ObjUtils.SetParentAndLayer(gameObject, _rightPanelItemLayer, InventoryType.Truck);
                _rectTransform.anchoredPosition = _originalPosition;
                UpdateCells(_lastGridPosition, _currentShape, true);
            }
        }

        // 车厢区 => 其他任何非法位置
        // 车厢区 => 暂存区
        // 直接从指尖掉落
        // 直接刷新掉落
        else
        {
            ObjUtils.SetParentAndLayer(gameObject, _cabinetPanelItemLayer, InventoryType.Cabinet);
            if (!RectUtils.IsRectangleInside(_cabinetPanelRect, _rectTransform))
            {
                // rectTransform.anchoredPosition = originalPosition;
                _rectTransform.anchoredPosition = _middlePosition;
            }

            SetColliderAndRigidbody(true);
            UpdateCells(_lastGridPosition, _currentShape, false);
        }
    }

    #endregion

    #region 任务店内交互逻辑

    private void HandleLeftPanelDropInTask()
    {
        // 左 => 右
        // 1. 有空位，放置成功
        // 2. 无空位，刷新掉落
        if (RectUtils.IsRectangleInside(_rightPanelRect, _rectTransform))
        {
            var nearestSlot = FindNearestSlot();
            if (nearestSlot != null && CanPlaceItem(nearestSlot))
            {
                // 放入
                SetColliderAndRigidbody(false);
                ObjUtils.SetParentAndLayer(gameObject, _rightPanelItemLayer, InventoryType.Truck);

                // Debug.Log("更新状态位置：" + nearestSlot.gridPosition);
                _lastGridPosition = nearestSlot.gridPosition;
                // 将物品放置在相应位置
                _rectTransform.anchoredPosition = nearestSlot.worldPosition;
                propData.position = nearestSlot.gridPosition;
                UpdateCells(nearestSlot.gridPosition, _currentShape, true);
            }
            else
            {
                // 无法放入
                // 来自左面板，刷新掉落
                SetColliderAndRigidbody(true);
                ObjUtils.SetParentAndLayer(gameObject, _leftPanelItemLayer, InventoryType.Pool);
                _rectTransform.anchoredPosition = _middlePosition;
            }
        }

        // 左 => 左
        // 左 => 其他任何非法位置
        else
        {
            SetColliderAndRigidbody(true);
            ObjUtils.SetParentAndLayer(gameObject, _leftPanelItemLayer, InventoryType.Pool);
            if (!RectUtils.IsRectangleInside(_leftPanelRect, _rectTransform))
            {
                // rectTransform.anchoredPosition = originalPosition;
                _rectTransform.anchoredPosition = _middlePosition;
            }
        }
    }

    private void HandleRightPanelDropInTask()
    {
        // 右 => 右
        // 1. 有空位，放置成功
        // 2. 无空位，回到原位
        if (RectUtils.IsRectangleInside(_rightPanelRect, _rectTransform))
        {
            var nearestSlot = FindNearestSlot();
            if (nearestSlot != null && CanPlaceItem(nearestSlot))
            {
                SetColliderAndRigidbody(false);
                ObjUtils.SetParentAndLayer(gameObject, _rightPanelItemLayer, InventoryType.Truck);

                // Debug.Log("更新状态位置：" + nearestSlot.gridPosition);
                _lastGridPosition = nearestSlot.gridPosition;
                // 将物品放置在相应位置
                _rectTransform.anchoredPosition = nearestSlot.worldPosition;
                propData.position = nearestSlot.gridPosition;
                UpdateCells(nearestSlot.gridPosition, _currentShape, true);
            }
            else
            {
                // 如果没有找到合适的槽，则恢复位置
                // 1. 重置旋转状态
                if (_rotateCount != _originalRotateCount)
                {
                    while (_rotateCount != _originalRotateCount)
                    {
                        _rotateCount += 1;
                        RotateProp();
                    }
                }

                // 来自右面板，回到原位即可
                ObjUtils.SetParentAndLayer(gameObject, _rightPanelItemLayer, InventoryType.Truck);
                _rectTransform.anchoredPosition = _originalPosition;
                UpdateCells(_lastGridPosition, _currentShape, true);
            }
        }

        // 右 => 其他任何非法位置
        // 右 => 左
        // 直接从指尖掉落
        // 直接刷新掉落
        else
        {
            ObjUtils.SetParentAndLayer(gameObject, _leftPanelItemLayer, InventoryType.Pool);
            if (!RectUtils.IsRectangleInside(_leftPanelRect, _rectTransform))
            {
                // rectTransform.anchoredPosition = originalPosition;
                _rectTransform.anchoredPosition = _middlePosition;
            }

            SetColliderAndRigidbody(true);
            UpdateCells(_lastGridPosition, _currentShape, false);
        }
    }

    #endregion
}