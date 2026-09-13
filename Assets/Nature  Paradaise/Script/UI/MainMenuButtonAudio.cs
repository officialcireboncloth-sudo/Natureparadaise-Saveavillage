using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MainMenuButtonAudio : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [SerializeField] MainMenuController menu;
    float lastHoverTime = -1f;

    void Awake()
    {
        if (menu == null) menu = GetComponentInParent<MainMenuController>();
    }

    public void OnPointerEnter(PointerEventData eventData) => Hover();
    public void OnSelect(BaseEventData eventData) => Hover();

    void Hover()
    {
        if (Time.unscaledTime - lastHoverTime < 0.05f) return;
        lastHoverTime = Time.unscaledTime;
        menu?.PlayHoverSound();
    }
}
