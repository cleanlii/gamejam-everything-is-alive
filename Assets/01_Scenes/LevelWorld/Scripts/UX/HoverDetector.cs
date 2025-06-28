using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Sprite originalSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Image image;
    [SerializeField] private UnityEvent onHover;
    [SerializeField] private UnityEvent onUnhover;
    public bool isHovered;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isHovered) return;
        if (LevelStateController.Instance.IsAnyAnimating()) return;

        isHovered = true;
        image.sprite = hoverSprite;
        onHover?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovered) return;
        if (LevelStateController.Instance.IsAnyAnimating()) return;

        isHovered = false;
        image.sprite = originalSprite;
        onUnhover?.Invoke();
    }
}