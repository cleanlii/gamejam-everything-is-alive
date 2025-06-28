using System;
using System.Collections.Generic;
using cfg.level;
using UnityEngine;

[CreateAssetMenu(fileName = "IconConfig", menuName = "Config/Icon")]
public class IconConfig : ScriptableObject
{
    public List<Pin> pins; // 地图标记
    public List<ObjectIcon> objIcons; // 物品信息界面的图标（表示类型）
    public List<ShapeIcon> shapeIcons;

    public Sprite GetObjectIcon(ObjectType objType)
    {
        // 遍历 objIcons 列表，查找与传入的 objType 匹配的 ObjectIcon
        foreach (var objIcon in objIcons)
        {
            if (objIcon.objType == objType) return objIcon.iconImg; // 找到后返回对应的 Sprite
        }

        // 如果没有找到，返回 null
        return null;
    }
}

[Serializable]
public class Icon
{
    public string iconName;
    public Sprite iconImg;
}

[Serializable]
public class Pin : Icon
{
    public BuildingType buildingType;
    public GameObject prefab; // 世界层Prefab
}

[Serializable]
public class ObjectIcon : Icon
{
    public ObjectType objType;
}

public enum ObjectType
{
    Item,
    Prop,
    Backpack
}

[Serializable]
public class ShapeIcon : Icon
{
    public GridShape shape;
}