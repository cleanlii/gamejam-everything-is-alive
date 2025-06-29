using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using cfg.level;
using I2.Loc;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemLibrary", menuName = "Library/Item")]
public class ItemLibrary : ScriptableObject
{
    [SerializeField] private bool update;
    [SerializeField] private string spritePath;
    public List<Item> items;
    public List<Prop> props;
    public List<Buff> buffs;
    public List<Tag> tags;
    public List<BlockTypeData> blocks;

    public Item GetItemTemplate(string name)
    {
        var item = items.Find(i => i.itemName == name);
        if (item == null) Debug.LogWarning($"Item with name '{name}' not found.");
        return item;
    }

    public Prop GetPropTemplate(string name)
    {
        var prop = props.Find(p => p.propName == name);
        if (prop == null) Debug.LogWarning($"Prop with name '{name}' not found.");
        return prop;
    }

    public Buff GetBuffData(string name)
    {
        var buff = buffs.Find(b => b.buffName == name);
        if (buff == null) Debug.LogWarning($"Buff with name '{name}' not found.");
        return buff;
    }

    public Tag GetTagData(ItemTag itemTag)
    {
        // 每个tag与图标为唯一对应关系
        var tag = tags.Find(t => t.tagRange.Contains(itemTag));
        if (tag == null) Debug.LogWarning($"Tag with ItemTag '{itemTag}' not found.");
        return tag;
    }

    // private void OnEnable()
    // {
    //     items = new List<ItemTemplate>();

    //     // 添加特殊形状物品模板
    //     // TODO: 物品模版可以在Lib文件中手动创建、也可以在这里提前写构造函数
    //     var zShape = CreateShape(
    //         "011",
    //         "010",
    //         "110"
    //     );
    //     var lShape = CreateShape(
    //         "10",
    //         "10",
    //         "11"
    //     );

    //     // items.Add(new ItemTemplate("Z形快递盒", ItemType.Ordinary, Resources.Load<Sprite>(""), zShape));
    //     // items.Add(new ItemTemplate("L形快递盒", ItemType.Ordinary, Resources.Load<Sprite>(""), lShape));
    // }

    #region 读取表格并初始化

    /// <summary>
    ///     从表格中获取Item数据
    /// </summary>
    public void InitializeItem()
    {
    }

    /// <summary>
    ///     从表格中获取Prop数据
    /// </summary>
    public void InitializeProp()
    {
    }

    // 创建新的物品
    private Item CreateNewItem(ItemTemplate item)
    {
        return new Item(item.itemTags)
        {
            itemName = item.itemName,
            itemSource = item.itemSource,
            gridType = item.shape,
            shape = GridShapeCalculator.GetShapeByType(item.shape),
            itemSize = item.itemSize,
            itemValue = item.itemValue,
            itemLocalizedName = "ItemName/" + item.itemLocalization.localizedName,
            itemLocalizedDescription = "ItemDescription/" + item.itemLocalization.localizedDes
        };
    }

    // 更新已有的 Item，如果有变更则返回 true
    private bool UpdateItem(Item existingItem, ItemTemplate newItem)
    {
        var updated = false; // 标记是否有更新

        // 比较 shape 数组是否发生变化
        var newShape = GridShapeCalculator.GetShapeByType(newItem.shape);
        if (!RectUtils.ShapesAreEqual(existingItem.shape, newShape))
        {
            Debug.Log($"更新 {existingItem.itemName} 的形状");
            existingItem.shape = newShape;
            updated = true;
        }

        if (existingItem.itemSprite == null)
        {
            Debug.LogWarning(existingItem.itemName + "暂无图片！");
            // 从 Resources 文件夹加载所有 Sprite
            var sprites = Resources.LoadAll<Sprite>(spritePath);

            foreach (var sprite in sprites)
            {
                // 获取 Sprite 的文件名，移除可能的路径和扩展名
                var spriteName = Path.GetFileNameWithoutExtension(sprite.name);

                // 判断文件名是否以 _itemName 结尾
                if (spriteName.EndsWith($"{existingItem.itemName}"))
                {
                    existingItem.itemSprite = sprite;
                    Debug.Log($"更新 {existingItem.itemName} 的图片！");
                    updated = true;
                }
            }
        }

        if (existingItem.itemSize != newItem.itemSize)
        {
            Debug.Log($"更新 {existingItem.itemName} 的尺寸：{existingItem.itemSize} -> {newItem.itemSize}");
            existingItem.itemSize = newItem.itemSize;
            updated = true;
        }

        if (existingItem.itemValue != newItem.itemValue)
        {
            Debug.Log($"更新 {existingItem.itemName} 的值：{existingItem.itemValue} -> {newItem.itemValue}");
            existingItem.itemValue = newItem.itemValue;
            updated = true;
        }

        if (existingItem.gridType != newItem.shape)
        {
            Debug.Log($"更新 {existingItem.gridType} 的值：{existingItem.gridType} -> {newItem.shape}");
            existingItem.gridType = newItem.shape;
            updated = true;
        }

        // 更新全部本地化
        // Debug.Log($"更新 {existingItem.itemName} 的本地化名称索引");
        // existingItem.itemLocalizedName = "ItemName/" + newItem.itemLocalization.localizedName;
        // updated = true;
        //
        // Debug.Log($"更新 {existingItem.itemName} 的本地化描述索引");
        // existingItem.itemLocalizedDescription = "ItemDescription/" + newItem.itemLocalization.localizedDes;
        // updated = true;

        return updated;
    }

    // 创建新的 Prop
    private Prop CreateNewProp(PropTemplate prop)
    {
        return new Prop
        {
            propName = prop.propName,
            shape = GridShapeCalculator.GetShapeByType(prop.shape),
            propSize = prop.propSize,
            propPrice = prop.propPrice,
            propValue = prop.propValue,
            propLocalizedName = "Prop/" + prop.propLocalization.localizedName,
            propLocalizedDescription = "Prop/" + prop.propLocalization.localizedDes
        };
    }

    // 更新已有的 Prop，如果有任何属性发生变更则返回 true
    private bool UpdateProp(Prop existingProp, PropTemplate newProp)
    {
        var updated = false; // 标记是否有更新

        // 比较 shape 数组是否发生变化
        var newShape = GridShapeCalculator.GetShapeByType(newProp.shape);
        if (!RectUtils.ShapesAreEqual(existingProp.shape, newShape))
        {
            Debug.Log($"更新 {existingProp.propName} 的形状");
            existingProp.shape = newShape;
            updated = true;
        }

        if (existingProp.propSize != newProp.propSize)
        {
            Debug.Log($"更新 {existingProp.propName} 的尺寸：{existingProp.propSize} -> {newProp.propSize}");
            existingProp.propSize = newProp.propSize;
            updated = true;
        }

        if (existingProp.propPrice != newProp.propPrice)
        {
            Debug.Log($"更新 {existingProp.propName} 的价格：{existingProp.propPrice} -> {newProp.propPrice}");
            existingProp.propPrice = newProp.propPrice;
            updated = true;
        }

        if (existingProp.propValue != newProp.propValue)
        {
            Debug.Log($"更新 {existingProp.propName} 的值：{existingProp.propValue} -> {newProp.propValue}");
            existingProp.propValue = newProp.propValue;
            updated = true;
        }

        // 全部更新本地化设置
        // Debug.Log($"更新 {existingProp.propName} 的本地化名称索引");
        // existingProp.propLocalizedName = "Prop/" + newProp.propLocalization.localizedName;
        // updated = true;
        //
        // Debug.Log($"更新 {existingProp.propName} 的本地化描述索引");
        // existingProp.propLocalizedDescription = "Prop/" + newProp.propLocalization.localizedDes;
        // updated = true;

        return updated;
    }

    #endregion
}

[Serializable]
public class Item
{
    public string itemName;
    public Sprite itemSprite;
    public BuildingType itemSource; // 可能的所有来源，默认唯一
    public GridShape gridType;
    public Vector2Int[] shape; // 使用Vector2Int数组来表示形状
    public int itemSize; // 物品占据格数
    public int itemValue; // 物品价值
    public LocalizedString itemLocalizedName;
    public LocalizedString itemLocalizedDescription; // 物品描述
    public ItemTag[] itemTags;
    public string likeItemName;
    public string hateItemName;
    public bool needCorner;
    public Sprite satisfiedSprite;
    public Sprite unsatisfiedSprite;

    // public Item(string name, ItemTag[] tags, Sprite sprite, Vector2Int[] shape)
    // {
    //     this.itemName = name;
    //     this.itemTags = tags;
    //     this.itemSprite = sprite;
    //     this.shape = shape;
    //     this.itemSize = shape.Length;
    //     this.itemValue = CalculateItemValue();
    // }

    public Item(ItemTag itemTag)
    {
        itemTags = new ItemTag[1];
        itemTags[0] = itemTag;
    }

    public Item(ItemTag[] itemTags)
    {
        this.itemTags = itemTags;
    }

    public void SetItemTags(ItemTag itemTag)
    {
        itemTags = new ItemTag[1];
        itemTags[0] = itemTag;
    }

    private int CalculateItemValue()
    {
        var tagValueDict = new Dictionary<ItemTag, int>
        {
            { ItemTag.Ordinary, 2 }, // 默认基础价值
            { ItemTag.CoolI, 6 }, { ItemTag.NaturalI, 6 }, { ItemTag.FragileI, 6 },
            { ItemTag.CoolII, 10 }, { ItemTag.NaturalII, 10 }, { ItemTag.FragileII, 10 },
            { ItemTag.CoolIII, 14 }, { ItemTag.NaturalIII, 14 }, { ItemTag.FragileIII, 14 }
        };

        var tagValue = 0;
        // 计算所有标签的值
        foreach (var tag in itemTags)
        {
            if (tagValueDict.ContainsKey(tag)) tagValue += tagValueDict[tag];
        }

        return itemSize + tagValue;
    }
}


[Serializable]
public class Prop
{
    public string propName; // 道具名称
    public Sprite[] propSprite; // 道具整体图案
    public PropShapeDictionary propMapping;
    // public ItemTag[] propTags;  // 功能总和（如需要可用于计算价值）
    public Vector2Int[] shape; // 总体形状（用于放置检测）
    public int propSize; // 道具大小
    public int propValue; // 道具价值
    public int propPrice; // 商店价格
    public LocalizedString propLocalizedName;
    public LocalizedString propLocalizedDescription; // 物品描述

    // private ItemLibrary database;  // 引用PropUnit数据库

    // public Prop(string name, Sprite sprite, PropShapeDictionary mapping)
    // {
    //     this.propName = name;
    //     this.propSprite[0] = sprite;
    //     this.propMapping = mapping;
    //     // this.database = db;
    //     this.shape = CalculateShape();
    //     // this.propTags = CalculatePropTags();
    // }

    private Vector2Int[] CalculateShape()
    {
        return propMapping.Keys.ToArray();
    }

    // private ItemTag[] CalculatePropTags()
    // {
    //     // 获取所有PropUnit名称，转换为PropUnit对象，然后平展和去重它们的ItemTags
    //     var unitTags = propMapping.Values
    //         .Select(unitName => database.units.FirstOrDefault(unit => unit.unitName == unitName)?.unitTag)
    //         .Where(tags => tags != null)
    //         .SelectMany(tags => tags)
    //         .Distinct()
    //         .ToArray();

    //     return unitTags;
    // }
}

[Serializable]
public class Buff
{
    public string buffName; // Buff名称
    // public string description;      // Buff描述
    public Sprite[] buffSprite;
    public BuffEffect effect;
    public Vector2Int[] shape;
    public int buffValue;
    public int buffPrice; // 商店价格
    public LocalizedString buffLocalizedName;
    public LocalizedString buffLocalizedDescription; // 物品描述

    // 使用字典来存储不同的modifier
    public Dictionary<string, int> modifiers;

    public Buff()
    {
        modifiers = new Dictionary<string, int>();
    }

    // 方法来设置固定值
    public void SetFixedValues()
    {
        switch (effect)
        {
            case BuffEffect.EnergyI:
                modifiers["energy"] = 3;
                break;
            case BuffEffect.EnergyII:
                modifiers["energy"] = 6;
                break;
            case BuffEffect.EnergyIII:
                modifiers["energy"] = 9;
                break;
            case BuffEffect.GoldI:
                modifiers["gold"] = 5;
                break;
            // 根据需要添加其他类型和固定值
        }
    }
}

[Serializable]
public class Tag
{
    public string tagName;
    public List<ItemTag> tagRange;
    public Sprite enabledIcon;
    public Sprite disabledIcon;
}

[Serializable]
public class BlockTypeData
{
    public string name;
    public GoodsType belong;
    public BlockType type;
    public List<BlockTexture> textures;
}

// public enum ItemTag
// {
//     None, Ordinary,
//     CoolI, NaturalI, FragileI,
//     CoolII, NaturalII, FragileII,
//     CoolIII, NaturalIII, FragileIII
// }

public enum ItemEffect
{
    Cool,
    Natural,
    Fragile
}

public enum BuffEffect
{
    None,
    EnergyI,
    EnergyII,
    EnergyIII,
    GoldI
}

public enum BlockType
{
    Empty,
    Decorative,
    Functional
}

[Serializable]
public class BlockTexture
{
    public string name;
    public Texture2D texture;
    public ItemTag associatedItemTag;
    public BuffEffect associatedBuffEffect;
}