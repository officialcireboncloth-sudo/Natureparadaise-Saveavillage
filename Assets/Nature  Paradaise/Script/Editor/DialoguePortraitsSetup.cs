#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Imports the approved portrait atlases and binds speaker assets without saving scenes.</summary>
public static class DialoguePortraitsSetup
{
    const string Root = "Assets/Nature  Paradaise";
    const string Art = Root + "/UI/Dialogue/Portraits";
    [MenuItem("Nature Paradise/UI/Dialogue/Validate Character Portraits")]
    public static void Validate()
    {
        var speakers = new[] { Resources.Load<DialogueSpeakerSO>("Narrative/Speaker_Player"), Resources.Load<DialogueSpeakerSO>("Narrative/Testing/Speaker_Mina") };
        foreach (var speaker in speakers)
        {
            if (speaker == null || speaker.expressions.Count != 6 || speaker.portrait == null) throw new InvalidOperationException("Speaker portraits are incomplete");
            foreach (DialogueExpression expression in Enum.GetValues(typeof(DialogueExpression)))
                if (speaker.GetPortrait(expression.ToString()) == null) throw new InvalidOperationException("Missing expression " + expression);
        }
        if (!speakers[0].isPlayer || Resources.Load<DialogueConversationSO>("Narrative/Testing/Dialogue_PortraitPreview") == null)
            throw new InvalidOperationException("Player or dialogue preview is missing");
        const string evidence = "ArtSource/Dialogue/PlayerPortraitApply_2026-10-29";
        Directory.CreateDirectory(evidence);
        File.WriteAllText(evidence + "/UnityApplied.txt", "PASS Player + Mina, twelve expression sprites, player binding and preview imported in main Unity.\n");
        Debug.Log("[DIALOGUE] Character portrait bindings validated.");
    }
    [MenuItem("Nature Paradise/UI/Dialogue/Apply Character Portraits")]
    public static void Apply()
    {
        var playerSprites = ImportAtlas(Art + "/Player/Player_Expressions.png", "Player");
        var minaSprites = ImportAtlas(Art + "/Mina/Mina_Expressions.png", "Mina");
        var player = LoadOrCreate<DialogueSpeakerSO>(Root + "/Resources/Narrative/Speaker_Player.asset");
        player.speakerId = "player"; player.displayName = "Player"; player.isPlayer = true;
        Assign(player, playerSprites);
        var mina = AssetDatabase.LoadAssetAtPath<DialogueSpeakerSO>(Root + "/Resources/Narrative/Testing/Speaker_Mina.asset");
        if (mina == null) throw new InvalidOperationException("Mina speaker asset missing");
        Assign(mina, minaSprites);
        CreatePreview(player, mina);
        var theme = LoadOrCreate<DialogueTheme>(Root + "/Resources/UI/DialogueTheme.asset");
        theme.panelColor = new(.97f, .94f, .86f, 1); theme.textColor = new(.19f, .16f, .12f, 1);
        theme.accentColor = new(.40f, .48f, .34f, 1); theme.listenerBrightness = .68f;
        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();
        Debug.Log("[DIALOGUE] Player + Mina portraits applied, six expressions each. No scene saved.");
    }
    static void CreatePreview(DialogueSpeakerSO player, DialogueSpeakerSO mina)
    {
        var preview = LoadOrCreate<DialogueConversationSO>(Root + "/Resources/Narrative/Testing/Dialogue_PortraitPreview.asset");
        preview.conversationId = "testing.portrait_expressions";
        preview.defaultSpeaker = mina; preview.playerSpeaker = player; preview.counterpartSpeaker = mina;
        preview.entryNodeId = "portrait-0"; preview.nodes = new List<DialogueNode>();
        string[] lines = { "Aku siap mendengarkan.", "Senang bertemu denganmu!", "Hmm, sepertinya ada yang perlu kita perbaiki.", "Sayang sekali... semoga besok lebih baik.", "Wah! Aku tidak menyangka!", "Sebentar, aku pikirkan dulu." };
        for (int i = 0; i < 12; i++) preview.nodes.Add(new()
        {
            nodeId = "portrait-" + i, speakerOverride = i < 6 ? mina : player,
            emotion = ((DialogueExpression)(i % 6)).ToString(), text = lines[i % 6],
            nextNodeId = i == 11 ? "" : "portrait-" + (i + 1)
        });
        EditorUtility.SetDirty(preview);
    }
    [MenuItem("Nature Paradise/UI/Dialogue/Preview Expressions (Play Mode)")]
    static void Preview()
    {
        var service = DialogueService.Instance;
        if (!EditorApplication.isPlaying || service == null) { Debug.Log("Jalankan Play Mode di scene game untuk preview dialog."); return; }
        service.StartConversation(Resources.Load<DialogueConversationSO>("Narrative/Testing/Dialogue_PortraitPreview"));
    }
    static void Assign(DialogueSpeakerSO speaker, Sprite[] sprites)
    {
        speaker.portrait = sprites[0];
        speaker.expressions = new List<DialogueExpressionPortrait>();
        for (int i = 0; i < 6; i++) speaker.expressions.Add(new() { expression = (DialogueExpression)i, portrait = sprites[i] });
        EditorUtility.SetDirty(speaker);
    }
    static Sprite[] ImportAtlas(string path, string prefix)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Portrait atlas missing: " + path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false; importer.isReadable = false; importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Compressed; importer.compressionQuality = 70;
        importer.spritePixelsPerUnit = 100;
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        if (width * 2 != height * 3) throw new InvalidOperationException("Atlas must contain a 3 x 2 square grid: " + path);
        string[] names = Enum.GetNames(typeof(DialogueExpression));
        var grid = new SpriteMetaData[6];
        for (int i = 0; i < 6; i++) grid[i] = new SpriteMetaData
        {
            name = prefix + "_" + names[i], rect = new Rect(i % 3 * width / 3, (1 - i / 3) * height / 2, width / 3, height / 2),
            alignment = (int)SpriteAlignment.BottomCenter, pivot = new(.5f, 0)
        };
#pragma warning disable CS0618
        importer.spritesheet = grid;
#pragma warning restore CS0618
        foreach (var platform in new[] { "Android", "iPhone" }) importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
        {
            name = platform, overridden = true, maxTextureSize = 2048,
            format = platform == "Android" ? TextureImporterFormat.ETC2_RGBA8 : TextureImporterFormat.ASTC_6x6,
            textureCompression = TextureImporterCompression.Compressed, compressionQuality = 70
        });
        importer.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        return names.Select(name => sprites.Single(s => s.name == prefix + "_" + name)).ToArray();
    }
    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
    }
}
#endif
