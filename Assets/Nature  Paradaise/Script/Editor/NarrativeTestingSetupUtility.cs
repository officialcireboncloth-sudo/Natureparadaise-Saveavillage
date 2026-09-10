using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Membuat konten dan scene fixture narrative yang dapat diedit. Tidak berjalan otomatis saat Play.</summary>
public static class NarrativeTestingSetupUtility
{
    const string MapScenePath = "Assets/Nature  Paradaise/Map/Scenes/World/Map.unity";
    const string LegacyTestingScenePath = "Assets/Nature  Paradaise/Map/Scenes/Testing/TestingScene.unity";
    const string ContentFolder = "Assets/Nature  Paradaise/Resources/Narrative/Testing";
    const string GardenerPrefabPath = "Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Prefabs/Characters/TFP_Female_Gardener_01A.prefab";

    [MenuItem("Nature Paradise/Narrative/Create Test NPC and UI in Map")]
    public static void SetupTestingScene()
    {
        EnsureFolders();
        AssetDatabase.DeleteAsset($"{ContentFolder}/Quest_TalkToMina.asset");
        ItemSO wood = AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/Nature  Paradaise/Resources/Items/Materials/Wood.asset");
        if (wood == null) throw new System.InvalidOperationException("Item Wood tidak ditemukan.");
        QuestDefinitionSO quest = CreateQuest(wood);
        DialogueSpeakerSO speaker = CreateSpeaker();
        DialogueConversationSO intro = CreateIntroDialogue(speaker, quest);
        DialogueConversationSO active = CreateActiveDialogue(speaker);
        DialogueConversationSO ready = CreateReadyDialogue(speaker, quest);
        DialogueConversationSO completed = CreateCompletedDialogue(speaker);
        AssetDatabase.SaveAssets();

        RemoveLegacyTestingSceneObjects();
        Scene scene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
        Transform systems = FindTransform(scene, "00_SYSTEMS");
        Transform npcs = FindTransform(scene, "NPCs");
        Transform ui = FindTransform(scene, "50_UI");
        if (systems == null || npcs == null || ui == null)
            throw new System.InvalidOperationException("Map harus memiliki root 00_SYSTEMS, NPCs, dan 50_UI.");

        CreateServices(systems, quest);
        CreateNpc(npcs, quest, intro, active, ready, completed);
        CreateNarrativeUI(ui);
        if (Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[NARRATIVE TEST] Mina, quest Kayu untuk Mina, Dialogue UI, Quest Tracker, dan Quest Journal sudah dipasang di Map.");
    }

    static void RemoveLegacyTestingSceneObjects()
    {
        Scene testingScene = EditorSceneManager.OpenScene(LegacyTestingScenePath, OpenSceneMode.Single);
        Transform systems = FindTransform(testingScene, "00_SYSTEMS");
        Transform npcs = FindTransform(testingScene, "NPCs");
        Transform ui = FindTransform(testingScene, "50_UI");
        if (systems != null) DestroyNamed(systems, "NarrativeServices_Editable");
        if (npcs != null) DestroyNamed(npcs, "NPC_QuestTester_Mina_Editable");
        if (ui != null) DestroyNamed(ui, "NarrativeCanvas_Test_Editable");
        EditorSceneManager.MarkSceneDirty(testingScene);
        EditorSceneManager.SaveScene(testingScene);
    }

    static QuestDefinitionSO CreateQuest(ItemSO wood)
    {
        QuestDefinitionSO quest = LoadOrCreate<QuestDefinitionSO>($"{ContentFolder}/Quest_WoodForMina.asset");
        quest.questId = "test.wood_for_mina";
        quest.title = "Kayu untuk Mina";
        quest.description = "Mina membutuhkan kayu. Pegang Axe, tebang pohon, ambil 3 Wood yang jatuh, lalu kembali bicara dengannya.";
        quest.category = "Testing";
        quest.requiredVillageLevel = 1;
        quest.repeatable = false;
        quest.completionMode = QuestCompletionMode.TurnIn;
        quest.objectives = new List<QuestObjectiveDefinition>
        {
            new()
            {
                objectiveId = "collect-wood",
                description = "Ambil Wood dari hasil menebang pohon",
                type = QuestObjectiveType.Collect,
                targetId = wood.name,
                targetItem = wood,
                requiredAmount = 3,
                countExistingInventory = false
            }
        };
        quest.goldReward = 250;
        quest.itemRewards = new List<QuestItemReward>();
        EditorUtility.SetDirty(quest);
        return quest;
    }

    static DialogueSpeakerSO CreateSpeaker()
    {
        DialogueSpeakerSO speaker = LoadOrCreate<DialogueSpeakerSO>($"{ContentFolder}/Speaker_Mina.asset");
        speaker.speakerId = "npc.mina.test";
        speaker.displayName = "Mina";
        speaker.nameColor = new Color(1f, 0.72f, 0.28f, 1f);
        EditorUtility.SetDirty(speaker);
        return speaker;
    }

    static DialogueConversationSO CreateIntroDialogue(DialogueSpeakerSO speaker, QuestDefinitionSO quest)
    {
        DialogueConversationSO dialogue = LoadOrCreate<DialogueConversationSO>($"{ContentFolder}/Dialogue_Mina_Intro.asset");
        dialogue.conversationId = "test.mina.intro";
        dialogue.defaultSpeaker = speaker;
        dialogue.entryNodeId = "start";
        dialogue.nodes = new List<DialogueNode>
        {
            new()
            {
                nodeId = "start",
                text = "Halo! Aku butuh 3 Wood. Pegang Axe dari hotbar, tebang pohon, lalu ambil kayu yang jatuh. Tracker di layar akan mencatat jumlahnya.",
                emotion = "Friendly",
                commands = new List<DialogueCommand>
                {
                    new() { type = DialogueCommandType.StartQuest, quest = quest }
                }
            }
        };
        EditorUtility.SetDirty(dialogue);
        return dialogue;
    }

    static DialogueConversationSO CreateActiveDialogue(DialogueSpeakerSO speaker)
    {
        DialogueConversationSO dialogue = LoadOrCreate<DialogueConversationSO>($"{ContentFolder}/Dialogue_Mina_Active.asset");
        dialogue.conversationId = "test.mina.active";
        dialogue.defaultSpeaker = speaker;
        dialogue.entryNodeId = "start";
        dialogue.nodes = new List<DialogueNode>
        {
            new() { nodeId = "start", text = "Cari pohon yang bisa ditebang, pegang Axe, lalu ambil 3 Wood yang jatuh. Tekan J kapan saja untuk melihat detail quest.", emotion = "Reminder" }
        };
        EditorUtility.SetDirty(dialogue);
        return dialogue;
    }

    static DialogueConversationSO CreateReadyDialogue(DialogueSpeakerSO speaker, QuestDefinitionSO quest)
    {
        DialogueConversationSO dialogue = LoadOrCreate<DialogueConversationSO>($"{ContentFolder}/Dialogue_Mina_Ready.asset");
        dialogue.conversationId = "test.mina.ready";
        dialogue.defaultSpeaker = speaker;
        dialogue.entryNodeId = "start";
        dialogue.nodes = new List<DialogueNode>
        {
            new()
            {
                nodeId = "start",
                text = "Objective sudah selesai. Mau melaporkan quest sekarang?",
                emotion = "Question",
                choices = new List<DialogueChoice>
                {
                    new()
                    {
                        text = "Selesaikan quest (+250 Gold)",
                        commands = new List<DialogueCommand> { new() { type = DialogueCommandType.CompleteQuest, quest = quest } }
                    },
                    new() { text = "Nanti dulu" }
                }
            }
        };
        EditorUtility.SetDirty(dialogue);
        return dialogue;
    }

    static DialogueConversationSO CreateCompletedDialogue(DialogueSpeakerSO speaker)
    {
        DialogueConversationSO dialogue = LoadOrCreate<DialogueConversationSO>($"{ContentFolder}/Dialogue_Mina_Completed.asset");
        dialogue.conversationId = "test.mina.completed";
        dialogue.defaultSpeaker = speaker;
        dialogue.entryNodeId = "start";
        dialogue.nodes = new List<DialogueNode>
        {
            new() { nodeId = "start", text = "Tes selesai! Dialog Completed sekarang punya prioritas dan tidak tertimpa dialog quest sebelumnya.", emotion = "Happy" }
        };
        EditorUtility.SetDirty(dialogue);
        return dialogue;
    }

    static void CreateServices(Transform parent, QuestDefinitionSO quest)
    {
        DestroyNamed(parent, "NarrativeServices_Editable");
        GameObject root = new("NarrativeServices_Editable");
        root.transform.SetParent(parent, false);
        QuestService questService = root.AddComponent<QuestService>();
        root.AddComponent<DialogueService>();
        SerializedObject serializedQuest = new(questService);
        SerializedProperty catalog = serializedQuest.FindProperty("questCatalog");
        catalog.arraySize = 1;
        catalog.GetArrayElementAtIndex(0).objectReferenceValue = quest;
        serializedQuest.ApplyModifiedPropertiesWithoutUndo();
    }

    static void CreateNpc(Transform parent, QuestDefinitionSO quest, DialogueConversationSO intro,
        DialogueConversationSO active, DialogueConversationSO ready, DialogueConversationSO completed)
    {
        DestroyNamed(parent, "NPC_QuestTester_Mina_Editable");
        GameObject root = new("NPC_QuestTester_Mina_Editable");
        root.transform.SetParent(parent, false);
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        Vector3 basePosition = player != null ? player.transform.position : Vector3.zero;
        Vector3 facing = player != null ? player.FacingDirection : Vector3.forward;
        root.transform.position = basePosition + new Vector3(facing.x, 0f, facing.z).normalized * 2.2f;

        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1f, 0f);
        collider.height = 2f;
        collider.radius = 0.45f;

        GameObject gardenerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GardenerPrefabPath);
        if (gardenerPrefab != null)
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(gardenerPrefab, root.transform);
            visual.name = "Visual_Mina_ToonFarm";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
        }

        DialogueTrigger trigger = root.AddComponent<DialogueTrigger>();
        SerializedObject serialized = new(trigger);
        serialized.FindProperty("speakerTargetId").stringValue = "npc.mina.test";
        serialized.FindProperty("promptText").stringValue = "E: Bicara dengan Mina (Quest Test)";
        serialized.FindProperty("promptHeight").floatValue = 2.25f;
        serialized.FindProperty("defaultConversation").objectReferenceValue = intro;
        SerializedProperty conditions = serialized.FindProperty("conditionalConversations");
        conditions.arraySize = 3;
        SetConditionalConversation(conditions.GetArrayElementAtIndex(0), completed, quest, QuestStatus.Completed);
        SetConditionalConversation(conditions.GetArrayElementAtIndex(1), ready, quest, QuestStatus.ReadyToTurnIn);
        SetConditionalConversation(conditions.GetArrayElementAtIndex(2), active, quest, QuestStatus.Active);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetConditionalConversation(SerializedProperty entry, DialogueConversationSO conversation,
        QuestDefinitionSO quest, QuestStatus status)
    {
        entry.FindPropertyRelative("conversation").objectReferenceValue = conversation;
        SerializedProperty conditions = entry.FindPropertyRelative("conditions");
        conditions.arraySize = 1;
        SerializedProperty condition = conditions.GetArrayElementAtIndex(0);
        condition.FindPropertyRelative("type").enumValueIndex = (int)DialogueConditionType.QuestStatus;
        condition.FindPropertyRelative("quest").objectReferenceValue = quest;
        condition.FindPropertyRelative("requiredQuestStatus").enumValueIndex = (int)status;
    }

    static void CreateNarrativeUI(Transform parent)
    {
        DestroyNamed(parent, "NarrativeCanvas_Test_Editable");
        GameObject root = new("NarrativeCanvas_Test_Editable", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(parent, false);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 360;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        BuildQuestTrackerUI(root.transform);
        BuildDialogueUI(root.transform);
        BuildQuestUI(root.transform);
    }

    static void BuildQuestTrackerUI(Transform canvas)
    {
        GameObject controllerObject = CreateRect("QuestTrackerUI", canvas);
        Stretch(controllerObject.GetComponent<RectTransform>());
        QuestTrackerUI controller = controllerObject.AddComponent<QuestTrackerUI>();
        GameObject panel = CreatePanel("ActiveQuestTracker", controllerObject.transform, new Color(0.035f, 0.055f, 0.07f, 0.92f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.025f, 0.49f);
        panelRect.anchorMax = new Vector2(0.34f, 0.73f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        TMP_Text content = CreateText("CurrentQuest", panel.transform, 20f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        content.textWrappingMode = TextWrappingModes.Normal;
        content.margin = new Vector4(18f, 14f, 18f, 14f);
        Stretch(content.rectTransform);

        SerializedObject serialized = new(controller);
        serialized.FindProperty("panelRoot").objectReferenceValue = panel;
        serialized.FindProperty("contentText").objectReferenceValue = content;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BuildDialogueUI(Transform canvas)
    {
        GameObject controllerObject = CreateRect("DialogueUIController", canvas);
        Stretch(controllerObject.GetComponent<RectTransform>());
        DialogueUIController controller = controllerObject.AddComponent<DialogueUIController>();
        GameObject panel = CreatePanel("DialoguePanel", controllerObject.transform, new Color(0.055f, 0.045f, 0.075f, 0.96f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.12f, 0.04f);
        panelRect.anchorMax = new Vector2(0.88f, 0.30f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

        TMP_Text speaker = CreateText("Speaker", panel.transform, 30, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        SetRect(speaker.rectTransform, new Vector2(0.04f, 0.72f), new Vector2(0.70f, 0.94f));
        TMP_Text body = CreateText("Body", panel.transform, 25, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        SetRect(body.rectTransform, new Vector2(0.04f, 0.24f), new Vector2(0.73f, 0.73f));
        body.textWrappingMode = TextWrappingModes.Normal;
        TMP_Text emotion = CreateText("Emotion", panel.transform, 17, FontStyles.Italic, TextAlignmentOptions.TopRight);
        SetRect(emotion.rectTransform, new Vector2(0.72f, 0.74f), new Vector2(0.95f, 0.92f));
        Button continueButton = CreateButton("ContinueButton", panel.transform, "Lanjut  [E / Space]");
        SetRect(continueButton.GetComponent<RectTransform>(), new Vector2(0.73f, 0.08f), new Vector2(0.96f, 0.27f));
        GameObject choices = CreateRect("Choices", panel.transform);
        SetRect(choices.GetComponent<RectTransform>(), new Vector2(0.70f, 0.08f), new Vector2(0.96f, 0.70f));
        VerticalLayoutGroup choiceLayout = choices.AddComponent<VerticalLayoutGroup>();
        choiceLayout.spacing = 8f;
        choiceLayout.childControlHeight = true;
        choiceLayout.childForceExpandHeight = true;
        Button choiceTemplate = CreateButton("ChoiceButtonTemplate", choices.transform, "Pilihan");
        choiceTemplate.gameObject.SetActive(false);

        SerializedObject serialized = new(controller);
        serialized.FindProperty("panelRoot").objectReferenceValue = panel;
        serialized.FindProperty("speakerNameText").objectReferenceValue = speaker;
        serialized.FindProperty("bodyText").objectReferenceValue = body;
        serialized.FindProperty("emotionText").objectReferenceValue = emotion;
        serialized.FindProperty("continueButton").objectReferenceValue = continueButton;
        serialized.FindProperty("choicesRoot").objectReferenceValue = choices.GetComponent<RectTransform>();
        serialized.FindProperty("choiceButtonPrefab").objectReferenceValue = choiceTemplate;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BuildQuestUI(Transform canvas)
    {
        GameObject controllerObject = CreateRect("QuestJournalUI", canvas);
        Stretch(controllerObject.GetComponent<RectTransform>());
        QuestJournalUI controller = controllerObject.AddComponent<QuestJournalUI>();
        GameObject panel = CreatePanel("QuestJournalPanel", controllerObject.transform, new Color(0.035f, 0.055f, 0.07f, 0.98f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.18f, 0.13f);
        panelRect.anchorMax = new Vector2(0.82f, 0.87f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

        TMP_Text heading = CreateText("Heading", panel.transform, 34, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        heading.text = "QUEST JOURNAL  [J: Tutup]";
        SetRect(heading.rectTransform, new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.97f));

        GameObject list = CreateRect("QuestList", panel.transform);
        SetRect(list.GetComponent<RectTransform>(), new Vector2(0.04f, 0.08f), new Vector2(0.36f, 0.85f));
        VerticalLayoutGroup listLayout = list.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 9f;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandHeight = false;
        Button questTemplate = CreateButton("QuestButtonTemplate", list.transform, "Quest");
        LayoutElement templateLayout = questTemplate.gameObject.AddComponent<LayoutElement>();
        templateLayout.preferredHeight = 62f;
        questTemplate.gameObject.SetActive(false);

        TMP_Text title = CreateText("QuestTitle", panel.transform, 30, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        SetRect(title.rectTransform, new Vector2(0.40f, 0.77f), new Vector2(0.94f, 0.86f));
        TMP_Text status = CreateText("QuestStatus", panel.transform, 20, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        status.color = new Color(0.35f, 0.9f, 0.65f);
        SetRect(status.rectTransform, new Vector2(0.40f, 0.70f), new Vector2(0.94f, 0.77f));
        TMP_Text description = CreateText("Description", panel.transform, 22, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        description.textWrappingMode = TextWrappingModes.Normal;
        SetRect(description.rectTransform, new Vector2(0.40f, 0.52f), new Vector2(0.94f, 0.70f));
        TMP_Text objectives = CreateText("Objectives", panel.transform, 22, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        SetRect(objectives.rectTransform, new Vector2(0.40f, 0.28f), new Vector2(0.94f, 0.50f));
        TMP_Text rewards = CreateText("Rewards", panel.transform, 21, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        SetRect(rewards.rectTransform, new Vector2(0.40f, 0.14f), new Vector2(0.70f, 0.27f));
        Button turnIn = CreateButton("TurnInButton", panel.transform, "Laporkan Quest");
        SetRect(turnIn.GetComponent<RectTransform>(), new Vector2(0.72f, 0.10f), new Vector2(0.94f, 0.21f));

        SerializedObject serialized = new(controller);
        serialized.FindProperty("panelRoot").objectReferenceValue = panel;
        serialized.FindProperty("questListRoot").objectReferenceValue = list.GetComponent<RectTransform>();
        serialized.FindProperty("questButtonPrefab").objectReferenceValue = questTemplate;
        serialized.FindProperty("titleText").objectReferenceValue = title;
        serialized.FindProperty("statusText").objectReferenceValue = status;
        serialized.FindProperty("descriptionText").objectReferenceValue = description;
        serialized.FindProperty("objectivesText").objectReferenceValue = objectives;
        serialized.FindProperty("rewardsText").objectReferenceValue = rewards;
        serialized.FindProperty("turnInButton").objectReferenceValue = turnIn;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject CreateRect(string name, Transform parent)
    {
        GameObject result = new(name, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        return result;
    }

    static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    static TMP_Text CreateText(string name, Transform parent, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    static Button CreateButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.12f, 0.32f, 0.38f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.2f, 0.55f, 0.58f, 1f);
        colors.pressedColor = new Color(0.1f, 0.7f, 0.52f, 1f);
        button.colors = colors;
        TMP_Text text = CreateText("Label", buttonObject.transform, 20f, FontStyles.Bold, TextAlignmentOptions.Center);
        text.text = label;
        Stretch(text.rectTransform);
        return button;
    }

    static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(8f, 5f);
        rect.offsetMax = new Vector2(-8f, -5f);
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets/Nature  Paradaise/Resources", "Narrative");
        EnsureFolder("Assets/Nature  Paradaise/Resources/Narrative", "Testing");
    }

    static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }

    static Transform FindTransform(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindRecursive(root.transform, name);
            if (found != null) return found;
        }
        return null;
    }

    static Transform FindRecursive(Transform current, string name)
    {
        if (current.name == name) return current;
        for (int i = 0; i < current.childCount; i++)
        {
            Transform found = FindRecursive(current.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static void DestroyNamed(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
    }
}
