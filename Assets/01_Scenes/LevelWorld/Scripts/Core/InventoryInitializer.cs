using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryInitializer : MonoBehaviour
{
    public Inventory inventoryInfo;
    // private Backpack backpackInfo;
    public Dictionary<Backpack, Vector2Int> bpDic;
    public ItemManager itemManager;
    public InventoryManager inventoryManager; // 用于调用背包接口
    public CellData[,] cells;
    public RectTransform parentPanel; // 父Panel引用
    public InventoryInitializer oppositeInitializer;
    public InventoryType type;
    // private bool isHairScreen;
    public float gridSize;
    public GridLayoutGroup gridLayout;
    private RectOffset _gridPadding;

    public void InitializeInventory(int inventoryWidth, int inventoryHeight)
    {
        _gridPadding = new RectOffset(0, 0, 0, 0);

        // 屏幕高度
        float screenHeight = Screen.height;
        // 屏幕宽度
        float screenWidth = Screen.width;

        // if (screenHeight > 1284f && screenWidth > 2778f)
        // {
        //     screenHeight = 1284f;
        //     screenWidth = 2778f;
        // }

        // 动态调整面板宽度
        // 正方形背包边长约为屏幕高度的3/4
        // float panelWidth = screenHeight * 0.66f;
        // float panelHeight = panelWidth * (1004f / 838f);

        // 使用固定面板宽度和位置
        float panelWidth;
        float panelHeight;
        switch (type)
        {
            case InventoryType.Truck:
                panelWidth = inventoryManager.rightPanel.sizeDelta.x;
                panelHeight = inventoryManager.rightPanel.sizeDelta.y;
                break;
            case InventoryType.Pool:
                panelWidth = inventoryManager.leftPanel.sizeDelta.x;
                panelHeight = inventoryManager.leftPanel.sizeDelta.y;
                break;
            default:
                panelWidth = inventoryManager.leftPanel.sizeDelta.x;
                panelHeight = inventoryManager.leftPanel.sizeDelta.y;
                break;
        }

        // 设置Pivot
        var panelRect = parentPanel.GetComponent<RectTransform>();
        if (type is InventoryType.Pool)
        {
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(1, 0.5f);
            panelRect.anchoredPosition = inventoryManager.leftPanel.anchoredPosition;
        }
        else
        {
            // panelRect.anchorMin = new Vector2(1, 1);
            // panelRect.anchorMax = new Vector2(1, 1);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            // panelRect.pivot = new Vector2(1, 1);
            panelRect.pivot = new Vector2(0, 0.5f);
            // panelRect.anchoredPosition = new Vector2(panelWidth / 20, -screenHeight / 12);
            panelRect.anchoredPosition = inventoryManager.rightPanel.anchoredPosition;
        }

        // panelRect.sizeDelta = new Vector2(panelWidth, panelWidth); // 动态调整宽度和高度
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight); // 动态调整宽度和高度
        // panelRect.anchoredPosition = new Vector2(0, 0); // 右侧顶边对齐
        // Debug.Log("当前屏幕宽度为：" + panelRect.sizeDelta);

        if (type is InventoryType.Truck)
        {
            // 创建一个副本存放位置信息
            var rightPanelPos = Instantiate(panelRect.gameObject, panelRect.parent);
            var rightPanelRect = rightPanelPos.GetComponent<RectTransform>();
            rightPanelPos.SetActive(false);
            var inc = rightPanelPos.GetComponent<InventoryInitializer>();
            if (inc != null) Destroy(inc);

            LevelStateController.Instance.OriginalInventoryPos = rightPanelRect;
        }

        // 背景层
        var backgroundLayer = CreateLayer("BackgroundLayer", parentPanel);
        var bgImage = backgroundLayer.AddComponent<Image>();
        bgImage.raycastTarget = false;
        bgImage.sprite = inventoryInfo.bgSprite;
        if (type is InventoryType.Pool) bgImage.color = new Color(1f, 1f, 1f, 0);
        bgImage.type = Image.Type.Sliced;
        bgImage.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
        RectUtils.SetBackgroundPivot(bgImage.rectTransform);

        // 网格层
        var gridLayer = CreateLayer("GridLayer", parentPanel);
        gridLayout = gridLayer.AddComponent<GridLayoutGroup>();
        OptimizedGridLayer(screenWidth, screenHeight);
        gridLayout.cellSize = new Vector2(gridSize, gridSize); // 每个格子的大小
        gridLayout.spacing = new Vector2(0, 0);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = inventoryWidth;
        gridLayout.padding = _gridPadding;
        gridLayout.padding = new RectOffset(0, 0, 0, 0);
        gridLayout.childAlignment = type == InventoryType.Truck ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft;

        var containerWidth = panelRect.rect.width;
        var containerHeight = panelRect.rect.height;
        var gridWidth = inventoryWidth * gridSize + (inventoryWidth - 1) * gridLayout.spacing.x;
        var gridHeight = inventoryHeight * gridSize + (inventoryHeight - 1) * gridLayout.spacing.y;

        var xOffset = (containerWidth - gridWidth) / 2f;
        var yOffset = (containerHeight - gridHeight) / 2f;
        // if (isItemPool && isHairScreen)
        // {
        //     // TODO: 多机型适配、这里以13代iPhone刘海屏为例，左侧panel需要右移动
        //     gridLayout.padding.left = 105;
        // }

        // 创建x*y的格子
        for (var y = 0; y < inventoryHeight; y++)
        {
            for (var x = 0; x < inventoryWidth; x++)
            {
                var grid = new GameObject($"Grid_{x}_{y}", typeof(RectTransform), typeof(Image));
                grid.transform.SetParent(gridLayer.transform, false);
                var gridImage = grid.GetComponent<Image>();
                gridImage.sprite = inventoryInfo.gridSprite;
                gridImage.type = Image.Type.Sliced;
                // gridImage.pixelsPerUnitMultiplier = 0.15f;
                gridImage.color = inventoryInfo.gridColor;

                var rectTransform = grid.GetComponent<RectTransform>();
                var xPos = xOffset + gridLayout.padding.left + x * (gridSize + gridLayout.spacing.x);
                var yPos = yOffset + gridLayout.padding.top + y * (gridSize + gridLayout.spacing.y);
                rectTransform.anchoredPosition = new Vector2(xPos, -yPos);
                cells[x, y] = new CellData(grid, x, y, rectTransform.anchoredPosition);
                // Debug.Log(parentPanel.name + "层" + cells[x, y].worldPosition + "注册为网格！");
            }
        }

        // 背包层
        var inventoryLayer = CreateLayer("InventoryLayer", parentPanel);

        // 物品层
        var itemLayer = CreateLayer("ItemLayer", parentPanel);
        itemManager.gridSize = gridSize;
        // if (isItemPool)
        // {
        //     itemManager.InitializeItems(gridSize);
        // }

        // 高亮层
        if (type is InventoryType.Truck)
        {
            var highlightLayer = CreateLayer("HighlightLayer", parentPanel);
            var highlightLayout = highlightLayer.AddComponent<GridLayoutGroup>();
            inventoryManager.CopyGridLayoutGroupParameters(gridLayout, highlightLayout);
            for (var y = 0; y < inventoryHeight; y++)
            {
                for (var x = 0; x < inventoryWidth; x++)
                {
                    var block = new GameObject($"Block_{x}_{y}", typeof(RectTransform), typeof(Image));
                    block.transform.SetParent(highlightLayer.transform, false);
                    var gridImage = block.GetComponent<Image>();
                    gridImage.sprite = inventoryInfo.highlightSprite;
                    gridImage.type = Image.Type.Sliced;
                    // gridImage.pixelsPerUnitMultiplier = 0.15f;
                    gridImage.color = new Color32(0, 0, 0, 0);
                    gridImage.raycastTarget = false;

                    var rectTransform = block.GetComponent<RectTransform>();
                    var xPos = xOffset + gridLayout.padding.left + x * (gridSize + gridLayout.spacing.x);
                    var yPos = yOffset + gridLayout.padding.top + y * (gridSize + gridLayout.spacing.y);
                    rectTransform.anchoredPosition = new Vector2(xPos, -yPos);
                    cells[x, y].blockObj = block;
                    // Debug.Log(parentPanel.name + "层" + cells[x, y].worldPosition + "注册为网格！");
                }
            }
        }

        // 交互层
        // 正在拖拽中的物体放入此层暂存
        var draggingLayer = CreateLayer("DraggingLayer", parentPanel);


        // 读取默认背包
        foreach (var bp in bpDic.Keys)
        {
            if (bpDic.Count == 1)
            {
                // 计算背包在x*y网格中的起始位置（默认为中央）
                var startX = (inventoryWidth - bp.size.x) / 2;
                var startY = (inventoryHeight - bp.size.y) / 2;
                var startPos = new Vector2Int(startX, startY);
                var bpInstance = CreateBackpack(inventoryLayer, bp, bp.bpName, startPos);
            }
            else
            {
                var bpInstance = CreateBackpack(inventoryLayer, bp, bp.bpName, bpDic[bp]);
            }
        }

        switch (type)
        {
            case InventoryType.Truck:
                inventoryLayer.SetActive(true);
                gridLayer.SetActive(false);
                break;
            case InventoryType.Cabinet:
            case InventoryType.Pool:
            default:
                inventoryLayer.SetActive(false);
                gridLayer.SetActive(true);
                break;
        }

        inventoryManager.gridLayoutGroup = gridLayout;
        inventoryManager.defaultGridObj = cells[0, 0].gridObj;
    }

    private GameObject CreateLayer(string name, Transform parent)
    {
        var layer = new GameObject(name);
        var rectTransform = layer.AddComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        return layer;
    }

    private GameObject CreateBackpack(GameObject parentLayer, Backpack bp, string bpName, Vector2Int startPos)
    {
        var bpInstance = new GameObject(bpName);
        var bpRect = bpInstance.AddComponent<RectTransform>();
        bpInstance.AddComponent<CanvasGroup>();

        bpRect.SetParent(parentLayer.transform, false);

        var bpImage = bpInstance.AddComponent<Image>();
        bpImage.sprite = bp.bgSprite;
        bpImage.type = Image.Type.Sliced;
        // bpImage.pixelsPerUnitMultiplier = 0.3f;

        // 设置锚点和偏移量
        bpRect.anchorMin = new Vector2(0, 1);
        bpRect.anchorMax = new Vector2(0, 1);
        bpRect.pivot = new Vector2(0, 1); // 设置pivot为中心
        bpRect.sizeDelta = Vector2.zero; // 重置sizeDelta以使其大小可以由layout group决定

        var bpLayout = bpInstance.AddComponent<GridLayoutGroup>();
        inventoryManager.CopyGridLayoutGroupParameters(gridLayout, bpLayout);
        if (type is InventoryType.Truck)
        {
            // bpLayout.padding = new RectOffset(6, 6, 6, 6);
            bpLayout.constraintCount = bp.size.x;
            // bpLayout.childAlignment = TextAnchor.MiddleCenter;
        }

        bpInstance.AddComponent<BackpackInteraction>().Initialize(bp, bpInstance, startPos, this);
        bpInstance.tag = "Backpack";
        var bpInc = bpInstance.GetComponent<BackpackInteraction>();
        bpInc.slotObj = new List<GameObject>();

        // 根据InventoryData创建对应背包
        for (var y = 0; y < bp.size.y; y++)
        {
            for (var x = 0; x < bp.size.x; x++)
            {
                var gridSlot = cells[x + startPos.x, y + startPos.y].gridObj; // 复制对应位置的网格层格子
                var slot = Instantiate(gridSlot, bpInstance.transform); // 将其复制到背包层中
                slot.name = $"Slot_{x}_{y}";
                var slotImage = slot.GetComponent<Image>();
                slotImage.sprite = bp.gridSprite;
                slotImage.color = Color.white;
                cells[x + startPos.x, y + startPos.y].slotObj = slot;
                cells[x + startPos.x, y + startPos.y].innerBp = bpInc.backpackData; // 注册为背包格
                cells[x + startPos.x, y + startPos.y].UpdateCellState();
                bpInc.slotObj.Add(slot);
                // Debug.Log(parentPanel.name + "层" + cells[x + startPos.x, y + startPos.y].gridPosition + "注册为背包格！");
            }
        }

        bpRect.sizeDelta = new Vector2(
            bpLayout.cellSize.x * bp.size.x + bpLayout.spacing.x * (bp.size.x - 1) + bpLayout.padding.left * 2,
            bpLayout.cellSize.y * bp.size.y + bpLayout.spacing.y * (bp.size.y - 1) + bpLayout.padding.top * 2
        );

        bpRect.anchoredPosition = cells[startPos.x, startPos.y].worldPosition;

        if (type is InventoryType.Truck)
        {
            // 添加物理组件
            var rb = bpInstance.AddComponent<Rigidbody2D>();
            // 设置Rigidbody2D属性
            rb.mass = 3.0f; // 质量，适中
            rb.gravityScale = 20.0f; // 重力比例，标准重力
            rb.drag = 0.0f; // 线性阻尼，减少空气阻力
            rb.angularDrag = 0.0f; // 角度阻尼，减少旋转阻力
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            // rb.freezeRotation = true;

            // 设置初始速度
            rb.velocity = new Vector2(0f, 5.0f); // 初始速度，右上角方向

            // 创建一个PhysicsMaterial2D并设置其弹性
            var material = new PhysicsMaterial2D();
            material.bounciness = 0.3f; // 弹性
            material.friction = 1.0f; // 摩擦力
            var collider = bpInstance.AddComponent<PolygonCollider2D>();
            itemManager.SetPolygonColliderPoints(collider, bp.shape);
            collider.sharedMaterial = material;

            bpInc.SetBackpack();

            inventoryManager.backpacks.Add(bpInc.backpackData);
            bpInc.backpackData.isPlaced = true;
        }

        bpInstance.SetActive(true);

        return bpInstance;
    }

    private void OptimizedGridLayer(float screenWidth, float screenHeight)
    {
        var aspectRatio = screenWidth / screenHeight;
        var optimizedRatioPC = screenWidth / 1920;
        var optimizedRatioMobile = screenWidth / 2778;

        var gridOffset = 100f;

        LevelStateController.Instance.SetScreenModeBasedOnDevice();

        switch (LevelStateController.Instance.GetCurrentScreenMode())
        {
            case ScreenMode.SingleScale:
                break;
            case ScreenMode.DoubleScale:
                gridOffset = 160f;
                break;
        }

        if (aspectRatio >= 2) // 宽高比大于等于2（宽屏、iPhone新机型）
        {
            // gridSize = 116f * optimizedRatioMobile;
            gridSize = gridOffset * optimizedRatioMobile;
            // gridPadding = new RectOffset(Mathf.FloorToInt(59 * optimizedRatioMobile), 0, Mathf.FloorToInt(59 * optimizedRatioMobile), 0);
            // isHairScreen = true;
        }
        else if (aspectRatio >= 16f / 9f) // 16:9比例（PC、iPhone老机型）
        {
            // gridSize = 100f * optimizedRatioPC;
            gridSize = 150f;
            // gridPadding = new RectOffset(Mathf.FloorToInt(26 * optimizedRatioPC), 0, Mathf.FloorToInt(86 * optimizedRatioPC), 0);
            // isHairScreen = false;
        }
        else // 默认
        {
            gridSize = gridOffset;
            // gridPadding = new RectOffset(10, 0, 10, 0);
            // isHairScreen = false;
        }

        // Debug.Log("!!!!!!CurrentScreenMode!!!!!: " + inventoryManager.GetCurrentScreenMode());
        // Debug.Log("!!!!!!AspectRatio!!!!!: " + aspectRatio);
        // Debug.Log("!!!!!!OptimizedRatio!!!!!: " + optimizedRatioMobile);
        // Debug.Log("!!!!!!gridSize!!!!!: " + gridSize);
    }

    public void ResetCellData()
    {
        for (var y = 0; y < inventoryManager.inventoryHeight; y++)
        {
            for (var x = 0; x < inventoryManager.inventoryWidth; x++)
            {
                // cells[x, y].isOccupied = false;
                cells[x, y].InnerItem = null;
            }
        }
    }

    public bool IsValidGrid(int cellX, int cellY)
    {
        return cellX >= 0 && cellX < inventoryManager.inventoryWidth && cellY >= 0 && cellY < inventoryManager.inventoryHeight;
    }
}