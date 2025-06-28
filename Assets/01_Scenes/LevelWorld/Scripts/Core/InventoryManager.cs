using System;
using System.Collections.Generic;
using System.Linq;
using cfg.level;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("写死的背包名字")]
    [SerializeField] string InventoryName = "Inventory";
  
    [Space(10)]
    public static Action<ObjectInformation> OnDraggingBpInfo;

    public InventoryLibrary inventoryLibrary;
    public ItemManager itemManager;
    public GameObject pickingBp;
    public GridLayoutGroup gridLayoutGroup; // 保存背包网格参数
    public GameObject defaultGridObj;
    public InventoryInitializer defaultInitializer;
    public RectTransform leftPanel;
    public RectTransform rightPanel;
    public List<GameObject> leftPanelBorders = new();
    public List<CellData> highlightedCells = new();
    public Color32 gridColor;
    public Color32 highlightGreen;
    public Color32 highlightRed;
    // public Dictionary<string, Backpack> backpackDictionary;  // 建立映射关系，避免重复读取数据文件
    public List<BackpackData> backpacks; // 保存背包实例列表，随时刷新
    public int inventoryWidth = 9;
    public int inventoryHeight = 9;

    private void CreateInventory(string inventoryName, RectTransform inventoryPanel)
    {
        // 查找网格信息
        var inventoryInfo = inventoryLibrary.GetInventoryData(inventoryName);
        inventoryWidth = inventoryInfo.size.x;
        inventoryHeight = inventoryInfo.size.y;
        
        var bpInfo = new Dictionary<Backpack, Vector2Int>();
        if (inventoryInfo.inventoryType == InventoryType.Truck)
        {
            for (var i = 0; i < inventoryInfo.defaultBpNames.Length; i++)
            {
                var bp = inventoryLibrary.GetBackpackData(inventoryInfo.defaultBpNames[i]);
                var pos = inventoryInfo.defaultBpPos[i];
                bpInfo[bp] = pos;
                // Debug.Log("添加背包数据：" + bp.bpName);
                // Debug.Log("背包起始位置：" + pos);
            }

            gridColor = inventoryInfo.gridColor;
            highlightGreen = inventoryInfo.highlightGreen;
            highlightRed = inventoryInfo.highlightRed;

            itemManager.highlightGreen = highlightGreen;
            itemManager.highlightRed = highlightRed;
        }
        else
            bpInfo.Add(inventoryLibrary.GetBackpackData(inventoryInfo.defaultBpName), new Vector2Int(0, 0));
        // Backpack backpackInfo = inventoryLibrary.GetBackpackData(inventoryInfo.defaultBpName);

        // 实例化一个网格
        // 获取对应的panel
        RectTransform oppositePanel;
        switch (inventoryInfo.inventoryType)
        {
            case InventoryType.Truck:
                oppositePanel = leftPanel;
                break;
            case InventoryType.Cabinet:
                oppositePanel = rightPanel;
                break;
            case InventoryType.Pool:
                oppositePanel = rightPanel;
                break;
            default:
                oppositePanel = rightPanel;
                break;
        }

        // 检查是否已经有InventoryInitializer脚本
        var inventoryInitializer = inventoryPanel.GetComponent<InventoryInitializer>();
        if (inventoryInitializer == null)
        {
            // 如果没有，则添加一个新的InventoryInitializer脚本
            inventoryInitializer = inventoryPanel.gameObject.AddComponent<InventoryInitializer>();
        }

        inventoryInitializer.oppositeInitializer = oppositePanel.GetComponent<InventoryInitializer>();
        if (inventoryInitializer.oppositeInitializer == null)
            inventoryInitializer.oppositeInitializer = oppositePanel.gameObject.AddComponent<InventoryInitializer>();

        // 配置InventoryInitializer
        inventoryInitializer.inventoryInfo = inventoryInfo;
        inventoryInitializer.type = inventoryInfo.inventoryType;
        inventoryInitializer.parentPanel = inventoryPanel;
        // inventoryInitializer.backpackInfo = backpackInfo;
        inventoryInitializer.bpDic = bpInfo;
        inventoryInitializer.cells = new CellData[inventoryWidth, inventoryHeight];
        inventoryInitializer.itemManager = itemManager;
        inventoryInitializer.inventoryManager = this;

        if (inventoryInfo.inventoryType is InventoryType.Truck) defaultInitializer = inventoryInitializer;

        // 调用初始化方法
        inventoryInitializer.InitializeInventory(inventoryWidth, inventoryHeight);
    }

    // 创建背包实例（不带网格层信息）
    public void LoadBackpack(GameObject bpInstance, GameObject parentPanel, Backpack bp, RectTransform pos)
    {
        var bpRect = bpInstance.AddComponent<RectTransform>();
        bpInstance.AddComponent<CanvasGroup>();

        bpRect.SetParent(parentPanel.transform, false);

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
        CopyGridLayoutGroupParameters(gridLayoutGroup, bpLayout);
        bpLayout.constraintCount = bp.size.x;
        bpLayout.childAlignment = TextAnchor.MiddleCenter;

        bpInstance.AddComponent<BackpackInteraction>().Initialize(bp, bpInstance, defaultPos, this);
        var bpInc = bpInstance.GetComponent<BackpackInteraction>();
        bpInc.slotObj = new List<GameObject>();

        // 根据InventoryData创建对应背包
        for (var y = 0; y < bp.size.y; y++)
        {
            for (var x = 0; x < bp.size.x; x++)
            {
                var slot = Instantiate(defaultGridObj, bpInstance.transform); // 创建货箱内网格
                slot.name = $"Slot_{x}_{y}";
                var slotImage = slot.GetComponent<Image>();
                slotImage.sprite = bp.gridSprite;
                slotImage.color = Color.white;
                bpInc.slotObj.Add(slot);
            }
        }

        bpRect.sizeDelta = new Vector2(
            bpLayout.cellSize.x * bp.size.x + bpLayout.spacing.x * (bp.size.x - 1) + bpLayout.padding.left * 2,
            bpLayout.cellSize.y * bp.size.y + bpLayout.spacing.y * (bp.size.y - 1) + bpLayout.padding.top * 2
        );

        bpRect.position = pos.position;

        // 添加物理组件
        var rb = bpInstance.AddComponent<Rigidbody2D>();
        // 设置Rigidbody2D属性
        rb.mass = 1.0f; // 质量，适中
        rb.gravityScale = 3.0f; // 重力比例，标准重力
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

        collider.enabled = false;
        rb.bodyType = RigidbodyType2D.Static;

        bpInstance.name = bp.bpName;
        bpInstance.tag = "Backpack";
        // bpInstance.name = $"Backpack_{bp.bpName}";
        bpInc.SetBackpack();

        bpInstance.SetActive(true);
    }

    public void RefreshBackpack()
    {
        // 回合结束，清空物品但保留其他道具
        foreach (var bp in backpacks)
        {
            bp.holdingItems.Clear();
            // bp.backpackData.holdingBuffs.Clear();
            // bp.backpackData.holdingProps.Clear();
            bp.gameObject.GetComponent<BackpackInteraction>().UpdateInnerCells();
        }
    }

    public void CopyGridLayoutGroupParameters(GridLayoutGroup src, GridLayoutGroup dst)
    {
        dst.cellSize = src.cellSize;
        dst.spacing = src.spacing;
        dst.startCorner = src.startCorner;
        dst.startAxis = src.startAxis;
        dst.childAlignment = src.childAlignment;
        dst.constraint = src.constraint;
        dst.constraintCount = src.constraintCount;
        dst.padding = src.padding;
    }

    private Vector2Int defaultPos;

    public void Initialize()
    {
        backpacks = new List<BackpackData>();

        InitializeBorders();

        defaultPos = new Vector2Int(-1, -1);
    }

    public void CreateMyInventory()
    {
        CreateInventory("默认提货点", leftPanel);
        CreateInventory(InventoryName, rightPanel);

        BondInventory();
    }

    private void BondInventory()
    {
        itemManager.backpackHolder = defaultInitializer;
        itemManager.taskItemHolder = defaultInitializer.oppositeInitializer;
    }

    private void InitializeBorders()
    {
        foreach (var border in leftPanelBorders)
        {
            var boxCollider = border.GetComponent<BoxCollider2D>();

            // 根据名字设置偏移值
            switch (border.name)
            {
                case "TopBorder":
                    boxCollider.size = new Vector2(leftPanel.rect.height, 200);
                    boxCollider.offset = new Vector2(0, 100);
                    break;
                case "BottomBorder":
                    boxCollider.size = new Vector2(leftPanel.rect.height, 200);
                    boxCollider.offset = new Vector2(0, -100);
                    break;
                case "LeftBorder":
                    boxCollider.size = new Vector2(200, leftPanel.rect.width);
                    boxCollider.offset = new Vector2(-100, 0);
                    break;
                case "RightBorder":
                    boxCollider.size = new Vector2(200, leftPanel.rect.width);
                    boxCollider.offset = new Vector2(100, 0);
                    break;
            }

            // 创建一个PhysicsMaterial2D并设置其弹性
            var material = new PhysicsMaterial2D();
            material.friction = 1.0f; // 摩擦力

            boxCollider.sharedMaterial = material;
        }
    }

    #region 展示拖拽中货箱信息

    public void ShowDraggingInfo(BackpackData bp)
    {
        if (LevelStateController.Instance.IsAnyDragging())
        {
            var bpInfo = new BackpackInformation(bp);
            OnDraggingBpInfo?.Invoke(bpInfo);
        }
    }

    public void HideDraggingInfo()
    {
        LevelViewController.Instance.ResetDraggingInfo();
    }

    #endregion

    #region 计算实时空格

    public int CalculateRealTimeGrids()
    {
        var grids = 0;
        foreach (var cell in defaultInitializer.cells)
        {
            if (cell.GetCellState() == CellData.State.Packable) grids++;
        }

        return grids;
    }

    #endregion
}

[Serializable]
public class CellData
{
    public enum State
    {
        // 有且只有三种状态
        // 1. Empty: 没有背包也没有物品
        // 2. Packable: 有背包但没有物品
        // 3. Occupied: 有背包也有物品
        Empty,
        Packable,
        Occupied
    }

    // 储存每个格子的信息，一共包含三层
    public GameObject gridObj; // 网格层obj
    public GameObject slotObj; // 背包层obj
    public GameObject blockObj; // 高亮层obj
    public Vector3 worldPosition; // 格子的世界坐标
    public Vector2Int gridPosition; // 网格坐标

    public List<ItemTag> enabledTags; // 该格激活的Tag效果，必须innerItem occupied时有效
    // 用于记录具体某一格激活了哪些效果，只要一个物体占据了这一格，则这个InnerItem激活对应的所有效果
    // ondrag时计算boundary格子
    // dragend之后遍历确定的boundary格子，update tag状态，激活tag

    public ItemData InnerItem
    {
        get => _innerItem;
        set
        {
            _innerItem = value;
            if (value != null)
            {
                _innerBuff = null;
                _innerProp = null;
            }

            UpdateCellState();
        }
    }

    public BuffData InnerBuff
    {
        get => _innerBuff;
        set
        {
            _innerBuff = value;
            if (value != null)
            {
                _innerItem = null;
                _innerProp = null;
            }

            UpdateCellState();
        }
    }

    public PropData InnerProp
    {
        get => _innerProp;
        set
        {
            _innerProp = value;
            if (value != null)
            {
                _innerItem = null;
                _innerBuff = null;
            }

            UpdateCellState();
        }
    }

    public BackpackData innerBp; // 属于哪一个背包

    public CellData(GameObject gridObject, int x, int y, Vector3 position)
    {
        gridObj = gridObject;
        gridPosition = new Vector2Int(x, y);
        worldPosition = position;
        innerBp = null;
        _innerItem = null;
        _innerBuff = null;
        _innerProp = null;
        UpdateCellState();
    }

    public void UpdateCellState()
    {
        // 根据 innerBp 和 innerItem/innerBuff/innerProp 的存在更新状态
        if (innerBp == null && _innerItem == null && _innerBuff == null && _innerProp == null)
            currentState = State.Empty;
        else if (innerBp != null && _innerItem == null && _innerBuff == null && _innerProp == null)
            currentState = State.Packable;
        else if (innerBp != null && (_innerItem != null || _innerBuff != null || _innerProp != null))
            currentState = State.Occupied;
        else
            throw new InvalidOperationException("Invalid level world state");
    }

    public void UpdateBlockObj(Sprite image)
    {
        var blockImg = blockObj.GetComponent<Image>().sprite;
        if (blockImg != image) blockImg = image;
    }

    public State GetCellState()
    {
        // 返回当前状态
        return currentState;
    }

    // public bool isPackable = false;  // 是否被背包占用
    // public bool isOccupied = false;  // 是否被物品占用
    private State currentState;

    private ItemData _innerItem; // 属于哪一个物品
    private BuffData _innerBuff;
    private PropData _innerProp;
}

public class ObjectData
{
    public string displayName; // 显示名
    public string description;
    public GameObject gameObject; // 对应的游戏对象实例
    public bool isPicking; // 物品是否被拿起
    public bool isPlaced; // 物品是否已经放置在背包中
    public string name; // 物品名
    public Vector2Int position; // 物品在背包层中的位置(左上角)
    public Vector2Int[] shape; // 当前形状

    public ObjectInteraction objInc;
}

public class ItemData : ObjectData
{
    public bool isValuable;
    // public List<ItemTag> itemTags;
    public Image itemImage;
    public BuildingType itemSource;
    public GridShape gridShape;
    public ActivateTagDictionary itemTagDic;
    public int itemValue;

    public ItemData(Item template, ItemInteraction itemInc, GameObject obj, Image img, Vector2Int position)
    {
        name = template.itemName;
        objInc = itemInc;
        displayName = template.itemLocalizedName;
        gameObject = obj;
        itemImage = img;
        this.position = position;
        gridShape = template.gridType;
        shape = template.shape;
        itemValue = template.itemValue;
        isPlaced = false;
        isPicking = false;
        isValuable = true;
        description = template.itemLocalizedDescription;
        // this.itemTags = new List<ItemTag>();
        // this.itemTags.AddRange(template.itemTags);
        itemTagDic = new ActivateTagDictionary();

        if (template.itemTags.Length > 0)
        {
            foreach (var tag in template.itemTags)
            {
                itemTagDic[tag] = false;
            }
        }
    }

    public bool isActivated(List<ItemTag> availableTags)
    {
        // 将 availableTags 转换为 HashSet 以便于快速查找
        var availableTagSet = new HashSet<ItemTag>(availableTags);

        // 遍历 itemTagDic 的键，更新每个键的状态
        foreach (var tag in itemTagDic.Keys.ToList())
        {
            itemTagDic[tag] = availableTagSet.Contains(tag);
        }

        // 检查所有标签是否都被激活
        return itemTagDic.Values.All(isActive => isActive);
    }
}

public class PropData : ObjectData
{
    public PropShapeDictionary mapping;
    public int value;
    public int price;

    public PropData(Prop template, GameObject obj)
    {
        name = template.propName;
        displayName = template.propLocalizedName;
        gameObject = obj;
        shape = template.shape;
        mapping = template.propMapping;
        value = template.propValue;
        price = template.propPrice;
        isPlaced = false;
        isPicking = false;
        description = template.propLocalizedDescription;
    }

    public List<ItemTag> GetAllTags()
    {
        var tags = new List<ItemTag>();
        foreach (var cell in mapping)
        {
            if (cell.Value != ItemTag.None && cell.Value != ItemTag.Ordinary) tags.Add(cell.Value);
        }

        return tags;
    }
}

public class BuffData : ObjectData
{
    public BuffEffect effect;
    public bool isEnabled; // buff是否已激活
    public int value;
    public int price;

    public BuffData(Buff template, GameObject obj)
    {
        name = template.buffName;
        displayName = template.buffLocalizedName;
        gameObject = obj;
        shape = template.shape;
        value = template.buffValue;
        price = template.buffPrice;
        effect = template.effect;
        isPlaced = false;
        isPicking = false;
        isEnabled = false;
        description = template.buffLocalizedDescription;
    }
}

public class BackpackData : ObjectData
{
    public List<BuffData> holdingBuffs;
    public List<ItemData> holdingItems;
    public List<PropData> holdingProps;
    public int value;
    public int price;

    public BackpackData(Backpack template, BackpackInteraction bpInc, GameObject obj, Vector2Int position)
    {
        name = template.bpName;
        objInc = bpInc;
        displayName = template.bpLocalizedName;
        gameObject = obj;
        this.position = position;
        shape = template.shape;
        isPlaced = false;
        isPicking = false;
        value = template.bpValue;
        price = template.bpPrice;
        description = template.bpLocalizedDescription;

        holdingItems = new List<ItemData>();
        holdingBuffs = new List<BuffData>();
        holdingProps = new List<PropData>();
    }
}

public enum ScreenMode
{
    // 影响网格大小
    SingleScale,
    DoubleScale
}