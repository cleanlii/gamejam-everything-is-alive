using UnityEngine;

public class LayeredBlendController : MonoBehaviour
{
    public GameObject prefabWithLayers;

    void Start()
    {
        // 获取所有子对象的SpriteRenderer组件
        SpriteRenderer[] renderers = prefabWithLayers.GetComponentsInChildren<SpriteRenderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            // 为每个图层创建一个材质实例并应用自定义Shader
            Material material = new Material(Shader.Find("Unlit/TestLayerEffect"));
            material.mainTexture = renderers[i].sprite.texture;
            renderers[i].material = material;

            // 设置混合模式，可以根据需要调整
            material.SetFloat("_BlendMode", GetBlendModeFromLayerName(renderers[i].name));
        }
    }

    float GetBlendModeFromLayerName(string layerName)
    {
        // 根据图层名称的后缀设置混合模式
        if (layerName.EndsWith("_Normal", System.StringComparison.OrdinalIgnoreCase))
        {
            return 0.0f;
        }
        else if (layerName.EndsWith("_Multiply", System.StringComparison.OrdinalIgnoreCase))
        {
            return 1.0f;
        }
        else if (layerName.EndsWith("_Screen", System.StringComparison.OrdinalIgnoreCase))
        {
            return 2.0f;
        }
        else if (layerName.EndsWith("_ColorDodge", System.StringComparison.OrdinalIgnoreCase))
        {
            return 3.0f;
        }
        else if (layerName.EndsWith("_ColorBurn", System.StringComparison.OrdinalIgnoreCase))
        {
            return 4.0f;
        }
        else if (layerName.EndsWith("_LinearDodge", System.StringComparison.OrdinalIgnoreCase))
        {
            return 5.0f;
        }
        else if (layerName.EndsWith("_LinearBurn", System.StringComparison.OrdinalIgnoreCase))
        {
            return 6.0f;
        }
        else if (layerName.EndsWith("_Overlay", System.StringComparison.OrdinalIgnoreCase))
        {
            return 7.0f;
        }
        else if (layerName.EndsWith("_HardLight", System.StringComparison.OrdinalIgnoreCase))
        {
            return 8.0f;
        }
        else if (layerName.EndsWith("_SoftLight", System.StringComparison.OrdinalIgnoreCase))
        {
            return 9.0f;
        }
        else if (layerName.EndsWith("_VividLight", System.StringComparison.OrdinalIgnoreCase))
        {
            return 10.0f;
        }
        else if (layerName.EndsWith("_LinearLight", System.StringComparison.OrdinalIgnoreCase))
        {
            return 11.0f;
        }
        else if (layerName.EndsWith("_PinLight", System.StringComparison.OrdinalIgnoreCase))
        {
            return 12.0f;
        }
        else if (layerName.EndsWith("_HardMix", System.StringComparison.OrdinalIgnoreCase))
        {
            return 13.0f;
        }
        else if (layerName.EndsWith("_Difference", System.StringComparison.OrdinalIgnoreCase))
        {
            return 14.0f;
        }
        else if (layerName.EndsWith("_Exclusion", System.StringComparison.OrdinalIgnoreCase))
        {
            return 15.0f;
        }
        else if (layerName.EndsWith("_Hue", System.StringComparison.OrdinalIgnoreCase))
        {
            return 16.0f;
        }
        else
        {
            return 0.0f;
        }
    }
}
