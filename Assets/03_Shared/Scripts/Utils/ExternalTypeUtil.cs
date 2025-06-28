public static class ExternalTypeUtil
{
    public static UnityEngine.Vector2Int NewVector2Int(cfg.vector2int v)
    {
        return new UnityEngine.Vector2Int(v.x, v.y);
    }

    public static UnityEngine.Vector2 NewVector2(cfg.vector2 v)
    {
        return new UnityEngine.Vector2(v.x, v.y);
    }

    public static UnityEngine.Vector3 NewVector3(cfg.vector3 v)
    {
        return new UnityEngine.Vector3(v.x, v.y, v.z);
    }

    public static UnityEngine.Vector4 NewVector4(cfg.vector4 v)
    {
        return new UnityEngine.Vector4(v.x, v.y, v.z, v.w);
    }
}