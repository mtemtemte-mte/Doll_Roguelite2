using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StartPanelHoverTint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    Image targetImage;
    Color normalColor;
    Color hoverColor;
    Color pressedColor;
    bool isPointerInside;

    public void Configure(Image target, Color normal, Color hover, Color pressed)
    {
        targetImage = target;
        normalColor = normal;
        hoverColor = hover;
        pressedColor = pressed;
        Apply(normalColor);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        Apply(hoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        Apply(normalColor);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Apply(pressedColor);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Apply(isPointerInside ? hoverColor : normalColor);
    }

    void OnDisable()
    {
        isPointerInside = false;
        Apply(normalColor);
    }

    void Apply(Color color)
    {
        if (targetImage != null)
            targetImage.color = color;
    }
}
