using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Presenter UI dialog. Seluruh visual wajib dihubungkan lewat Inspector.</summary>
[DisallowMultipleComponent]
public sealed class DialogueUIController : MonoBehaviour
{
    [SerializeField] GameObject panelRoot;
    [SerializeField] TMP_Text speakerNameText;
    [SerializeField] TMP_Text bodyText;
    [SerializeField] TMP_Text emotionText;
    [SerializeField] Image portraitImage;
    [SerializeField] Button continueButton;
    [SerializeField] RectTransform choicesRoot;
    [SerializeField] Button choiceButtonPrefab;
    [SerializeField] KeyCode continueKey = KeyCode.E;

    readonly List<Button> spawnedChoices = new();
    DialogueService boundService;
    int openedFrame;

    void OnEnable()
    {
        Bind();
        if (continueButton != null) continueButton.onClick.AddListener(Continue);
    }

    void Start()
    {
        Bind();
        Refresh();
    }

    void OnDisable()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(Continue);
        Unbind();
    }

    void Update()
    {
        if (boundService == null) Bind();
        if (boundService != null && boundService.IsOpen && Time.frameCount > openedFrame &&
            (Input.GetKeyDown(continueKey) || Input.GetKeyDown(KeyCode.Space))) Continue();
    }

    void Bind()
    {
        DialogueService service = DialogueService.Instance;
        if (service == boundService) return;
        Unbind();
        boundService = service;
        if (boundService == null) return;
        boundService.ConversationStarted += HandleStarted;
        boundService.NodeChanged += Refresh;
        boundService.ConversationEnded += Refresh;
    }

    void Unbind()
    {
        if (boundService == null) return;
        boundService.ConversationStarted -= HandleStarted;
        boundService.NodeChanged -= Refresh;
        boundService.ConversationEnded -= Refresh;
        boundService = null;
    }

    void HandleStarted()
    {
        openedFrame = Time.frameCount;
        Refresh();
    }

    public void Continue()
    {
        if (boundService == null || !boundService.IsOpen) return;
        if (boundService.VisibleChoices.Count == 0) boundService.Continue();
    }

    void Refresh()
    {
        bool open = boundService != null && boundService.IsOpen;
        if (panelRoot != null) panelRoot.SetActive(open);
        ClearChoices();
        if (!open) return;

        DialogueNode node = boundService.CurrentNode;
        DialogueSpeakerSO speaker = boundService.CurrentSpeaker;
        if (speakerNameText != null)
        {
            speakerNameText.text = speaker != null ? speaker.displayName : string.Empty;
            speakerNameText.color = speaker != null ? speaker.nameColor : Color.white;
        }
        if (bodyText != null) bodyText.text = node.text;
        if (emotionText != null) emotionText.text = node.emotion;
        if (portraitImage != null)
        {
            portraitImage.sprite = speaker != null ? speaker.portrait : null;
            portraitImage.enabled = portraitImage.sprite != null;
        }

        bool hasChoices = boundService.VisibleChoices.Count > 0;
        if (continueButton != null) continueButton.gameObject.SetActive(!hasChoices);
        if (!hasChoices || choicesRoot == null || choiceButtonPrefab == null) return;
        for (int i = 0; i < boundService.VisibleChoices.Count; i++)
        {
            int selectedIndex = i;
            Button button = Instantiate(choiceButtonPrefab, choicesRoot);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = boundService.VisibleChoices[i].text;
            button.onClick.AddListener(() => boundService?.SelectChoice(selectedIndex));
            button.gameObject.SetActive(true);
            spawnedChoices.Add(button);
        }
    }

    void ClearChoices()
    {
        foreach (Button button in spawnedChoices) if (button != null) Destroy(button.gameObject);
        spawnedChoices.Clear();
    }
}
