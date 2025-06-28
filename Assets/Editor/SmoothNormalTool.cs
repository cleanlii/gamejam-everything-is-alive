using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public enum WRITETYPE
{
    VertexColor = 0,
    Tangent = 1,
    // Texter=2,
}

public class SmoothNormalTool : EditorWindow
{
    // 角度阈值，处理复杂高模Mesh、同一位置多个顶点的情况下可用，默认为0
    public float angle = 0.0f;

    private bool addVertexColors = false;

    public WRITETYPE wt;

    // 保存路径（测试）
    private readonly string savePath = "Assets/09_Test/TestSmoothNormals/";

    [MenuItem("Tools/Smooth Normal Tool")]
    public static void ShowWindow()
    {
        GetWindow<SmoothNormalTool>("Smooth Normal Tool");
    }

    private GameObject selectedObject;

    void OnGUI()
    {
        GUILayout.Space(5);
        GUILayout.Label("1. 请选择需要平滑法线的物体", EditorStyles.boldLabel);
        selectedObject = EditorGUILayout.ObjectField("Mesh Object", selectedObject, typeof(GameObject), true) as GameObject;
        GUILayout.Space(10);
        GUILayout.Label("2. 请选择需要写入平滑后的物体空间法线数据的目标", EditorStyles.boldLabel);
        wt = (WRITETYPE)EditorGUILayout.EnumPopup("写入目标", wt);
        GUILayout.Space(10);
        GUILayout.Label("3. 请选择平滑角度", EditorStyles.boldLabel);
        angle = EditorGUILayout.Slider(angle, 0.0f, 180.0f);
        GUILayout.Space(10);

        addVertexColors = EditorGUILayout.Toggle(new GUIContent("  自动添加顶点色", "默认添加白色，原Mesh中不包含顶点色数据时请勾选"), addVertexColors);

        switch (wt)
        {
            case WRITETYPE.Tangent:
                GUILayout.Label("  将会把平滑后的法线写入到顶点切线中", EditorStyles.boldLabel);
                break;
            case WRITETYPE.VertexColor:
                GUILayout.Label("  将会把平滑后的法线写入到顶点色的RGB通道中", EditorStyles.boldLabel);
                break;
        }

        GUILayout.Space(10);
        if (GUILayout.Button("4. 平滑法线（在Scene中可预览）"))
        {
            if (selectedObject != null)
            {
                if (addVertexColors)
                {
                    AddVertexColors(selectedObject);
                }
                SmoothNormalPrev(wt);
            }
            else
            {
                Debug.LogError("请选择正确包含Mesh的物体!");
            }
        }
        GUILayout.Space(10);
        if (GUILayout.Button("5. 导出Mesh至本地"))
        {
            if (addVertexColors)
            {
                AddVertexColors(selectedObject);
            }
            SaveAsMesh();
        }
        GUILayout.Space(20);
        GUILayout.Label("保存路径：", EditorStyles.boldLabel);
        GUILayout.Label(savePath, EditorStyles.boldLabel);
    }

    public void SmoothNormalPrev(WRITETYPE wt)
    {
        MeshFilter[] meshFilters = selectedObject.GetComponentsInChildren<MeshFilter>();
        SkinnedMeshRenderer[] skinMeshRenders = selectedObject.GetComponentsInChildren<SkinnedMeshRenderer>();

        //遍历两种Mesh，调用平滑法线方法（三种备选方案：Unity自带、直接计算、角度阈值优化）
        foreach (var meshFilter in meshFilters)
        {
            Mesh mesh = meshFilter.sharedMesh;
            // MeshExtends. referred to: https://medium.com/@fra3point/runtime-normals-recalculation-in-unity-a-complete-approach-db42490a5644
            Vector3[] averageNormals = MeshExtends.RecalculateNormals(mesh, angle);
            OverrideMesh(mesh, averageNormals);

        }
        foreach (var skinMeshRender in skinMeshRenders)
        {
            Mesh mesh = skinMeshRender.sharedMesh;
            Vector3[] averageNormals = MeshExtends.RecalculateNormals(mesh, angle);
            OverrideMesh(mesh, averageNormals);
        }
    }

    // 直接写入并预览，但第二次打开会重置
    public void OverrideMesh(Mesh mesh, Vector3[] averageNormals)
    {
        switch (wt)
        {
            //写入到顶点切线
            case WRITETYPE.Tangent:
                var tangents = new Vector4[mesh.vertexCount];
                for (var j = 0; j < mesh.vertexCount; j++)
                {
                    tangents[j] = new Vector4(averageNormals[j].x, averageNormals[j].y, averageNormals[j].z, 0);
                }
                mesh.tangents = tangents;
                break;

            // 写入到顶点色
            case WRITETYPE.VertexColor:
                if (mesh.colors.Length > 0 || mesh.colors32.Length > 0)
                {
                    Color[] _colors = new Color[mesh.vertexCount];
                    Color[] _colors2 = new Color[mesh.vertexCount];
                    _colors2 = mesh.colors;
                    for (var j = 0; j < mesh.vertexCount; j++)
                    {
                        _colors[j] = new Vector4(averageNormals[j].x, averageNormals[j].y, averageNormals[j].z, _colors2[j].a);
                    }
                    mesh.colors = _colors;
                    break;
                }
                else
                {
                    Debug.LogError("该Mesh未包含顶点颜色信息，请勾选自动添加项或使用gcc重新导出完整模型");
                    break;
                }
        }
    }

    // 直接保存到本地
    public void SaveAsMesh()
    {
        MeshFilter[] meshFilters = selectedObject.GetComponentsInChildren<MeshFilter>();
        SkinnedMeshRenderer[] skinMeshRenders = selectedObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var meshFilter in meshFilters)
        {
            Mesh mesh = meshFilter.sharedMesh;
            Vector3[] averageNormals = MeshExtends.RecalculateNormals(mesh, angle);
            ExportMeshFile(mesh, averageNormals);

        }
        foreach (var skinMeshRender in skinMeshRenders)
        {
            Mesh mesh = skinMeshRender.sharedMesh;
            Vector3[] averageNormals = MeshExtends.RecalculateNormals(mesh, angle);
            ExportMeshFile(mesh, averageNormals);
        }
    }

    public void ExportMeshFile(Mesh mesh, Vector3[] averageNormals)
    {
        Mesh mesh2 = new Mesh();
        CopyMesh(mesh2, mesh);
        switch (wt)
        {
            case WRITETYPE.Tangent:
                var tangents = new Vector4[mesh2.vertexCount];
                for (var j = 0; j < mesh2.vertexCount; j++)
                {
                    tangents[j] = new Vector4(averageNormals[j].x, averageNormals[j].y, averageNormals[j].z, 0);
                }
                mesh2.tangents = tangents;
                Debug.Log("切线写入成功");
                break;
            case WRITETYPE.VertexColor:
                Color[] _colors = new Color[mesh2.vertexCount];
                Color[] _colors2 = new Color[mesh2.vertexCount];
                _colors2 = mesh2.colors;
                for (var j = 0; j < mesh2.vertexCount; j++)
                {
                    _colors[j] = new Vector4(averageNormals[j].x, averageNormals[j].y, averageNormals[j].z, _colors2[j].a);
                }
                mesh2.colors = _colors;
                Debug.Log("顶点色写入成功");
                break;
        }

        //创建文件夹路径
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
        AssetDatabase.Refresh();

        mesh2.name = mesh2.name + "_SMNormal";
        // Debug.Log(mesh2.vertexCount);
        AssetDatabase.CreateAsset(mesh2, "Assets/09_Test/TestSmoothNormals/" + mesh2.name + ".asset");
        Debug.Log("导出Mesh成功");
    }

    public void CopyMesh(Mesh dest, Mesh src)
    {
        dest.Clear();
        dest.vertices = src.vertices;

        List<Vector4> uvs = new List<Vector4>();

        src.GetUVs(0, uvs); dest.SetUVs(0, uvs);
        src.GetUVs(1, uvs); dest.SetUVs(1, uvs);
        src.GetUVs(2, uvs); dest.SetUVs(2, uvs);
        src.GetUVs(3, uvs); dest.SetUVs(3, uvs);

        dest.normals = src.normals;
        dest.tangents = src.tangents;
        dest.boneWeights = src.boneWeights;
        dest.colors = src.colors;
        dest.colors32 = src.colors32;
        dest.bindposes = src.bindposes;

        dest.subMeshCount = src.subMeshCount;

        for (int i = 0; i < src.subMeshCount; i++)
            dest.SetIndices(src.GetIndices(i), src.GetTopology(i), i);

        dest.name = src.name;
    }

    private void AddVertexColors(GameObject obj)
    {
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            Mesh mesh = meshFilter.sharedMesh;
            // 检查是否包含顶点颜色，考虑可能用color32储存的情况
            if (mesh.colors.Length > 0 || mesh.colors32.Length > 0)
            {
                Debug.LogWarning("该Mesh已包含顶点色信息，跳过添加...");
            }
            else
            {
                // Mesh不包含顶点颜色信息，添加白色顶点颜色
                // 不排除应用于高模的极端情况，这个时候考虑使用color32提升性能
                Color[] colors = new Color[mesh.vertexCount];
                for (int i = 0; i < colors.Length; i++)
                {
                    // 默认添加为白色
                    colors[i] = Color.white;
                }
                mesh.colors = colors;
                Debug.Log("已添加默认顶点色: " + obj.name);
            }
        }
    }
}
