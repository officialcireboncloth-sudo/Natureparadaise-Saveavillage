using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MainMenuButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
{
    [SerializeField] MainMenuController menu;
    float lastHoverTime = -1f;

    void Awake()
    {
        if (menu == null) menu = GetComponentInParent<MainMenuController>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetPointerHighlight(true);
        Hover();
    }
    public void OnPointerExit(PointerEventData eventData) => SetPointerHighlight(false);
    void OnDisable() => SetPointerHighlight(false);

    void SetPointerHighlight(bool hovered)
    {
        Button button = GetComponent<Button>();
        if (button == null) return;
        ColorBlock colors = button.colors;
        // Selected takes priority over Highlighted in Unity, including the initial menu selection.
        colors.selectedColor = hovered ? colors.highlightedColor : colors.normalColor;
        button.colors = colors;
    }
    public void OnSelect(BaseEventData eventData) => Hover();

    void Hover()
    {
        if (Time.unscaledTime - lastHoverTime < 0.05f) return;
        lastHoverTime = Time.unscaledTime;
        menu?.PlayHoverSound();
    }
}
