using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuffInteraction : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const float mouseMoveThreshold = 5f;
    public BuffData buffData;

    public PlayerInputControl buffPlayActions;

    public void SetBuffedItem()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = FindInParent<Canvas>(gameObject);
        originalPosition = rectTransform.anchoredPosition;

        // 默认初始位置总是为商店
        buffManager = LevelStateController.Instance.GetService<BuffManager>();
        inventoryInitializer = LevelStateController.Instance.GetDefaultInventoryInit();
        middlePosition = inventoryInitializer.oppositeInitializer.cells[3, 0].worldPosition;

        floatOffset = new Vector2(-rectTransform.rect.width / 2, rectTransform.rect.height + inventoryInitializer.gridSize / 2);

        rb = GetComponent<Rigidbody2D>();
        cl = GetComponent<PolygonCollider2D>();

        // 获取收货界面
        // 默认buffedItem附属于rightpanel
        leftPanelItemLayer = inventoryInitializer.oppositeInitializer.transform.Find("ItemLayer");
        leftPanelDraggingLayer = inventoryInitializer.oppositeInitializer.transform.Find("DraggingLayer");
        rightPanelItemLayer = inventoryInitializer.transform.Find("ItemLayer");
        rightPanelDraggingLayer = inventoryInitializer.transform.Find("DraggingLayer");
        leftPanelRect = leftPanelItemLayer.GetComponent<RectTransform>();
        rightPanelRect = rightPanelItemLayer.GetComponent<RectTransform>();
        

        var tempShape = RectUtils.RotateShape(buffData.shape);

        for (var i = 0; i < 4; i++)
        {
            rotatedShapes.Add(tempShape);
            tempShape = RectUtils.RotateShape(tempShape);
        }

        rotateCount = 0;
        buffData.shape = rotatedShapes[3];
        currentShape = rotatedShapes[3];
    }

    public void Initialize(Buff buffTemplate, GameObject obj, ItemManager manager)
    {
        buffData = new BuffData(buffTemplate, obj);
        itemManager = manager;
        lastGridPosition = buffData.position;

        rotatedShapes = new List<Vector2Int[]>();
        buffPlayActions = new PlayerInputControl();

        SetBuffedItem();
    }

    public void CleanupBeforeDestroy()
    {
        if (buffPlayActions != null)
        {
            buffPlayActions.ItemPlay.Disable();
            buffPlayActions = null;
        }
    }

    private BuffManager buffManager;
    private ItemManager itemManager;
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Vector2 middlePosition;
    private Transform originalTransform;
    private InventoryInitializer inventoryInitializer;
    private Vector2Int[] currentShape;
    private Vector2Int lastGridPosition;
    private Vector2 floatOffset;
    private int rotateCount;
    private int originalRotateCount;
    private Vector2 initialMousePosition;
    private Vector2 mouseOffset;
    private Rigidbody2D rb;
    private PolygonCollider2D cl;

    // 收货界面
    private Transform leftPanelItemLayer;
    private Transform rightPanelItemLayer;
    private RectTransform leftPanelRect;
    private RectTransform rightPanelRect; // 本体车厢
    private Transform leftPanelDraggingLayer;
    private Transform rightPanelDraggingLayer;

    // 商店界面
    private RectTransform storeLeftPanel;
    private RectTransform storeMiddlePanel;
    private RectTransform storeSoldArea;

    private Transform originalParent;

    private Coroutine floatCoroutine;
    private List<Vector2Int[]> rotatedShapes;

    private void OnEnable()
    {
        if (buffPlayActions != null) buffPlayActions.ItemPlay.Enable();
    }

    private void OnDisable()
    {
        if (buffPlayActions != null) buffPlayActions.ItemPlay.Disable();
    }

    private void OnDestroy()
    {
        CleanupBeforeDestroy();
    }

    private void Update()
    {
        // if (buffPlayActions.ItemPlay.ScrollRotate.WasPerformedThisFrame())
        // {
        //     Debug.Log("检测到滚轮！");
        // }

        if (LevelStateController.Instance.IsAnyDragging() && itemManager.pickingItem == gameObject)
        {
            if (buffPlayActions.ItemPlay.Rotate.WasPerformedThisFrame())
            {
                HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticFeedbackStyle.Light);
                rotateCount += 1;
                RotateItem();
            }
        }
    }

    private void UpdateBuffEffect()
    {
        if (transform.parent == rightPanelItemLayer && buffData.isEnabled == false)
        {
            buffManager.EnableBuff(buffData.name);
            buffData.isEnabled = true;
        }
        else if (transform.parent != rightPanelItemLayer && buffData.isEnabled)
        {
            buffManager.DisableBuff(buffData.name);
            buffData.isEnabled = false;
        }
    }

    private void UpdateCells(Vector2Int gridPosition, Vector2Int[] shape, bool isOccupied)
    {
        // 按照形状更新对应网格的状态
        foreach (var offset in shape)
        {
            var cellX = gridPosition.x + offset.x;
            var cellY = gridPosition.y + offset.y;
            if (cellX >= 0 && cellX < 7 && cellY >= 0 && cellY < 7)
            {
                var cell = inventoryInitializer.cells[cellX, cellY];

                // Debug.Log("物品形状：" + offset);
                // Debug.Log("起始坐标：" + gridPosition);
                // Debug.Log("绝对坐标：" + new Vector2Int(cellX, cellY));

                if (isOccupied)
                {
                    cell.InnerBuff = buffData;
                    cell.UpdateCellState();
                    buffData.isPlaced = true;
                    if (cell.innerBp.holdingBuffs.Contains(buffData))
                    {
                    }
                    else
                        cell.innerBp.holdingBuffs.Add(buffData);
                }
                else
                {
                    cell.InnerBuff = null;
                    if (cell.innerBp.holdingBuffs != null && cell.innerBp.holdingBuffs.Contains(buffData)) cell.innerBp.holdingBuffs.Remove(buffData);
                    cell.UpdateCellState();
                    buffData.isPlaced = false;
                }
            }
            // inventoryInitializer.cells[cellX, cellY].isOccupied = isOccupied;
            // Debug.Log("网格" + inventoryInitializer.cells[cellX, cellY].gridPosition + "状态更新为：" + inventoryInitializer.cells[cellX, cellY].GetCellState());
        }
    }

    private void HighlightCells()
    {
        // 清除之前的高亮显示
        ClearHighlight();

        if (!RectUtils.IsRectangleInside(rightPanelRect, rectTransform)) return;

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
                if (cellX >= 0 && cellX < 7 && cellY >= 0 && cellY < 7)
                {
                    var cell = inventoryInitializer.cells[cellX, cellY];
                    if (cell.slotObj != null && cell.blockObj != null)
                    {
                        itemManager.highlightedCells.Add(cell);
                        var cellImage = cell.blockObj.GetComponent<Image>();
                        if (cellImage != null)
                            cellImage.color = canPlace ? itemManager.highlightGreen : itemManager.highlightRed; // 根据是否能放置高亮显示为绿色或红色
                    }
                }
            }
        }
    }

    private void RotateItem()
    {
        switch (rotateCount)
        {
            case 1:
                rectTransform.Rotate(0, 0, 90);
                rectTransform.pivot = new Vector2(1, 1);
                // itemData.shape = rotatedShapes[0];
                currentShape = rotatedShapes[0];
                cl.offset = new Vector2(-rectTransform.rect.width, 0);
                break;
            case 2:
                rectTransform.Rotate(0, 0, 90);
                rectTransform.pivot = new Vector2(1, 0);
                // itemData.shape = rotatedShapes[1];
                currentShape = rotatedShapes[1];
                cl.offset = new Vector2(-rectTransform.rect.width, rectTransform.rect.height);
                break;
            case 3:
                rectTransform.Rotate(0, 0, -270);
                rectTransform.pivot = new Vector2(0, 0);
                // itemData.shape = rotatedShapes[2];
                currentShape = rotatedShapes[2];
                cl.offset = new Vector2(0, rectTransform.rect.height);
                break;
            case 4:
                rectTransform.Rotate(0, 0, 90);
                rectTransform.pivot = new Vector2(0, 1);
                // itemData.shape = rotatedShapes[3];
                currentShape = rotatedShapes[3];
                cl.offset = new Vector2(0, 0);
                rotateCount = 0;
                break;
        }
    }

    private CellData FindNearestSlot()
    {
        CellData nearestSlot = null;
        var minDistance = float.MaxValue;

        Vector3 itemPosition = rectTransform.anchoredPosition;

        for (var y = 0; y < inventoryInitializer.cells.GetLength(1); y++)
        {
            for (var x = 0; x < inventoryInitializer.cells.GetLength(0); x++)
            {
                var cell = inventoryInitializer.cells[x, y];
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

        // Debug.Log("近似角度为：" + snappedRotation);

        switch (snappedRotation)
        {
            case 90:
                rotateCount = 1;
                rectTransform.rotation = Quaternion.Euler(0, 0, 90);
                rectTransform.pivot = new Vector2(1, 1);
                // itemData.shape = rotatedShapes[0];
                currentShape = rotatedShapes[0];
                cl.offset = new Vector2(-rectTransform.rect.width, 0);
                break;
            case 180:
                rotateCount = 2;
                rectTransform.rotation = Quaternion.Euler(0, 0, 180);
                rectTransform.pivot = new Vector2(1, 0);
                // itemData.shape = rotatedShapes[1];
                currentShape = rotatedShapes[1];
                cl.offset = new Vector2(-rectTransform.rect.width, rectTransform.rect.height);
                break;
            case -90:
                rotateCount = 3;
                rectTransform.rotation = Quaternion.Euler(0, 0, -90);
                rectTransform.pivot = new Vector2(0, 0);
                // itemData.shape = rotatedShapes[2];
                currentShape = rotatedShapes[2];
                cl.offset = new Vector2(0, rectTransform.rect.height);
                break;
            case 0:
                rotateCount = 0;
                rectTransform.rotation = Quaternion.Euler(0, 0, 0);
                rectTransform.pivot = new Vector2(0, 1);
                // itemData.shape = rotatedShapes[3];
                currentShape = rotatedShapes[3];
                cl.offset = new Vector2(0, 0);
                break;
        }
    }

    private void ChangePanel()
    {
        var temp = inventoryInitializer;
        inventoryInitializer = inventoryInitializer.oppositeInitializer;
        inventoryInitializer.oppositeInitializer = temp;
    }

    private bool CanPlaceItem(CellData slot)
    {
        var startPosition = slot.gridPosition;

        foreach (var offset in currentShape)
        {
            var cellX = startPosition.x + offset.x;
            var cellY = startPosition.y + offset.y;
            if (cellX < 0 || cellX >= 7 || cellY < 0 || cellY >= 7 ||
                inventoryInitializer.cells[cellX, cellY].GetCellState() != CellData.State.Packable ||
                !inventoryInitializer.cells[cellX, cellY].slotObj) return false;
        }

        return true;
    }

    private void ClearHighlight()
    {
        if (itemManager.highlightedCells.Count > 0)
        {
            foreach (var cell in itemManager.highlightedCells)
            {
                if (cell.slotObj != null && cell.blockObj != null)
                {
                    var cellImage = cell.blockObj.GetComponent<Image>();
                    if (cellImage != null) cellImage.color = new Color32(1, 1, 1, 0); // 恢复透明
                }
            }

            itemManager.highlightedCells.Clear();
        }
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

    private void SetCollider(bool isActive)
    {
        cl.enabled = isActive;
    }

    private void SetRigidbody(bool isActive)
    {
        if (isActive)
            rb.bodyType = RigidbodyType2D.Dynamic;
        else
            rb.bodyType = RigidbodyType2D.Static;
    }

    private void SetColliderAndRigidbody(bool status)
    {
        SetCollider(status);
        SetRigidbody(status);
    }

    private void FloatAboveMouse(PointerEventData eventData)
    {
        // 设置动画时长
        var duration = 0.3f;

        // 获取当前鼠标位置，计算目标位置
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera,
            out pos);
        Vector2 rotatedOffset = transform.rotation * mouseOffset;
        Vector2 initialPosition = transform.position;
        Vector2 targetPosition = canvas.transform.TransformPoint(pos + floatOffset + rotatedOffset);

        // 使用 DoTween 实现平滑移动，并实时更新位置防止瞬移
        transform.DOMove(targetPosition, duration)
            .SetEase(Ease.OutBack) // 使用弹性缓动效果
            .OnUpdate(() =>
            {
                // 在动画执行期间持续更新鼠标位置，防止瞬移
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position,
                    eventData.pressEventCamera, out pos);
                targetPosition = canvas.transform.TransformPoint(pos + floatOffset + rotatedOffset);
                transform.position = targetPosition;
            });
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!LevelStateController.Instance.IsAnyDragging())
        {
            buffPlayActions.ItemPlay.Enable();
            AudioManager.PlaySound("小世界_SE_拿起货物", AudioType.SE, false);

            HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticFeedbackStyle.Medium);

            itemManager.pickingItem = gameObject;
            itemManager.ShowDraggingInfo(buffData);

            originalPosition = rectTransform.anchoredPosition;
            originalRotateCount = rotateCount;
            originalParent = rectTransform.parent;

            // 停止可能的动画，避免位置不同步
            rectTransform.DOKill();
            canvasGroup.DOKill();

            canvasGroup.blocksRaycasts = false; // 使物品可穿透
            canvasGroup.alpha = 0.6f; // 透明
            rectTransform.localScale = Vector3.one * 1.1f; // 放大

            SetColliderAndRigidbody(false);
            
            if (originalParent == rightPanelItemLayer)
            {
                // UpdateCells(lastGridPosition, itemData.shape, false);
                UpdateCells(lastGridPosition, currentShape, false);
                rectTransform.SetParent(rightPanelDraggingLayer);

                ObjUtils.SetSiblingOrder(rightPanelItemLayer.parent.gameObject, leftPanelItemLayer.parent.gameObject);
            }
            else if (originalParent == leftPanelItemLayer)
            {
                // 吸附物品的旋转角度
                SnapItemRotation();
                rectTransform.SetParent(rightPanelDraggingLayer);
                ObjUtils.SetSiblingOrder(rightPanelItemLayer.parent.gameObject, leftPanelItemLayer.parent.gameObject);
            }

            // 启动检测鼠标移动和浮动动画
            FloatAboveMouse(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (itemManager.pickingItem == gameObject)
        {
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera,
                out pos);
            Vector2 rotatedOffset = transform.rotation * mouseOffset;
            transform.position = canvas.transform.TransformPoint(pos + floatOffset + rotatedOffset);

            HighlightCells();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (itemManager.pickingItem == gameObject)
        {
            buffPlayActions.ItemPlay.Disable();
            AudioManager.PlaySound("小世界_SE_放下货物", AudioType.SE, false);
            itemManager.pickingItem = null;
            itemManager.HideDraggingInfo();

            // 停止可能的动画，避免位置不同步
            rectTransform.DOKill();
            canvasGroup.DOKill();
            rectTransform.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutQuad); // 恢复原始大小
            canvasGroup.DOFade(1f, 0.1f);
            canvasGroup.blocksRaycasts = true;

            if (originalParent == leftPanelItemLayer) HandleLeftPanelDropInTask();

            if (originalParent == rightPanelItemLayer) HandleRightPanelDropInTask();

            HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticFeedbackStyle.Light);
            
            // 清除高亮显示
            ClearHighlight();

            // itemManager.PrintHighlightCells();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemManager.pickingItem == null) InfoPanelController.Instance.OpenInfoPanel(buffData);
    }

    #region 任务店内交互逻辑

    private void HandleLeftPanelDropInTask()
    {
        // 左 => 右
        // 1. 有空位，放置成功
        // 2. 无空位，刷新掉落
        if (RectUtils.IsRectangleInside(rightPanelRect, rectTransform))
        {
            var nearestSlot = FindNearestSlot();
            if (nearestSlot != null && CanPlaceItem(nearestSlot))
            {
                // 放入
                SetColliderAndRigidbody(false);
                rectTransform.SetParent(rightPanelItemLayer);

                // Debug.Log("更新状态位置：" + nearestSlot.gridPosition);
                lastGridPosition = nearestSlot.gridPosition;
                // 将物品放置在相应位置
                rectTransform.anchoredPosition = nearestSlot.worldPosition;
                buffData.position = nearestSlot.gridPosition;
                UpdateCells(nearestSlot.gridPosition, currentShape, true);
            }
            else
            {
                // 无法放入
                // 来自左面板，刷新掉落
                SetColliderAndRigidbody(true);
                rectTransform.SetParent(leftPanelItemLayer);
                rectTransform.anchoredPosition = middlePosition;
            }
        }

        // 左 => 左
        // 左 => 其他任何非法位置
        else
        {
            SetColliderAndRigidbody(true);
            rectTransform.SetParent(leftPanelItemLayer);
            if (!RectUtils.IsRectangleInside(leftPanelRect, rectTransform))
            {
                // rectTransform.anchoredPosition = originalPosition;
                rectTransform.anchoredPosition = middlePosition;
            }
        }
    }

    private void HandleRightPanelDropInTask()
    {
        // 右 => 右
        // 1. 有空位，放置成功
        // 2. 无空位，回到原位
        if (RectUtils.IsRectangleInside(rightPanelRect, rectTransform))
        {
            var nearestSlot = FindNearestSlot();
            if (nearestSlot != null && CanPlaceItem(nearestSlot))
            {
                SetColliderAndRigidbody(false);
                rectTransform.SetParent(rightPanelItemLayer);

                // Debug.Log("更新状态位置：" + nearestSlot.gridPosition);
                lastGridPosition = nearestSlot.gridPosition;
                // 将物品放置在相应位置
                rectTransform.anchoredPosition = nearestSlot.worldPosition;
                buffData.position = nearestSlot.gridPosition;
                UpdateCells(nearestSlot.gridPosition, currentShape, true);
            }
            else
            {
                // 如果没有找到合适的槽，则恢复位置
                // 1. 重置旋转状态
                if (rotateCount != originalRotateCount)
                {
                    while (rotateCount != originalRotateCount)
                    {
                        rotateCount += 1;
                        RotateItem();
                    }
                }

                // 来自右面板，回到原位即可
                rectTransform.SetParent(rightPanelItemLayer);
                rectTransform.anchoredPosition = originalPosition;
                UpdateCells(lastGridPosition, currentShape, true);
            }
        }

        // 右 => 其他任何非法位置
        // 右 => 左
        // 直接从指尖掉落
        // 直接刷新掉落
        else
        {
            rectTransform.SetParent(leftPanelItemLayer);
            if (!RectUtils.IsRectangleInside(leftPanelRect, rectTransform))
            {
                // rectTransform.anchoredPosition = originalPosition;
                rectTransform.anchoredPosition = middlePosition;
            }

            SetColliderAndRigidbody(true);
            UpdateCells(lastGridPosition, currentShape, false);
        }
    }

    #endregion
}