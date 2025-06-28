using System;
using System.Collections.Generic;
using System.Linq;
using cfg.level;
using PackageGame.Level.Gameplay;
using UnityEngine;

public class InfoPanelController : MonoBehaviour
{
    public static event Action<ObjectInformation> OnObjInfoPanelOpen;
    public static event Action OnTaskInfoPanelClose;
    public static event Action OnStoreInfoPanelClose;
    public static event Action OnEventInfoPanelClose;
    public static event Action OnObjInfoPanelClose;

    private static InfoPanelController _instance;

    public static InfoPanelController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<InfoPanelController>();
                if (_instance == null) Debug.LogError("No InfoPanelController found in the scene.");
            }

            return _instance;
        }
    }

    private void Awake()
    {
        // 检查是否已有实例存在且不是当前实例
        if (_instance != null && _instance != this)
            Destroy(gameObject);
        else
        {
            _instance = this;
            // 不要在场景切换时保留
            // DontDestroyOnLoad(gameObject);
        }
    }
    
    #region 查看物品信息

    public void OpenInfoPanel(ItemData item)
    {
        if (!LevelStateController.Instance.IsAnyDragging())
        {
            var itemInfo = new ItemInformation(item);
            OnObjInfoPanelOpen?.Invoke(itemInfo);
        }
    }

    public void OpenInfoPanel(PropData prop)
    {
        if (!LevelStateController.Instance.IsAnyDragging())
        {
            var propInfo = new PropInformation(prop);
            OnObjInfoPanelOpen?.Invoke(propInfo);
        }
    }

    public void OpenInfoPanel(BuffData buff)
    {
        if (!LevelStateController.Instance.IsAnyDragging())
        {
            var buffInfo = new BuffInformation(buff);
            OnObjInfoPanelOpen?.Invoke(buffInfo);
        }
    }

    public void OpenInfoPanel(BackpackData backpack)
    {
        if (!LevelStateController.Instance.IsAnyDragging())
        {
            var bpInfo = new BackpackInformation(backpack);
            OnObjInfoPanelOpen?.Invoke(bpInfo);
        }
    }

    public void CloseObjInfoPanel()
    {
        OnObjInfoPanelClose?.Invoke();
    }

    #endregion

    [Serializable]
    private class TagInformation
    {
        public string tagName;
        public ItemTag tagProperty;
        public Sprite tagIcon;
        public string tagDescription;
    }
}


public class ObjectInformation
{
    public string name;
    public string description;
    public int size;
    public int value;
    public ObjectType objectType;
}

public class BackpackInformation : ObjectInformation
{
    public ObjectState state;
    public ItemTag tag; // TODO: 背包词条
    public int price;
    // public List<BpTags> tags;

    public BackpackInformation(BackpackData bp)
    {
        name = bp.displayName;
        // this.name = StringUtils.SplitBackpackName(bp.name);
        // this.name = "不锈钢货箱";
        description = bp.description;
        size = bp.shape.Length;
        value = bp.value;
        price = bp.price;
        objectType = ObjectType.Backpack;

        if (bp.isPlaced == false)
            state = ObjectState.Awaiting;
        else
            state = ObjectState.Resellable;

        tag = ItemTag.None; // TODO: 临时背包词条，默认都为None
    }
}

public class ItemInformation : ObjectInformation
{
    public ObjectState state;
    public List<ItemTag> tags;

    public ItemInformation(ItemData item)
    {
        name = item.displayName;
        // this.name = StringUtils.SplitItemName(item.name);
        description = item.description;
        value = item.itemValue;
        size = item.shape.Length;
        objectType = ObjectType.Item;
        state = ObjectState.Locked; // 货物锁定无法买卖

        tags = new List<ItemTag>();
        tags.AddRange(item.itemTagDic.Keys);
    }
}

public class PropInformation : ObjectInformation
{
    public int price;
    public ObjectState state; // 单元类道具可以买卖、初始状态默认待买入
    public List<ItemTag> tags;

    public PropInformation(PropData prop)
    {
        name = prop.displayName;
        description = prop.description;
        value = prop.value;
        price = prop.price;
        size = prop.shape.Length;
        objectType = ObjectType.Prop;

        if (prop.isPlaced == false)
            state = ObjectState.Awaiting;
        else
            state = ObjectState.Resellable;

        tags = prop.GetAllTags();
        // this.name = GeneratePropInfoName(prop);
    }

    private string GeneratePropInfoName(PropData prop)
    {
        // 过滤掉None和Ordinary标签
        var filteredTags = tags.Where(tag => tag != ItemTag.None && tag != ItemTag.Ordinary).ToList();

        // 如果没有有效的标签，返回一个默认名字
        if (!filteredTags.Any())
            return "无类型模块";

        // 按照标签的类型进行分组（Cool, Fragile, Natural）
        var tagGroups = filteredTags.GroupBy(tag =>
        {
            if (tag.ToString().Contains("Cool"))
                return "Cool";
            if (tag.ToString().Contains("Fragile"))
                return "Fragile";
            if (tag.ToString().Contains("Natural"))
                return "Natural";
            return "Unknown";
        }).ToList();

        // 如果所有标签都是同一类型，返回对应的道具名字
        if (tagGroups.Count == 1)
        {
            switch (tagGroups.First().Key)
            {
                case "Cool":
                    return "冷冻模块";
                case "Fragile":
                    return "缓震模块";
                case "Natural":
                    return "保鲜模块";
                default:
                    return "未知模块";
            }
        }

        // 如果有多种类型，返回复合型道具
        return "复合模块";
    }
}

public class BuffInformation : ObjectInformation
{
    public ObjectState state;
    public BuffEffect buffTag; // Buff类道具可以买卖、初始状态默认待买入
    public int price;

    public BuffInformation(BuffData buff)
    {
        name = buff.displayName;
        // this.name = GenerateBuffInfoName(buff);
        description = buff.description;
        // this.description = "特殊的货箱改装部件，似乎蕴含着某种神秘的能量。";
        value = buff.value;
        price = buff.price;
        size = buff.shape.Length;
        buffTag = buff.effect;
        objectType = ObjectType.Prop; // 临时归为Prop类

        if (buff.isPlaced == false)
            state = ObjectState.Awaiting;
        else
            state = ObjectState.Resellable;
    }

    private string GenerateBuffInfoName(BuffData buff)
    {
        var newName = "能量模块";
        // switch (buff.effect)
        // {
        //     case BuffEffect.EnergyI:
        //         newName = "1级能量";
        //         break;
        //     case BuffEffect.EnergyII:
        //         newName = "2级能量";
        //         break;
        //     case BuffEffect.EnergyIII:
        //         newName = "3级能量";
        //         break;
        // }
        return newName;
    }
}

public enum ObjectState
{
    Awaiting, // Waiting to be sold
    Resellable, // Sold, can be resold
    Locked // Locked, cannot be resold
}