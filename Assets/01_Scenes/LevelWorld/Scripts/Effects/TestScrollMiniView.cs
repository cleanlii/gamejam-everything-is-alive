using UnityEngine;
using UnityEngine.UI;

public class TestScrollMiniView : MonoBehaviour
{
    public RectTransform image1;
    public RectTransform image2;
    public float scrollSpeed = 100f;

    private float imageHeight;

    void Start()
    {
        imageHeight = image1.rect.height;
        image2.anchoredPosition = new Vector2(image1.anchoredPosition.x, image1.anchoredPosition.y + imageHeight);
    }

    void Update()
    {
        float newY1 = image1.anchoredPosition.y - scrollSpeed * Time.deltaTime;
        float newY2 = image2.anchoredPosition.y - scrollSpeed * Time.deltaTime;

        if (newY1 <= -imageHeight)
        {
            newY1 = newY2 + imageHeight;
        }

        if (newY2 <= -imageHeight)
        {
            newY2 = newY1 + imageHeight;
        }

        image1.anchoredPosition = new Vector2(image1.anchoredPosition.x, newY1);
        image2.anchoredPosition = new Vector2(image2.anchoredPosition.x, newY2);
    }
}
