using UnityEngine;
using UnityEngine.EventSystems; 

public class Joystick : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Vector2 direction { get; set; }

    [SerializeField]
    private RectTransform leverTransform;
    private RectTransform rectTransform;

    //[SerializeField, Range(10f, 120f)]
    [SerializeField] private float leverRange = 120f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        DragEvent(eventData);
    }
    public void OnDrag(PointerEventData eventData)
    {
        DragEvent(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        direction = Vector2.zero;
        leverTransform.anchoredPosition = Vector2.zero;
    }
    private void DragEvent(PointerEventData eventData)
    {
        var inputPos = eventData.position - rectTransform.anchoredPosition;
        var rangeDir = inputPos.magnitude < leverRange ? inputPos : inputPos.normalized * leverRange;
        leverTransform.anchoredPosition = rangeDir;

        direction = inputPos.normalized;
    }
}