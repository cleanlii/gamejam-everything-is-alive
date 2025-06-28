using UnityEngine;

public class LevelCameraHandler : MonoBehaviour
{
    private Camera mainCamera;
    private Camera blackEdgeCamera;

    // 目标宽高比
    private readonly float targetAspect = 2778.0f / 1284.0f; // 21:9 iPhone 比例

    #region 屏幕裁切

    // 初始化摄像机
    private void SetupCameras()
    {
        // 获取主摄像机
        mainCamera = Camera.main;

        // 创建黑边摄像机
        var blackEdgeCamObj = new GameObject("BlackEdgeCamera");
        blackEdgeCamera = blackEdgeCamObj.AddComponent<Camera>();

        // 设置黑边摄像机的属性
        blackEdgeCamera.clearFlags = CameraClearFlags.SolidColor; // 只渲染黑色背景
        blackEdgeCamera.backgroundColor = Color.black; // 黑边颜色
        blackEdgeCamera.cullingMask = 0; // 不渲染任何内容，只显示黑色
        blackEdgeCamera.depth = mainCamera.depth - 1; // 设置在主摄像机之后渲染
    }

    // 调整视口和 Letterbox
    private void ApplyLetterbox()
    {
        var windowAspect = Screen.width / (float)Screen.height;
        var scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1.0f)
        {
            // 如果高度太大，添加上下黑边
            var scaleWidth = 1.0f;
            var scaleHeightAdjusted = scaleHeight;

            // 设置主摄像机的视口，使其不填满整个屏幕
            mainCamera.rect = new Rect(0, (1.0f - scaleHeightAdjusted) / 2.0f, scaleWidth, scaleHeightAdjusted);

            // 黑边摄像机的视口需要覆盖整个屏幕
            blackEdgeCamera.rect = new Rect(0, 0, 1, 1);
        }
        else
        {
            // 如果宽度太大，添加左右黑边
            var scaleWidthAdjusted = 1.0f / scaleHeight;

            // 设置主摄像机的视口，使其不填满整个屏幕
            mainCamera.rect = new Rect((1.0f - scaleWidthAdjusted) / 2.0f, 0, scaleWidthAdjusted, scaleHeight);

            // 黑边摄像机的视口需要覆盖整个屏幕
            blackEdgeCamera.rect = new Rect(0, 0, 1, 1);
        }
    }

    #endregion
}