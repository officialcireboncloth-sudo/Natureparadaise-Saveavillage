using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>Existing save/gameplay feedback API, routed to the shared queued information dock.</summary>
public class SaveLoadFeedback : MonoBehaviour
{
    public static SaveLoadFeedback Instance;
    public TMP_Text feedbackText;
    public float showDuration=2.5f;
    public float fadeInDuration=.15f;
    public float fadeOutDuration=.5f;
    void Awake()
    {
        if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}
        Instance=this;HideLegacyGraphics();
    }
    void HideLegacyGraphics()
    {
        if(feedbackText==null)feedbackText=GetComponentInChildren<TMP_Text>(true);
        if(feedbackText!=null)feedbackText.enabled=false;
        if(TryGetComponent<Image>(out var image))image.enabled=false;
        if(TryGetComponent<CanvasGroup>(out var group)){group.alpha=0;group.interactable=false;group.blocksRaycasts=false;}
    }
    public void ShowSave()=>ShowMessage("GAME SAVED!");
    public void ShowLoad()=>ShowMessage("GAME LOADED!");
    public void ShowMessage(string message){HideLegacyGraphics();GameplayInfoPanel.Notify(message,showDuration);}
    void OnDestroy(){if(Instance==this)Instance=null;}
}
