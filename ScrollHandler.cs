using UnityEngine;
using UnityEngine.UI;

public class ScrollHandler : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float scrollSensivity;

    public void ScrollLeft()
    {
        scrollRect.StopMovement();
        scrollRect.velocity = new Vector2(scrollSensivity, 0.0f);
    }
    public void ScrollRight()
    {
        scrollRect.StopMovement();
        scrollRect.velocity = new Vector2(-scrollSensivity, 0.0f);
    }
}
