using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using cfg.level;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ItemManager : MonoBehaviour
{
    public static Action<ObjectInformation> OnDraggingItemInfo;

    // 定义一个字典来表示tag等级的映射关系
    private static readonly Dictionary<ItemTag, int> TagLevels = new()
    {
        { ItemTag.CoolI, 1 }, { ItemTag.CoolII, 2 }, { ItemTag.CoolIII, 3 },
        { ItemTag.NaturalI, 1 }, { ItemTag.NaturalII, 2 }, { ItemTag.NaturalIII, 3 },
        { ItemTag.FragileI, 1 }, { ItemTag.FragileII, 2 }, { ItemTag.FragileIII, 3 }
    };

    // 定义基本类型的升级路径
    public static readonly Dictionary<ItemEffect, List<ItemTag>> UpgradePaths = new()
    {
        { ItemEffect.Cool, new List<ItemTag> { ItemTag.CoolI, ItemTag.CoolII, ItemTag.CoolIII } },
        { ItemEffect.Natural, new List<ItemTag> { ItemTag.NaturalI, ItemTag.NaturalII, ItemTag.NaturalIII } },
        { ItemEffect.Fragile, new List<ItemTag> { ItemTag.FragileI, ItemTag.FragileII, ItemTag.FragileIII } }
    };

    [Header("全局组件引用")]
    [Space(5)]
    public AioShaderAnimator shaderAnimator;
    public InventoryManager inventoryManager;
    public PropManager propManager;
    public ItemLibrary itemLibrary;
    public InventoryInitializer backpackHolder;
    public InventoryInitializer taskItemHolder;
    public GameObject itemPrefab; // 用于实例化物品的预制件
    public Transform panelPrefab; // 用于放置物品的父对象Panel
    public GameObject rightPanel;
    public Transform abandonedPanel;
    public List<CellData> highlightedCells = new();
    public ItemSpawner itemSpawner;

    [Header("重要参数")]
    [Space(5)]
    public GameObject pickingItem;
    public float gridSize;
    public Color32 highlightGreen;
    public Color32 highlightRed;

    [Header("自身特效相关")]
    [Space(5)]
    [SerializeField]
    private Material itemCoolActivating;
    [SerializeField] private Material itemCoolBefore;
    [SerializeField] private Material itemCoolAfter;
    [SerializeField] private Material itemFragileActivating;
    [SerializeField] private Material itemFragileBefore;
    [SerializeField] private Material itemFragileAfter;
    [SerializeField] private Material itemNaturalActivating;
    [SerializeField] private Material itemNaturalBefore;
    [SerializeField] private Material itemNaturalAfter;
    [SerializeField] private Material itemNormal;

    [Header("新增位置关系特效")]
    [Space(5)]
    [SerializeField] private Material itemNormalActivate;
    [SerializeField] private Material itemCornerBefore;
    [SerializeField] private Material itemCornerAfter;
    [SerializeField] private Material itemHateBefore;
    [SerializeField] private Material itemHateAfter;
    [SerializeField] private Material itemLikeBefore;
    [SerializeField] private Material itemLikeAfter;

    [Header("放置特效相关")]
    [Space(5)]
    [SerializeField]
    private GameObject itemPutDownVFX; // 粒子系统预设

    [Header("重要数据结构")]
    [Space(5)]
    public List<ItemData> items = new();
    // public List<PropData> props = new List<PropData>();
    // public List<BuffData> buffs = new List<BuffData>();
    private Dictionary<string, Item> _itemDictionary;
    private List<ItemData> _currentTaskItems;
    private List<ItemData> _completedTaskItems;
    private List<ItemData> _abandonedTaskItems;

    private Vector2Int _nextPosition = new(4, 4); // 默认开始生成item的位置
    private GameObject _putDownEffectParent; // 管理粒子系统的父对象

    public void Initialize()
    {
        // 初始化物品字典
        _itemDictionary = new Dictionary<string, Item>();
        _currentTaskItems = new List<ItemData>();
        _completedTaskItems = new List<ItemData>();
        _abandonedTaskItems = new List<ItemData>();
        foreach (var itemTemplate in itemLibrary.items)
        {
            _itemDictionary[itemTemplate.itemName] = itemTemplate;
        }

        // continueButton.interactable = false; // 初始化时禁用继续按钮
        // continueButton.onClick.AddListener(ClosePanelsAndContinue);
    }

    public void CreateMyItems()
    {
        foreach (var spawner in itemSpawner.spawnPoints)
        {
            var itemName = spawner.itemIndex;

            if (_itemDictionary.TryGetValue(itemName, out var itemTemplate)) CreateItemInPosition(itemTemplate, itemName, gridSize, spawner);
        }
    }

    public void CreateItemInPosition(Item itemTemplate, string newItemName, float gridSize, SpawnPoint spawnPoint)
    {
        var inventoryInitializer = panelPrefab.GetComponent<InventoryInitializer>();
        var leftPanelItemLayer = panelPrefab.transform.Find("ItemLayer");
        var itemGO = Instantiate(itemPrefab, leftPanelItemLayer);
        ObjUtils.SetParentAndLayer(itemGO, leftPanelItemLayer, InventoryType.Pool);

        // 查找子Obj设置Visual效果
        var itemVisual = itemGO.transform.Find("Visual");
        // 查找子Obj配置自带边框
        var itemFrame = itemGO.transform.Find("Frame");

        // 设置物品的图片
        var itemImage = itemVisual.GetComponent<Image>();
        if (itemImage != null)
        {
            itemImage.sprite = itemTemplate.itemSprite;
            itemImage.material = itemNormal; // 默认材质
            itemImage.alphaHitTestMinimumThreshold = 0.1f; // 设置透明度阈值，0.1 以上才响应点击
        }

        // 计算物品的尺寸
        var rectTransform = itemGO.GetComponent<RectTransform>();
        var frameHolder = itemGO.GetComponent<ItemFrame>();
        var visualRect = itemVisual.GetComponent<RectTransform>();
        var frameRect = itemFrame.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            // 根据物品形状调整尺寸
            var size = CalculateBoundingBox(itemTemplate.shape); // 最小包围盒
            // Debug.Log("当前物品" + itemTemplate.itemName + "占位" + size);
            var newSize = new Vector2(size.x * gridSize, size.y * gridSize);
            rectTransform.sizeDelta = newSize;
            visualRect.sizeDelta = newSize;

            // 调整子级偏移
            var pivotOffset = (newSize - frameRect.sizeDelta) * frameRect.pivot; // 计算 pivot 引起的偏移
            frameRect.sizeDelta = newSize;
            frameRect.anchoredPosition += pivotOffset; // 调整 anchoredPosition

            frameHolder.GenerateFrameGrid(itemTemplate.shape, gridSize);
        }

        // 设置物品位置（默认都是在LeftPanel中生成）
        rectTransform.anchoredPosition = spawnPoint.GetPosition();
        // Debug.Log("当前生成位置：" + inventoryInitializer.cells[nextPosition.x, nextPosition.y].worldPosition);

        // 添加物理组件
        var rb = itemGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        // 设置Rigidbody2D属性
        rb.mass = 1.0f; // 质量，适中
        rb.gravityScale = 0.5f; // 重力比例，标准重力
        rb.drag = 0.5f; // 线性阻尼，减少空气阻力
        rb.angularDrag = 0.3f; // 角度阻尼，减少旋转阻力
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        // rb.freezeRotation = true;

        // 设置初始速度
        // 设置随机初始速度
        // float speed = 5.0f; // 初速度大小
        // float angle = UnityEngine.Random.Range(0f, 2 * Mathf.PI); // 随机角度（弧度）
        // Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
        // rb.velocity = velocity;

        // 设置随机旋转角度
        // var randomAngle = Random.Range(0f, 360f);
        // itemGO.transform.rotation = Quaternion.Euler(0f, 0f, randomAngle);

        // 创建一个PhysicsMaterial2D并设置其弹性
        var material = new PhysicsMaterial2D
        {
            bounciness = 0.5f, // 弹性
            friction = 0.0f // 摩擦力
        };

        // 添加并配置 CompositeCollider2D（用于合并多个 BoxCollider2D）
        var compositeCollider = itemGO.AddComponent<CompositeCollider2D>();
        compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
        compositeCollider.sharedMaterial = material; // 物理材质

        itemGO.AddComponent<ItemInteraction>().Initialize(itemTemplate, itemGO, itemImage, _nextPosition, inventoryInitializer);
        itemGO.tag = "Item";
        // ItemInteraction itemInc = itemGO.GetComponent<ItemInteraction>();
        // itemInc.SetItem();

        itemGO.name = $"Item_{newItemName}";
        itemGO.SetActive(true);
        var item = itemGO.GetComponent<ItemInteraction>().itemData;
        item.name = newItemName;
        items.Add(item);
        _currentTaskItems.Add(item);

        spawnPoint.gameObject.SetActive(false);

        Debug.Log("当前任务已添加物品:" + item.name);
    }

    private void CreateItem(Item itemTemplate, string newItemName, float gridSize)
    {
        var inventoryInitializer = panelPrefab.GetComponent<InventoryInitializer>();
        var leftPanelItemLayer = panelPrefab.transform.Find("ItemLayer");
        var itemGO = Instantiate(itemPrefab, leftPanelItemLayer);
        ObjUtils.SetParentAndLayer(itemGO, leftPanelItemLayer, InventoryType.Pool);

        // 查找子Obj设置Visual效果
        var itemVisual = itemGO.transform.Find("Visual");
        // 查找子Obj配置自带边框
        var itemFrame = itemGO.transform.Find("Frame");

        // 设置物品的图片
        var itemImage = itemVisual.GetComponent<Image>();
        if (itemImage != null)
        {
            itemImage.sprite = itemTemplate.itemSprite;
            itemImage.material = itemNormal; // 默认材质
            itemImage.alphaHitTestMinimumThreshold = 0.1f; // 设置透明度阈值，0.1 以上才响应点击
        }

        // 计算物品的尺寸
        var rectTransform = itemGO.GetComponent<RectTransform>();
        var frameHolder = itemGO.GetComponent<ItemFrame>();
        var visualRect = itemVisual.GetComponent<RectTransform>();
        var frameRect = itemFrame.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            // 根据物品形状调整尺寸
            var size = CalculateBoundingBox(itemTemplate.shape); // 最小包围盒
            // Debug.Log("当前物品" + itemTemplate.itemName + "占位" + size);
            var newSize = new Vector2(size.x * gridSize, size.y * gridSize);
            rectTransform.sizeDelta = newSize;
            visualRect.sizeDelta = newSize;

            // 调整子级偏移
            var pivotOffset = (newSize - frameRect.sizeDelta) * frameRect.pivot; // 计算 pivot 引起的偏移
            frameRect.sizeDelta = newSize;
            frameRect.anchoredPosition += pivotOffset; // 调整 anchoredPosition

            frameHolder.GenerateFrameGrid(itemTemplate.shape, gridSize);
        }

        // 设置物品位置（默认都是在LeftPanel中生成）
        rectTransform.anchoredPosition = inventoryInitializer.cells[_nextPosition.x, _nextPosition.y].worldPosition;
        // Debug.Log("当前生成位置：" + inventoryInitializer.cells[nextPosition.x, nextPosition.y].worldPosition);

        // 添加物理组件
        var rb = itemGO.AddComponent<Rigidbody2D>();
        // 设置Rigidbody2D属性
        rb.mass = 1.0f; // 质量，适中
        rb.gravityScale = 0.5f; // 重力比例，标准重力
        rb.drag = 0.5f; // 线性阻尼，减少空气阻力
        rb.angularDrag = 0.3f; // 角度阻尼，减少旋转阻力
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        // rb.freezeRotation = true;

        // 设置初始速度
        // 设置随机初始速度
        // float speed = 5.0f; // 初速度大小
        // float angle = UnityEngine.Random.Range(0f, 2 * Mathf.PI); // 随机角度（弧度）
        // Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
        // rb.velocity = velocity;

        // 设置随机旋转角度
        var randomAngle = Random.Range(0f, 360f);
        itemGO.transform.rotation = Quaternion.Euler(0f, 0f, randomAngle);

        // 创建一个PhysicsMaterial2D并设置其弹性
        var material = new PhysicsMaterial2D
        {
            bounciness = 0.5f, // 弹性
            friction = 0.0f // 摩擦力
        };

        // 添加并配置 CompositeCollider2D（用于合并多个 BoxCollider2D）
        var compositeCollider = itemGO.AddComponent<CompositeCollider2D>();
        compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
        compositeCollider.sharedMaterial = material; // 物理材质

        itemGO.AddComponent<ItemInteraction>().Initialize(itemTemplate, itemGO, itemImage, _nextPosition, inventoryInitializer);
        itemGO.tag = "Item";
        // ItemInteraction itemInc = itemGO.GetComponent<ItemInteraction>();
        // itemInc.SetItem();

        itemGO.name = $"Item_{newItemName}";
        itemGO.SetActive(true);
        var item = itemGO.GetComponent<ItemInteraction>().itemData;
        item.name = newItemName;
        items.Add(item);
        _currentTaskItems.Add(item);
        Debug.Log("当前任务已添加物品:" + item.name);

        // 更新下一个物品的位置
        _nextPosition.x += 1;
        _nextPosition.y += 1;
        if (_nextPosition.x >= 6)
        {
            // 提行
            _nextPosition.x = 4;
            _nextPosition.y = 3;
        }

        // 追踪位置3秒，防止掉出屏幕范围
        StartCoroutine(TrackItemPosition(itemGO, 3.0f));
    }

    public void LoadPropItem(GameObject propInstance, GameObject parentPanel, Prop propTemplate, RectTransform pos)
    {
        propInstance.AddComponent<CanvasGroup>();

        // 计算物品的尺寸
        var rectTransform = propInstance.AddComponent<RectTransform>();
        rectTransform.SetParent(parentPanel.transform, false);
        if (rectTransform != null)
        {
            var size = CalculateBoundingBox(propTemplate.shape);
            // Debug.Log("当前物品" + itemTemplate.itemName + "占位" + size);
            rectTransform.sizeDelta = new Vector2(size.x * gridSize, size.y * gridSize); // 每个网格单元像素长宽

            // 复制其他属性
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);

            // 设置物品位置
            rectTransform.position = pos.position;
        }

        // 设置物品的图片
        var propImage = propInstance.AddComponent<Image>();
        if (propImage != null && propTemplate.propSprite.Length > 0) propImage.sprite = propTemplate.propSprite[0]; // 设置第一帧图片

        // 设置物品的动态帧，如果有两个以上的帧
        if (propTemplate.propSprite.Length > 1)
        {
            var spriteAnimator = propInstance.AddComponent<SpriteAnimator>();
            spriteAnimator.SetFrames(propTemplate.propSprite); // 添加SpriteAnimator并设置帧动画
        }

        // 添加物理组件
        var rb = propInstance.AddComponent<Rigidbody2D>();
        // 设置Rigidbody2D属性
        rb.mass = 1.0f; // 质量，适中
        rb.gravityScale = 3.0f; // 重力比例，标准重力
        rb.drag = 0.0f; // 线性阻尼，减少空气阻力
        rb.angularDrag = 0.0f; // 角度阻尼，减少旋转阻力
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        // rb.freezeRotation = true;

        // 设置初始速度
        rb.velocity = new Vector2(0f, 5.0f); // 初始速度，右上角方向

        // 创建一个PhysicsMaterial2D并设置其弹性
        var material = new PhysicsMaterial2D();
        material.bounciness = 0.3f; // 弹性
        material.friction = 0.0f; // 摩擦力

        // 添加并配置 PolygonCollider2D
        var collider = propInstance.AddComponent<PolygonCollider2D>();
        collider.sharedMaterial = material;
        SetPolygonColliderPoints(collider, propTemplate.shape);

        collider.enabled = true;
        rb.bodyType = RigidbodyType2D.Static;

        // 单元类交互
        propInstance.AddComponent<PropInteraction>().Initialize(propTemplate, propInstance, this);
        propInstance.tag = "Prop";
        // propInc.inventoryInitializer = panelPrefab.GetComponent<InventoryInitializer>();
        // propInc.canvas = gameCanvas;
        // props.Add(propInc.propData);

        propInstance.name = $"Prop_{propTemplate.propName}";
        propInstance.SetActive(true);
    }

    public void LoadBuffedItem(GameObject buffInstance, GameObject parentPanel, Buff buffTemplate, RectTransform pos)
    {
        buffInstance.AddComponent<CanvasGroup>();

        // 计算物品的尺寸
        var rectTransform = buffInstance.AddComponent<RectTransform>();
        rectTransform.SetParent(parentPanel.transform, false);
        if (rectTransform != null)
        {
            var size = CalculateBoundingBox(buffTemplate.shape);
            // Debug.Log("当前物品" + itemTemplate.itemName + "占位" + size);
            rectTransform.sizeDelta = new Vector2(size.x * gridSize, size.y * gridSize); // 每个网格单元像素长宽

            // 复制其他属性
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);

            // 设置物品位置
            rectTransform.position = pos.position;
        }

        // 设置物品的图片
        var buffImage = buffInstance.AddComponent<Image>();
        if (buffImage != null && buffTemplate.buffSprite.Length > 0) buffImage.sprite = buffTemplate.buffSprite[0]; // 设置第一帧图片

        // 设置物品的动态帧，如果有两个以上的帧
        if (buffTemplate.buffSprite.Length > 1)
        {
            var spriteAnimator = buffInstance.AddComponent<SpriteAnimator>();
            spriteAnimator.SetFrames(buffTemplate.buffSprite); // 添加SpriteAnimator并设置帧动画
        }

        // 添加物理组件
        var rb = buffInstance.AddComponent<Rigidbody2D>();
        // 设置Rigidbody2D属性
        rb.mass = 1.0f; // 质量，适中
        rb.gravityScale = 3.0f; // 重力比例，标准重力
        rb.drag = 0.0f; // 线性阻尼，减少空气阻力
        rb.angularDrag = 0.0f; // 角度阻尼，减少旋转阻力
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        // rb.freezeRotation = true;

        // 设置初始速度
        rb.velocity = new Vector2(0f, 5.0f); // 初始速度，右上角方向

        // 创建一个PhysicsMaterial2D并设置其弹性
        var material = new PhysicsMaterial2D();
        material.bounciness = 0.3f; // 弹性
        material.friction = 1.0f; // 摩擦力

        // 添加并配置 PolygonCollider2D
        var collider = buffInstance.AddComponent<PolygonCollider2D>();
        collider.sharedMaterial = material;
        SetPolygonColliderPoints(collider, buffTemplate.shape);

        collider.enabled = true;
        rb.bodyType = RigidbodyType2D.Static;

        // 配置Buff类交互
        buffInstance.AddComponent<BuffInteraction>().Initialize(buffTemplate, buffInstance, this);

        buffInstance.name = $"Buff_{buffTemplate.buffName}";
        buffInstance.SetActive(true);
    }

    public GridShape GetItemShapeByName(string itemName)
    {
        return itemLibrary.items.Find(item => item.itemName == itemName).gridType;
    }

    public ItemData GetItemDataByName(string itemName)
    {
        // 通过items列表查找
        return items.FirstOrDefault(item => item.name == itemName);
    }

    /// <summary>
    ///     检查每个Item是否被放入
    /// </summary>
    public void CheckCurrentTaskCompletionPerItem()
    {
        var currentTaskCompleted = true;

        // 检查当前任务
        foreach (var item in _currentTaskItems)
        {
            if (!item.isPlaced)
            {
                Debug.Log("当前任务未完成！");
                Debug.Log("以下物品未被放入背包：" + item.name);
                currentTaskCompleted = false;
                _abandonedTaskItems.Add(item);
                item.gameObject.transform.SetParent(abandonedPanel);
                item.gameObject.SetActive(false);
            }
            else
                _completedTaskItems.Add(item);
        }

        // 判断该任务是否已完成
        if (currentTaskCompleted)
        {
            Debug.Log("当前任务完成！");
            // taskManager.CompleteTask(taskManager.currentShowingTask);
        }
        // Debug.Log("当前任务未完成！");
    }

    /// <summary>
    ///     检查当前任务完成情况（零装入、部分装入、全装入）
    /// </summary>
    public int CheckCurrentTaskCompletionAlert()
    {
        var totalCount = _currentTaskItems.Count;
        var placedCount = _currentTaskItems.Count(item => item.isPlaced);

        if (placedCount == totalCount)
            return 2; // 全部装入
        if (placedCount == 0)
            return 0; // 零装入
        return 1; // 部分装入
    }

    /// <summary>
    ///     检查任务完成度
    /// </summary>
    /// <param name="taskData"></param>
    // public void CheckTaskCompletionPerItem(TaskData taskData)
    // {
    //     // 检查当前任务
    //     foreach (var itemName in taskData.taskItems)
    //     {
    //         var item = items.Find(i => i.name == itemName);
    //
    //         if (item == null) return;
    //
    //         if (!item.isPlaced)
    //         {
    //             Debug.Log($"任务 {taskData.taskName} (ID {taskData.taskId}) 未完成！");
    //             Debug.Log("以下物品未被放入背包：" + item.name);
    //             taskData.isCompleted = false;
    //             return;
    //         }
    //     }
    //
    //     Debug.Log($"任务 {taskData.taskName} (ID {taskData.taskId}) 已完成！");
    //     taskData.isCompleted = true;
    // }
    public void ResetItems()
    {
        // 清除背包中的物品
        // foreach (Transform child in rightPanel.transform.Find("ItemLayer"))
        // {
        //     Destroy(child.gameObject);
        // }

        foreach (var item in items)
        {
            Destroy(item.gameObject);
        }

        // 清空items列表和当前任务物品列表
        rightPanel.GetComponent<InventoryInitializer>().ResetCellData();

        items.Clear(); // 所有生成的item
        _completedTaskItems.Clear(); //所有带走的任务item
    }

    public void SetPolygonColliderPoints(PolygonCollider2D cl, Vector2Int[] shape)
    {
        var points = new List<Vector2>();

        foreach (var cell in shape)
        {
            // 左上角
            points.Add(new Vector2(cell.x * gridSize, -cell.y * gridSize));
            // 右上角
            points.Add(new Vector2((cell.x + 1) * gridSize, -cell.y * gridSize));
            // 左下角
            points.Add(new Vector2(cell.x * gridSize, -(cell.y + 1) * gridSize));
            // 右下角
            points.Add(new Vector2((cell.x + 1) * gridSize, -(cell.y + 1) * gridSize));
        }

        // 按顺时针或逆时针顺序排列顶点
        points = SortVertices(points);

        cl.SetPath(0, points.ToArray());
    }

    #region 计算实时金币

    public int CalculateRealTimeValues()
    {
        // 遍历所有网格
        var valuedItems = new List<ItemData>();
        var totalValues = 0;

        foreach (var cell in backpackHolder.cells)
        {
            if (cell.GetCellState() == CellData.State.Occupied && cell.InnerItem != null)
            {
                // Debug.Log("开始检查Item：" + cell.InnerItem.name);
                if (!valuedItems.Contains(cell.InnerItem) && cell.InnerItem.isValuable)
                {
                    // Debug.Log("计入价值：" + cell.InnerItem.itemValue);
                    valuedItems.Add(cell.InnerItem);
                    totalValues += cell.InnerItem.itemValue;
                }
            }
            // Debug.Log("跳过网格！");
        }

        valuedItems.Clear();
        return totalValues;
    }

    #endregion

    private Vector2Int CalculateBoundingBox(Vector2Int[] shape)
    {
        var maxX = 0;
        var maxY = 0;
        foreach (var point in shape)
        {
            if (point.x > maxX) maxX = point.x;
            if (point.y > maxY) maxY = point.y;
        }

        return new Vector2Int(maxX + 1, maxY + 1);
    }

    private List<Vector2> SortVertices(List<Vector2> vertices)
    {
        var center = new Vector2(vertices.Average(v => v.x), vertices.Average(v => v.y));
        return vertices.OrderBy(v => Mathf.Atan2(v.y - center.y, v.x - center.x)).ToList();
    }

    private IEnumerator TrackItemPosition(GameObject item, float duration)
    {
        // 等待1秒再开始追踪
        yield return new WaitForSeconds(1.0f);

        var timePassed = 0f;
        var hasResetPosition = false; // 标志位，表示是否已经重置过位置
        var itemInc = item.GetComponent<ItemInteraction>();
        var itemRect = itemInc.rectTransform;
        var panelRect = itemInc.leftPanelRect;

        while (timePassed < duration)
        {
            // 检查物体是否被拖拽
            if (item.transform.parent != itemInc.leftPanelItemLayer) yield break;

            // 检查物体是否已经被销毁
            if (item == null) yield break; // 物体已被销毁，停止协程

            // 检查物体是否超出范围，并且尚未重置过位置
            if (!RectUtils.IsRectangleInside(panelRect, itemRect) && !hasResetPosition)
            {
                ResetPosition(itemRect);
                hasResetPosition = true; // 设置标志位，表示位置已重置
            }

            timePassed += Time.deltaTime;
            yield return null; // 等待下一帧
        }
    }

    // private bool IsOutOfBounds(RectTransform rectTransform)
    // {
    //     Vector3 screenPoint = Camera.main.WorldToViewportPoint(rectTransform.position);
    //     return screenPoint.x < 0 || screenPoint.x > 1 || screenPoint.y < 0 || screenPoint.y > 1;
    // }


    private void ResetPosition(RectTransform rectTransform)
    {
        rectTransform.anchoredPosition = taskItemHolder.cells[4, 4].worldPosition;
        Debug.Log("物品位置已重置！");
    }

    #region 展示拖拽中物品信息

    public void ShowDraggingInfo(ItemData item)
    {
        if (LevelStateController.Instance.IsAnyDragging())
        {
            var itemInfo = new ItemInformation(item);
            OnDraggingItemInfo?.Invoke(itemInfo);
        }
    }

    public void ShowDraggingInfo(PropData prop)
    {
        if (LevelStateController.Instance.IsAnyDragging())
        {
            var propInfo = new PropInformation(prop);
            OnDraggingItemInfo?.Invoke(propInfo);
        }
    }

    public void ShowDraggingInfo(BuffData buff)
    {
        if (LevelStateController.Instance.IsAnyDragging())
        {
            var buffInfo = new BuffInformation(buff);
            OnDraggingItemInfo?.Invoke(buffInfo);
        }
    }

    public void HideDraggingInfo()
    {
        LevelViewController.Instance.ResetDraggingInfo();
    }

    #endregion

    #region 物品自身特效

    public void UpdateInnerItem()
    {
        foreach (var item in items)
        {
            if (item.isPlaced) HighlightUntaggedItem(item);
        }
    }

    public void HighlightUntaggedItem(ItemData itemData)
    {
        var availableItemEffects = new List<ItemEffect>();
        var availableItemTags = new List<ItemTag>();

        // 只对含有特殊tag的货物生效
        if (itemData.itemTagDic.Keys.Count > 0 && itemData.itemTagDic.Keys.Any(t => t != ItemTag.None && t != ItemTag.Ordinary))
        {
            // 遍历当前item内部的所有网格位置, 检测其接收到的所有效果
            foreach (var offset in itemData.shape)
            {
                var cellX = itemData.position.x + offset.x;
                var cellY = itemData.position.y + offset.y;
                var cell = backpackHolder.cells[cellX, cellY];

                foreach (var tag in propManager.taggedCells.Keys)
                {
                    if (propManager.taggedCells[tag].Contains(cell))
                    {
                        var count = propManager.taggedCells[tag].Count(c => c == cell);
                        for (var i = 0; i < count; i++)
                        {
                            availableItemEffects.AddRange(ConvertToItemEffects(tag));
                        }
                    }
                }
            }

            // switch (availableItemEffects.Count(t => t.Equals(ItemEffect.Cool)))
            // {
            //     case 1:
            //         availableItemTags.Add(ItemTag.CoolI);
            //         break;
            //     case 2:
            //         availableItemTags.Add(ItemTag.CoolII);
            //         break;
            //     case >= 3:
            //         availableItemTags.Add(ItemTag.CoolIII);
            //         break;
            // }

            // 统计基本类型标签的数量
            var itemEffectCount = availableItemEffects.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());

            // 生成结果标签
            var finalTags = new List<ItemTag>();
            foreach (var baseEffect in itemEffectCount.Keys)
            {
                var count = itemEffectCount[baseEffect];
                var path = UpgradePaths[baseEffect];

                // 确定要添加的最高级标签的索引
                var maxIndex = Math.Min(count, path.Count) - 1;

                // 添加从最低级到最高级的所有标签
                for (var i = 0; i <= maxIndex; i++)
                {
                    finalTags.Add(path[i]);
                }
            }

            // Debug.Log("finalTags个数：" + finalTags.Count);
            // foreach (var itemTag in finalTags)
            // {
            //     Debug.Log("遍历：" + itemTag);
            // }

            // // 遍历当前item内部的Tag
            // foreach (ItemTag tag in itemData.itemTagDic.Keys)
            // {
            //     if (tag == ItemTag.None || tag == ItemTag.Ordinary)
            //     {
            //         // isActivated = true;
            //         itemData.itemTagDic[tag] = true;
            //         continue;
            //     }
            //     // 遍历当前item内部的所有网格位置
            //     foreach (Vector2Int offset in itemData.shape)
            //     {
            //         int cellX = itemData.position.x + offset.x;
            //         int cellY = itemData.position.y + offset.y;
            //         CellData cell = inventoryInitializer.oppositeInitializer.cells[cellX, cellY];

            //         if (propManager.taggedCells[tag] != null)
            //         {
            //             if (propManager.taggedCells[tag].Contains(cell))
            //             {
            //                 // isActivated = true;
            //                 itemData.itemTagDic[tag] = true;
            //                 break;
            //             }
            //         }
            //         // isActivated = false;
            //         itemData.itemTagDic[tag] = false;
            //     }
            // }

            // bool allActivated = itemData.itemTagDic.Values.All(value => value);
            var allActivated = itemData.isActivated(finalTags);

            if (!allActivated && itemData.isPlaced)
            {
                itemData.isValuable = false;
                DeactivateItemTag(itemData);
                // AnimatingItemTag(itemData);
                Debug.Log("物品" + itemData.name + "未满足Tag条件！");
            }
            else if (allActivated && itemData.isPlaced)
            {
                itemData.isValuable = true;
                ActivateItemTag(itemData);
                // AnimatingItemTag(itemData);
                Debug.Log("物品" + itemData.name + "已满足Tag条件！");
            }
            else
            {
                itemData.isValuable = false;
                HideItemTag(itemData);
                // AnimatingItemTag(itemData);
                Debug.Log("物品" + itemData.name + "不在Tag检测环境中！");
            }
        }
    }

    public void HideItemTag(ItemData itemData)
    {
        // material.DisableKeyword("OUTBASE_ON");
        itemData.itemImage.material = itemNormal;
    }

    private void AnimatingItemTag(ItemData itemData)
    {
        // Image itemImage = itemData.gameObject.GetComponent<Image>();
        foreach (var tag in itemData.itemTagDic.Keys)
        {
            Material activatingMaterial = null;
            Material afterMaterial = null;

            switch (tag)
            {
                case ItemTag.CoolI:
                case ItemTag.CoolII:
                case ItemTag.CoolIII:
                    activatingMaterial = itemCoolActivating;
                    afterMaterial = itemCoolAfter;
                    break;
                case ItemTag.NaturalI:
                case ItemTag.NaturalII:
                case ItemTag.NaturalIII:
                    activatingMaterial = itemNaturalActivating;
                    afterMaterial = itemNaturalAfter;
                    break;
                case ItemTag.FragileI:
                case ItemTag.FragileII:
                case ItemTag.FragileIII:
                    activatingMaterial = itemFragileActivating;
                    afterMaterial = itemFragileAfter;
                    break;
            }

            if (activatingMaterial != null && afterMaterial != null && itemData.itemImage.material != afterMaterial)
            {
                // 设置激活材质
                itemData.itemImage.material = activatingMaterial;

                // 先进行果冻抖动效果
                itemData.itemImage.transform.DOShakeScale(0.3f, 0.1f).OnComplete(() =>
                {
                    // 果冻抖动完成后触发 Shine 动画
                    itemData.itemImage.material.EnableKeyword("SHINE_ON");
                    shaderAnimator.AnimateShader(itemData.itemImage.material, "_ShineLocation", () =>
                    {
                        // 动画结束后，切换到最终材质
                        itemData.itemImage.material = afterMaterial;
                    });
                });
            }
        }
    }

    private List<ItemEffect> ConvertToItemEffects(ItemTag tag)
    {
        var itemEffects = new List<ItemEffect>();
        if (TagLevels.ContainsKey(tag))
        {
            var baseTag = GetBasePropTag(tag);
            for (var i = 0; i < TagLevels[tag]; i++)
            {
                itemEffects.Add(baseTag);
            }
        }

        return itemEffects;
    }

    private ItemEffect GetBasePropTag(ItemTag tag)
    {
        if (tag.ToString().StartsWith("Cool")) return ItemEffect.Cool;

        if (tag.ToString().StartsWith("Natural")) return ItemEffect.Natural;

        if (tag.ToString().StartsWith("Fragile")) return ItemEffect.Fragile;
        return default;
    }

    private void ActivateItemTag(ItemData itemData)
    {
        AnimatingItemTag(itemData);
    }

    private void DeactivateItemTag(ItemData itemData)
    {
        // 遍历 itemTagDic 中的所有标签
        foreach (var tag in itemData.itemTagDic.Keys)
        {
            Material beforeMaterial = null;

            // 根据不同的标签类型，选择对应的 before 材质
            switch (tag)
            {
                case ItemTag.CoolI:
                case ItemTag.CoolII:
                case ItemTag.CoolIII:
                    beforeMaterial = itemCoolBefore;
                    break;
                case ItemTag.NaturalI:
                case ItemTag.NaturalII:
                case ItemTag.NaturalIII:
                    beforeMaterial = itemNaturalBefore;
                    break;
                case ItemTag.FragileI:
                case ItemTag.FragileII:
                case ItemTag.FragileIII:
                    beforeMaterial = itemFragileBefore;
                    break;
            }

            // 如果找到了对应的 before 材质，执行果冻抖动动画
            if (beforeMaterial != null && itemData.itemImage.material != beforeMaterial)
            {
                // 添加抖动效果
                itemData.itemImage.transform.DOShakeScale(0.3f, 0.1f).OnComplete(() =>
                {
                    // 抖动完成后，切换到原来的 before 材质
                    itemData.itemImage.material = beforeMaterial;
                });

                // 因为已经处理了第一个标签，提前退出循环
                return;
            }
        }
    }

    public void PrintHighlightCells()
    {
        if (highlightedCells.Count == 0)
            Debug.Log("高亮Cell坐标为空！");
        else
        {
            foreach (var cell in highlightedCells)
            {
                // 在这里对每个 CellData 进行处理
                Debug.Log($"高亮Cell坐标: {cell.gridPosition}");
            }
        }
    }

    #endregion

    #region 放置特效

    // 初始化粒子系统
    public void InitializePutDownEffect()
    {
        var inventoryPanel = LevelViewController.Instance.GetInventoryPanel();
        var itemLayer = inventoryPanel.transform.Find("ItemLayer");

        if (itemPutDownVFX != null)
        {
            _putDownEffectParent = Instantiate(itemPutDownVFX, itemLayer); // 将粒子效果挂载到指定层级
            _putDownEffectParent.SetActive(false);
        }
        else
            Debug.LogWarning("Item put down VFX prefab is not assigned.");
    }

    public void StartSingleItemPutDown(GameObject obj)
    {
        var rectTransform = obj.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            // 获取UI元素的中心点（相对于左上角的偏移）
            // var objCenter = RectUtils.GetCenterPosition(rectTransform, gameCanvas);

            // 将粒子系统父对象移动到UI元素的中心位置
            // _putDownEffectParent.transform.position = rectTransform.localPosition;

            var effectRect = _putDownEffectParent.GetComponent<RectTransform>();

            effectRect.anchoredPosition = rectTransform.anchoredPosition;
            effectRect.anchorMin = rectTransform.anchorMin;
            effectRect.anchorMax = rectTransform.anchorMax;
            effectRect.pivot = rectTransform.pivot;
            effectRect.rotation = rectTransform.rotation;
            effectRect.sizeDelta = rectTransform.sizeDelta;

            // 调整层级
            ObjUtils.SetSiblingOrder(obj, _putDownEffectParent);
            _putDownEffectParent.SetActive(true);

            // 重置粒子效果
            ResetExplosionEffect();

            // 触发放置效果
            TriggerPutDownEffect();
        }
        else
            Debug.LogWarning("无法获取物品的RectTransform");
    }

    // 触发粒子效果
    private void TriggerPutDownEffect()
    {
        if (_putDownEffectParent != null)
        {
            var particleSystems = _putDownEffectParent.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                ps.Play(); // 播放粒子系统
            }

            // 启动协程，等待所有粒子系统播放完毕
            StartCoroutine(WaitForEffectToFinish(particleSystems));
        }
        else
            Debug.LogWarning("Particle system parent is not assigned.");
    }

    private void ResetExplosionEffect()
    {
        if (_putDownEffectParent != null)
        {
            var particleSystems = _putDownEffectParent.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                ps.Clear(); // 清除当前的粒子状态
                ps.Stop(); // 确保粒子系统停止
            }
        }
    }

    private IEnumerator WaitForEffectToFinish(ParticleSystem[] particleSystems)
    {
        // 检查是否有粒子系统还在播放
        var anySystemPlaying = true;
        while (anySystemPlaying)
        {
            anySystemPlaying = false;
            foreach (var ps in particleSystems)
            {
                if (ps.isPlaying)
                {
                    anySystemPlaying = true;
                    break; // 如果有粒子系统仍在播放，退出循环
                }
            }

            yield return null; // 等待下一帧
        }

        // 所有粒子系统播放完毕后，禁用父对象
        _putDownEffectParent.SetActive(false);
    }

    #endregion

    #region 喜恶检测

    public void CheckItemRelationships()
    {
        // 遍历所有已放置的物品
        foreach (var item in items)
        {
            if (!item.isPlaced) continue;

            CheckLikeItemRelationship(item);
            CheckHateItemRelationship(item);
            CheckCornerRequirement(item);
        }
    }

    /// <summary>
    ///     重置物品的位置关系效果，用于物品移出背包时
    /// </summary>
    public void ResetItemRelationshipEffects(ItemData itemData)
    {
        if (itemData?.itemImage == null) return;

        // 停止所有正在进行的动画
        itemData.itemImage.transform.DOKill();

        // 重置为正常材质
        itemData.itemImage.material = itemNormal;

        // 重置缩放（防止抖动动画导致的缩放残留）
        itemData.itemImage.transform.localScale = Vector3.one;

        // 停止所有可能正在运行的闪烁协程（通过停止所有协程实现）
        StopAllCoroutines();

        Debug.Log($"已重置物品 {itemData.name} 的位置关系效果");
    }

    /// <summary>
    ///     检查喜欢物品的位置关系 - 必须相邻
    /// </summary>
    private void CheckLikeItemRelationship(ItemData itemData)
    {
        // 如果没有指定喜欢的物品，跳过检测
        if (string.IsNullOrEmpty(itemData.likeItemName)) return;

        // 获取当前物品占据的所有网格位置
        var currentItemCells = GetItemOccupiedCells(itemData);

        // 查找指定名称的喜欢物品
        var likedItem = items.FirstOrDefault(item =>
            item.name == itemData.likeItemName && item.isPlaced);

        if (likedItem == null)
        {
            // 喜欢的物品不存在或未放置，这违反了"必须相邻"的规则
            Debug.Log($"违反喜欢规则：物品 {itemData.name} 需要的喜欢物品 {itemData.likeItemName} 未放置或不存在！");
            TriggerLikeRelationshipViolationEffect(itemData);
            return;
        }

        // 获取喜欢物品占据的所有网格位置
        var likedItemCells = GetItemOccupiedCells(likedItem);

        // 检查是否相邻（任意一个网格相邻即可）
        var isAdjacent = IsItemsOrthogonallyAdjacent(currentItemCells, likedItemCells);

        if (!isAdjacent)
        {
            Debug.Log($"违反喜欢规则：物品 {itemData.name} 必须与 {itemData.likeItemName} 相邻放置！");
            // 触发喜欢关系违反的动画效果
            TriggerLikeRelationshipViolationEffect(itemData);
        }
        else
        {
            Debug.Log($"满足喜欢规则：物品 {itemData.name} 与 {itemData.likeItemName} 正确相邻放置！");
            // 触发喜欢关系满足的动画效果
            TriggerLikeRelationshipSatisfiedEffect(itemData);
        }
    }

    /// <summary>
    ///     检查讨厌物品的位置关系 - 不能相邻
    /// </summary>
    private void CheckHateItemRelationship(ItemData itemData)
    {
        // 如果没有指定讨厌的物品，跳过检测
        if (string.IsNullOrEmpty(itemData.hateItemName)) return;

        // 获取当前物品占据的所有网格位置
        var currentItemCells = GetItemOccupiedCells(itemData);

        // 查找指定名称的讨厌物品
        var hatedItem = items.FirstOrDefault(item =>
            item.name == itemData.hateItemName && item.isPlaced);

        if (hatedItem == null)
        {
            // 讨厌的物品不存在或未放置，不违反规则
            return;
        }

        // 获取讨厌物品占据的所有网格位置
        var hatedItemCells = GetItemOccupiedCells(hatedItem);

        // 检查是否相邻（任意一个网格相邻都违反规则）
        var isAdjacent = IsItemsOrthogonallyAdjacent(currentItemCells, hatedItemCells);

        if (isAdjacent)
        {
            Debug.Log($"违反讨厌规则：物品 {itemData.name} 不能与 {itemData.hateItemName} 相邻放置！");
            // 触发讨厌关系违反的动画效果
            TriggerHateRelationshipViolationEffect(itemData);
        }
        else
        {
            Debug.Log($"满足讨厌规则：物品 {itemData.name} 与 {itemData.hateItemName} 保持安全距离！");
            // 触发讨厌关系满足的动画效果
            TriggerHateRelationshipSatisfiedEffect(itemData);
        }
    }

    /// <summary>
    ///     获取物品占据的所有网格位置
    /// </summary>
    private List<Vector2Int> GetItemOccupiedCells(ItemData itemData)
    {
        var occupiedCells = new List<Vector2Int>();

        foreach (var offset in itemData.shape)
        {
            var cellPosition = new Vector2Int(
                itemData.position.x + offset.x,
                itemData.position.y + offset.y
            );
            occupiedCells.Add(cellPosition);
        }

        return occupiedCells;
    }

    /// <summary>
    ///     检查角落需求 - 需要占据四角之一
    /// </summary>
    private void CheckCornerRequirement(ItemData itemData)
    {
        // 如果不需要角落位置，跳过检测
        if (!itemData.needCorner) return;

        // 获取网格的尺寸
        var gridWidth = backpackHolder.cells.GetLength(0);
        var gridHeight = backpackHolder.cells.GetLength(1);

        // 定义四个角落位置
        var corners = new List<Vector2Int>
        {
            new(0, 0), // 左上角
            new(gridWidth - 1, 0), // 右上角
            new(0, gridHeight - 1), // 左下角
            new(gridWidth - 1, gridHeight - 1) // 右下角
        };

        // 获取当前物品占据的所有网格位置
        var occupiedCells = GetItemOccupiedCells(itemData);

        // 检查是否占据了任何一个角落
        var occupiesCorner = false;
        var occupiedCorner = Vector2Int.zero;

        foreach (var corner in corners)
        {
            if (occupiedCells.Contains(corner))
            {
                occupiesCorner = true;
                occupiedCorner = corner;
                break;
            }
        }

        if (!occupiesCorner)
        {
            Debug.Log($"违反角落规则：物品 {itemData.name} 需要放置在角落位置！当前位置不满足要求。");
            // 触发角落关系违反的动画效果
            TriggerCornerRelationshipViolationEffect(itemData);
        }
        else
        {
            var cornerName = GetCornerName(occupiedCorner, gridWidth, gridHeight);
            Debug.Log($"满足角落规则：物品 {itemData.name} 正确占据了{cornerName}！");
            // 触发角落关系满足的动画效果
            TriggerCornerRelationshipSatisfiedEffect(itemData);
        }
    }

    /// <summary>
    ///     触发喜欢关系满足的动画效果
    /// </summary>
    private void TriggerLikeRelationshipSatisfiedEffect(ItemData itemData)
    {
        if (itemData.itemImage == null) return;

        // 设置激活材质
        itemData.itemImage.material = itemNormalActivate;

        // 先进行轻微的果冻抖动效果（表示确认）
        itemData.itemImage.transform.DOShakeScale(0.3f, 0.1f).OnComplete(() =>
        {
            // 果冻抖动完成后触发 Shine 动画
            itemData.itemImage.material.EnableKeyword("SHINE_ON");
            shaderAnimator.AnimateShader(itemData.itemImage.material, "_ShineLocation", () =>
            {
                // 动画结束后，切换到喜欢关系的最终材质
                itemData.itemImage.material = itemLikeAfter;
            });
        });

        // 标记物品为合规状态
        itemData.isValuable = true;
    }

    /// <summary>
    ///     触发喜欢关系违反的动画效果
    /// </summary>
    private void TriggerLikeRelationshipViolationEffect(ItemData itemData)
    {
        if (itemData.itemImage == null) return;

        // 先进行果冻抖动效果（更强烈的抖动表示警告）
        itemData.itemImage.transform.DOShakeScale(0.5f, 0.2f).OnComplete(() =>
        {
            // 抖动完成后设置违反规则的材质
            itemData.itemImage.material = itemLikeBefore;

            // 添加闪烁效果
            StartCoroutine(FlashEffect(itemData.itemImage, itemLikeBefore, 3, 0.3f));
        });

        // 标记物品为不合规状态
        itemData.isValuable = false;
    }

    /// <summary>
    ///     触发讨厌关系满足的动画效果
    /// </summary>
    private void TriggerHateRelationshipSatisfiedEffect(ItemData itemData)
    {
        if (itemData.itemImage == null) return;

        // 设置激活材质
        itemData.itemImage.material = itemNormalActivate;

        // 先进行轻微的果冻抖动效果（表示确认）
        itemData.itemImage.transform.DOShakeScale(0.3f, 0.1f).OnComplete(() =>
        {
            // 果冻抖动完成后触发 Shine 动画
            itemData.itemImage.material.EnableKeyword("SHINE_ON");
            shaderAnimator.AnimateShader(itemData.itemImage.material, "_ShineLocation", () =>
            {
                // 动画结束后，切换到讨厌关系的最终材质
                itemData.itemImage.material = itemHateAfter;
            });
        });

        // 标记物品为合规状态
        itemData.isValuable = true;
    }

    /// <summary>
    ///     触发讨厌关系违反的动画效果
    /// </summary>
    private void TriggerHateRelationshipViolationEffect(ItemData itemData)
    {
        if (itemData.itemImage == null) return;

        // 先进行果冻抖动效果（更强烈的抖动表示警告）
        itemData.itemImage.transform.DOShakeScale(0.5f, 0.2f).OnComplete(() =>
        {
            // 抖动完成后设置违反规则的材质
            itemData.itemImage.material = itemHateBefore;

            // 添加闪烁效果
            StartCoroutine(FlashEffect(itemData.itemImage, itemHateBefore, 3, 0.3f));
        });

        // 标记物品为不合规状态
        itemData.isValuable = false;
    }

    /// <summary>
    ///     触发角落关系满足的动画效果
    /// </summary>
    private void TriggerCornerRelationshipSatisfiedEffect(ItemData itemData)
    {
        if (itemData.itemImage == null) return;

        // 设置激活材质
        itemData.itemImage.material = itemNormalActivate;

        // 先进行轻微的果冻抖动效果（表示确认）
        itemData.itemImage.transform.DOShakeScale(0.3f, 0.1f).OnComplete(() =>
        {
            // 果冻抖动完成后触发 Shine 动画
            itemData.itemImage.material.EnableKeyword("SHINE_ON");
            shaderAnimator.AnimateShader(itemData.itemImage.material, "_ShineLocation", () =>
            {
                // 动画结束后，切换到角落关系的最终材质
                itemData.itemImage.material = itemCornerAfter;
            });
        });

        // 标记物品为合规状态
        itemData.isValuable = true;
    }

    /// <summary>
    ///     触发角落关系违反的动画效果
    /// </summary>
    private void TriggerCornerRelationshipViolationEffect(ItemData itemData)
    {
        if (itemData.itemImage == null) return;

        // 先进行果冻抖动效果（更强烈的抖动表示警告）
        itemData.itemImage.transform.DOShakeScale(0.5f, 0.2f).OnComplete(() =>
        {
            // 抖动完成后设置违反规则的材质
            itemData.itemImage.material = itemCornerBefore;

            // 添加闪烁效果
            StartCoroutine(FlashEffect(itemData.itemImage, itemCornerBefore, 3, 0.3f));
        });

        // 标记物品为不合规状态
        itemData.isValuable = false;
    }

    /// <summary>
    ///     闪烁效果协程
    /// </summary>
    private IEnumerator FlashEffect(Image itemImage, Material baseMaterial, int flashCount, float flashInterval)
    {
        for (var i = 0; i < flashCount; i++)
        {
            // 切换到正常材质
            itemImage.material = itemNormal;
            yield return new WaitForSeconds(flashInterval);

            // 切换回警告材质
            itemImage.material = baseMaterial;
            yield return new WaitForSeconds(flashInterval);
        }
    }

    /// <summary>
    ///     获取角落名称用于调试输出
    /// </summary>
    private string GetCornerName(Vector2Int corner, int gridWidth, int gridHeight)
    {
        if (corner.x == 0 && corner.y == 0)
            return "左上角";
        if (corner.x == gridWidth - 1 && corner.y == 0)
            return "右上角";
        if (corner.x == 0 && corner.y == gridHeight - 1)
            return "左下角";
        if (corner.x == gridWidth - 1 && corner.y == gridHeight - 1)
            return "右下角";
        return "未知角落";
    }

    /// <summary>
    ///     检查两个物品是否仅在正交方向相邻（不包括对角）
    /// </summary>
    private bool IsItemsOrthogonallyAdjacent(List<Vector2Int> cells1, List<Vector2Int> cells2)
    {
        foreach (var cell1 in cells1)
        {
            foreach (var cell2 in cells2)
            {
                var deltaX = Mathf.Abs(cell1.x - cell2.x);
                var deltaY = Mathf.Abs(cell1.y - cell2.y);

                // 只考虑正交相邻：要么x差1且y差0，要么x差0且y差1
                if ((deltaX == 1 && deltaY == 0) || (deltaX == 0 && deltaY == 1)) return true;
            }
        }

        return false;
    }

    #endregion
}