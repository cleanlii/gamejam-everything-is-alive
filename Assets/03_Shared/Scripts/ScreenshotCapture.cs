using UnityEngine;
using System.IO;

public class ScreenshotCapture : MonoBehaviour
{
    public Camera screenshotCamera;
    public KeyCode screenshotKey = KeyCode.P; // 设置截图键为P键
    public int width = 2778; // 分辨率宽度
    public int height = 1284; // 分辨率高度
    public string savePath = "Assets/Screenshot.png";

    void Update()
    {
        // 检测P键是否被按下
        if (Input.GetKeyDown(screenshotKey))
        {
            TakeScreenshot();
        }
    }

    public void TakeScreenshot()
    {
        // 创建Render Texture
        RenderTexture rt = new RenderTexture(width, height, 24);
        screenshotCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // 渲染
        screenshotCamera.Render();

        // 读取像素
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        // 重置状态
        screenshotCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        // 编码为PNG并保存
        byte[] bytes = screenShot.EncodeToPNG();
        File.WriteAllBytes(savePath, bytes);

        Debug.Log($"Screenshot saved to {savePath}");
    }
}
