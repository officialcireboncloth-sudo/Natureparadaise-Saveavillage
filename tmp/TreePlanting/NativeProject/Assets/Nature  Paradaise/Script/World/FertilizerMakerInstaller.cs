using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Place the atlas-textured station beside Field 1 and upgrade recognized placeholders.</summary>
public static class FertilizerMakerInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded;
    }
    static void Loaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Map" && scene.name != "Map_GameplayPreview") return;
        Install(scene);
    }
    public static FertilizerProcessor Install(Scene scene)
    {
        FieldArea main = null; SoilScannerMachine scanner = null; FertilizerProcessor existing = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var foundProcessor = root.GetComponentInChildren<FertilizerProcessor>(true); if (foundProcessor != null && existing == null) existing = foundProcessor;
            foreach (var field in root.GetComponentsInChildren<FieldArea>(true)) if (field.FieldNumber == 1) main = field;
            var found = root.GetComponentInChildren<SoilScannerMachine>(true); if (found != null) scanner = found;
        }
        if (main == null) return existing;
        var prefab = Resources.Load<GameObject>("World/FertilizerMaker_Dummy"); if (prefab == null) return existing;
        // Old maps contain an empty testing placeholder far from the actual farm.
        // Upgrade that object in place so its stable processor/save identity is retained.
        var oldVisual = existing != null ? existing.transform.Find("ProcessorVisual_ReplaceMe") ??
            existing.transform.Find("Visual_Dummy_ReplaceLater") : null;
        bool relocate = existing == null || existing.GetComponentInChildren<Renderer>() == null ||
            existing.transform.Find("ProcessorVisual_ReplaceMe") != null;
        bool blank = existing != null && (existing.GetComponentInChildren<Renderer>() == null || oldVisual != null);
        if (existing != null && !blank) return existing;
        GameObject instance;
        if (existing != null)
        {
            instance = existing.gameObject;
            if (oldVisual != null)
            {
                oldVisual.gameObject.SetActive(false);
                if(Application.isPlaying)Object.Destroy(oldVisual.gameObject);else Object.DestroyImmediate(oldVisual.gameObject);
            }
            var visual=prefab.transform.GetChild(0);
            Object.Instantiate(visual.gameObject, instance.transform, false).name=visual.name;
            var body=instance.GetComponent<BoxCollider>();if(body==null)body=instance.AddComponent<BoxCollider>();
            body.center=prefab.GetComponent<BoxCollider>().center;body.size=prefab.GetComponent<BoxCollider>().size;
        }
        else { instance = Object.Instantiate(prefab); SceneManager.MoveGameObjectToScene(instance, scene); }
        instance.name = "Pembuat Pupuk";
        if(!relocate)return instance.GetComponent<FertilizerProcessor>();
        var point = scanner != null ? scanner.transform.position + scanner.transform.right * 1.8f :
            main.transform.TransformPoint(new Vector3(-main.Columns * main.CellSize * .5f - 1.25f, 0, -2));
        instance.transform.position = FieldGroundSurface.GroundPosition(point);
        instance.transform.rotation = scanner != null ? scanner.transform.rotation : Quaternion.Euler(0, main.transform.eulerAngles.y, 0);
        return instance.GetComponent<FertilizerProcessor>();
    }
}
