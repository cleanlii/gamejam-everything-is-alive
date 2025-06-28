using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class ObjUtils
{
    /// <summary>
    ///     改变两个Obj的层级顺序
    /// </summary>
    /// <param name="topObject"></param>
    /// <param name="bottomObject"></param>
    public static void SetSiblingOrder(GameObject topObject, GameObject bottomObject)
    {
        // 获取两个对象在父母层级中的索引位置
        var index1 = topObject.transform.GetSiblingIndex();
        var index2 = bottomObject.transform.GetSiblingIndex();

        // 如果 topObject 已经在上层（索引值大于 bottomObject），则不需要交换
        if (index1 > index2) return;

        // 否则，交换两个对象的位置
        topObject.transform.SetSiblingIndex(index2);
        bottomObject.transform.SetSiblingIndex(index1);
    }

    /// <summary>
    ///     调整透明度为零，自动判断Image或者SpriteRenderer
    /// </summary>
    /// <param name="obj"></param>
    public static void SetTransparencyToZero(GameObject obj)
    {
        // 检查是否有 Image 组件
        var img = obj.GetComponent<Image>();
        if (img != null)
        {
            var color = img.color;
            color.a = 0f; // 设置透明度为0
            img.color = color;
            // Debug.Log("Image 透明度已设置为 0");
            return; // 如果已经处理了Image组件，就直接返回
        }

        // 检查是否有 SpriteRenderer 组件
        var spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            var color = spriteRenderer.color;
            color.a = 0f; // 设置透明度为0
            spriteRenderer.color = color;
            // Debug.Log("SpriteRenderer 透明度已设置为 0");
            return;
        }

        Debug.LogWarning("GameObject 没有 Image 或 SpriteRenderer 组件！");
    }

    /// <summary>
    ///     从父集中找某个Component
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="child"></param>
    /// <returns></returns>
    private static T FindInParent<T>(GameObject child) where T : Component
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

    /// <summary>
    ///     调整SpriteRenderer透明度
    /// </summary>
    /// <param name="spriteRenderer"></param>
    /// <param name="alpha"></param>
    public static void SetSpriteTransparency(SpriteRenderer spriteRenderer, float alpha)
    {
        if (spriteRenderer != null)
        {
            var color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }

    /// <summary>
    ///     调整CanvasGroup透明度
    /// </summary>
    /// <param name="canvasGroup"></param>
    /// <param name="alpha"></param>
    public static void SetCanvasGroupTransparency(CanvasGroup canvasGroup, float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.interactable = alpha > 0.5f; // 根据透明度设置交互性
            canvasGroup.blocksRaycasts = alpha > 0.5f; // 阻挡射线
        }
    }

    /// <summary>
    ///     调整SpriteRenderer层级
    /// </summary>
    /// <param name="spriteRenderer"></param>
    /// <param name="layerName"></param>
    /// <param name="order"></param>
    public static void SetSpriteSortingLayer(SpriteRenderer spriteRenderer, string layerName, int order = 0)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = layerName;
            spriteRenderer.sortingOrder = order;
        }
    }

    /// <summary>
    ///     获取指定父对象下特定子对象的所有直接子物体。
    /// </summary>
    /// <param name="parent">父对象。</param>
    /// <param name="childName">子对象名称。</param>
    /// <returns>子对象的 GameObject 列表。</returns>
    public static List<GameObject> GetChildObjects(GameObject parent, string childName)
    {
        var result = new List<GameObject>();

        // 查找指定的子对象
        var childParent = parent.transform.Find(childName);
        if (childParent == null)
        {
            Debug.LogWarning($"Child object with name '{childName}' not found under '{parent.name}'.");
            return result;
        }

        // 遍历直接子物体
        foreach (Transform child in childParent)
        {
            result.Add(child.gameObject);
        }

        return result;
    }

    /// <summary>
    ///     设置父级Obj和对应Layer
    /// </summary>
    /// <param name="child"></param>
    /// <param name="newParent"></param>
    /// <param name="type"></param>
    public static void SetParentAndLayer(GameObject child, Transform newParent, InventoryType type)
    {
        // 设置父对象
        child.transform.SetParent(newParent);

        // 获取对应的 Layer 名称
        var layer = GetLayerFromInventoryType(type);
        if (layer == -1)
        {
            Debug.LogError($"Invalid layer for InventoryType: {type}. Check Layer settings in Unity.");
            return;
        }

        // 设置 Layer（包括子对象）
        SetLayerRecursively(child, layer);
    }

    // 根据 InventoryType 获取对应的 Layer
    private static int GetLayerFromInventoryType(InventoryType type)
    {
        switch (type)
        {
            case InventoryType.Cabinet:
                return LayerMask.NameToLayer("InCabinet");
            case InventoryType.Truck:
                return LayerMask.NameToLayer("InTrunk");
            case InventoryType.Pool:
                return LayerMask.NameToLayer("InPool");
            case InventoryType.Store:
                return LayerMask.NameToLayer("UI");
            default:
                return -1; // 返回 -1 表示无效的 Layer
        }
    }

    // 递归设置对象及其所有子对象的 Layer
    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}