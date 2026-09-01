using TMPro;
using UnityEngine;

/// <summary>View sederhana panel shop dan teks transaksi; logika ekonomi tetap berada di ShopManager.</summary>
public class ShopUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    public TMP_Text shopText;

    public void Show()
    {
        WorldInteractionPrompt.AcquireSuppression(this);
        if (panel != null)
            panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
        WorldInteractionPrompt.ReleaseSuppression(this);
    }

    public void SetText(string text)
    {
        if (shopText != null)
            shopText.text = text;
    }

    public bool IsVisible()
    {
        return panel != null && panel.activeSelf;
    }

    void OnDisable() => WorldInteractionPrompt.ReleaseSuppression(this);
}
