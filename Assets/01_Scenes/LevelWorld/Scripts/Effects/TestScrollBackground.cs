using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestScrollBackground : MonoBehaviour
{
    public RectTransform image00;
    public RectTransform image01;
    public RectTransform image10;
    public RectTransform image11;
    public float scrollSpeed = 100f;

    private float imageWidth;
    private float imageHeight;

    void Start()
    {
        imageWidth = image00.rect.width;
        imageHeight = image00.rect.height;

        // // 初始化图片位置
        // image00.anchoredPosition = new Vector2(0, 0);
        // image01.anchoredPosition = new Vector2(0, imageHeight);
        // image10.anchoredPosition = new Vector2(imageWidth, 0);
        // image11.anchoredPosition = new Vector2(imageWidth, imageHeight);
    }

    void Update()
    {
        // 计算每帧的移动量
        float moveAmount = scrollSpeed * Time.deltaTime;

        // 更新每张图片的位置
        image00.anchoredPosition += new Vector2(-moveAmount, -moveAmount);
        image01.anchoredPosition += new Vector2(-moveAmount, -moveAmount);
        image10.anchoredPosition += new Vector2(-moveAmount, -moveAmount);
        image11.anchoredPosition += new Vector2(-moveAmount, -moveAmount);

        // 检查并调整位置以实现无缝滚动
        if (image00.anchoredPosition.x <= -imageWidth || image00.anchoredPosition.y <= -imageHeight)
        {
            image00.anchoredPosition = image11.anchoredPosition + new Vector2(imageWidth, imageHeight);
            image01.anchoredPosition = image00.anchoredPosition + new Vector2(0, imageHeight);
            image10.anchoredPosition = image00.anchoredPosition + new Vector2(imageWidth, 0);
            image11.anchoredPosition = image00.anchoredPosition + new Vector2(imageWidth, imageHeight);
        }

        if (image01.anchoredPosition.x <= -imageWidth || image01.anchoredPosition.y <= -imageHeight)
        {
            image01.anchoredPosition = image00.anchoredPosition + new Vector2(0, imageHeight);
        }

        if (image10.anchoredPosition.x <= -imageWidth || image10.anchoredPosition.y <= -imageHeight)
        {
            image10.anchoredPosition = image00.anchoredPosition + new Vector2(imageWidth, 0);
        }

        if (image11.anchoredPosition.x <= -imageWidth || image11.anchoredPosition.y <= -imageHeight)
        {
            image11.anchoredPosition = image00.anchoredPosition + new Vector2(imageWidth, imageHeight);
        }
    }
}
