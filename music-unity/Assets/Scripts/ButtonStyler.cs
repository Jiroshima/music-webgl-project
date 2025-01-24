using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ButtonStyler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Button myButton;
    public TMP_Text buttonText;
    public Image buttonImage;

    public Color normalColor = new Color(0.25f, 0.45f, 0.75f); // Light blue
    public Color hoverColor = new Color(0.35f, 0.55f, 0.85f); // Lighter blue
    public Color pressedColor = new Color(0.15f, 0.35f, 0.65f); // Darker blue

    private void Start()
    {
        // Set initial color to normal
        SetButtonColor(normalColor);

        // Button click handler
        myButton.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        // Handle click logic, e.g., Play/Pause
        Debug.Log("Button clicked!");
    }

    private void SetButtonColor(Color color)
    {
        buttonImage.color = color;
    }

    // Called when the pointer enters the button area
    public void OnPointerEnter(PointerEventData eventData)
    {
        SetButtonColor(hoverColor);
    }

    // Called when the pointer exits the button area
    public void OnPointerExit(PointerEventData eventData)
    {
        SetButtonColor(normalColor);
    }
}
