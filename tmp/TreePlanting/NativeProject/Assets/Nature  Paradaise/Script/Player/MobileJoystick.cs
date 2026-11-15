using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
/// <summary>Joystick virtual yang mengubah drag pointer menjadi arah analog ternormalisasi.</summary>
public sealed class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] RectTransform background;
    [SerializeField] RectTransform handle;
    [SerializeField, Range(0f, 0.9f)] float deadZone = 0.12f;

    public static MobileJoystick Active { get; private set; }
    public Vector2 Direction { get; private set; }

    public void Configure(RectTransform backgroundRect, RectTransform handleRect)
    {
        background = backgroundRect;
        handle = handleRect;
    }

    void OnEnable() => Active = this;

    void OnDisable()
    {
        if (Active == this) Active = null;
        ResetJoystick();
    }

    public void OnPointerDown(PointerEventData eventData) => UpdateDirection(eventData);
    public void OnDrag(PointerEventData eventData) => UpdateDirection(eventData);
    public void OnPointerUp(PointerEventData eventData) => ResetJoystick();

    void UpdateDirection(PointerEventData eventData)
    {
        if (background == null || handle == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        Vector2 radius = background.rect.size * 0.5f;
        Vector2 normalized = new(
            radius.x > 0f ? localPoint.x / radius.x : 0f,
            radius.y > 0f ? localPoint.y / radius.y : 0f);
        normalized = Vector2.ClampMagnitude(normalized, 1f);
        Direction = normalized.magnitude < deadZone ? Vector2.zero : normalized;
        handle.anchoredPosition = new Vector2(Direction.x * radius.x * 0.55f, Direction.y * radius.y * 0.55f);
    }

    void ResetJoystick()
    {
        Direction = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }
}
