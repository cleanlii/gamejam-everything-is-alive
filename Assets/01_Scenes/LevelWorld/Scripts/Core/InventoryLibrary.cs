using System;
using System.Collections.Generic;
using cfg.level;
using I2.Loc;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryLibrary", menuName = "Library/Inventory")]
public class InventoryLibrary : ScriptableObject
{
    [SerializeField] private bool initialize;
    public List<Inventory> inventories;
    public List<Backpack> backpacks;

    public void InitializeBackpack()
    {
    }

    public Inventory GetInventoryData(string name)
    {
        return inventories.Find(inventory => inventory.inventoryName == name);
    }

    public Backpack GetBackpackData(string name)
    {
        return backpacks.Find(backpack => backpack.bpName == name);
    }

    // 创建新的 Backpack
    private Backpack CreateNewBackpack(BackpackTemplate bp)
    {
        Debug.Log($"正在创建新 Backpack：{bp.bpName}");
        return new Backpack
        {
            bpName = bp.bpName,
            shape = GridShapeCalculator.GetShapeByType(bp.shape),
            size = bp.bpSize,
            bpValue = bp.bpValue,
            bpPrice = bp.bpPrice,
            bpLocalizedName = "Backpack/" + bp.bpLocalization.localizedName,
            bpLocalizedDescription = "Backpack/" + bp.bpLocalization.localizedDes
        };
    }

    // 更新已有的 Backpack，如果有变更则返回 true
    private bool UpdateBackpack(Backpack existingBp, BackpackTemplate newBp)
    {
        var updated = false; // 标记是否有更新

        // 比较 shape 数组是否发生变化
        var newShape = GridShapeCalculator.GetShapeByType(newBp.shape);
        if (!RectUtils.ShapesAreEqual(existingBp.shape, newShape))
        {
            Debug.Log($"更新 {existingBp.bpName} 的形状");
            existingBp.shape = newShape;
            updated = true;
        }

        if (existingBp.size != newBp.bpSize)
        {
            Debug.Log($"更新 {existingBp.bpName} 的尺寸：{existingBp.size} -> {newBp.bpSize}");
            existingBp.size = newBp.bpSize;
            updated = true;
        }

        if (existingBp.bpPrice != newBp.bpPrice)
        {
            Debug.Log($"更新 {existingBp.bpName} 的价格：{existingBp.bpPrice} -> {newBp.bpPrice}");
            existingBp.bpPrice = newBp.bpPrice;
            updated = true;
        }

        if (existingBp.bpValue != newBp.bpValue)
        {
            Debug.Log($"更新 {existingBp.bpName} 的值：{existingBp.bpValue} -> {newBp.bpValue}");
            existingBp.bpValue = newBp.bpValue;
            updated = true;
        }

        // 全部更新本地化设置
        // Debug.Log($"更新 {existingBp.bpName} 的本地化名称");
        // existingBp.bpLocalizedName = "Backpack/" + newBp.bpLocalization.localizedName;
        // updated = true;
        //
        // Debug.Log($"更新 {existingBp.bpName} 的本地化描述");
        // existingBp.bpLocalizedDescription = "Backpack/" + newBp.bpLocalization.localizedDes;
        // updated = true;

        return updated;
    }
}

[Serializable]
public class Inventory
{
    // 这里因为用列表是因为字典结构不能可视化编辑

    // 储存「车辆」信息
    public string inventoryName; // 网格名称
    public InventoryType inventoryType;
    public Sprite bgSprite; // 网格层背景图片
    public Sprite gridSprite; // 网格层单格图片
    public Sprite highlightSprite; // 高亮层单格图片
    public Color32 gridColor; // 网格层单格颜色
    public Color32 highlightGreen;
    public Color32 highlightRed;
    public Vector2Int size; // 网格尺寸
    public string defaultBpName; // 默认自带背包名
    public string[] defaultBpNames; // 默认自带背包名
    public Vector2Int[] defaultBpPos; // 默认背包起始位置（一一对应）
}

[Serializable]
public class Backpack
{
    // 储存「货箱」信息
    public string bpName; // 背包的名称
    public Sprite bgSprite; // 背包背景图片
    public Sprite gridSprite; // 背包单格图片
    public Vector2Int[] shape; // 背包的形状
    public Vector2Int size; // 背包的尺寸（网格数量）
    public int bpValue; // 背包价值
    public int bpPrice; // 商店价格
    public LocalizedString bpLocalizedName;
    public LocalizedString bpLocalizedDescription; // 物品描述
}

public enum InventoryType
{
    Cabinet,
    Pool,
    Truck,
    Store
}