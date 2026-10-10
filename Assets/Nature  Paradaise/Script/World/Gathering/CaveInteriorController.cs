using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Authorable cave marker. Entry and return use the same additive transition as houses.</summary>
[DisallowMultipleComponent]
public sealed class CaveInteriorController : MonoBehaviour
{
    static readonly System.Collections.Generic.List<CaveInteriorController> caves = new();
    public static bool IsMiningContext { get { foreach(var cave in caves) if(cave!=null && cave.IsCurrentInterior) return true; return false; } }
    void OnEnable() { if(!caves.Contains(this)) caves.Add(this); }
    void OnDisable() { caves.Remove(this); }
    [SerializeField] string mineName = "Tambang";
    [SerializeField, Min(1)] int floor = 1;
    public string MineName => mineName;
    public int Floor => floor;
    public bool IsCurrentInterior
    {
        get
        {
            if(!isActiveAndEnabled) return false;
            var transition = SceneTransitionManager.Instance;
            if (transition != null && transition.IsInsideInterior)
                return transition.CurrentInteriorSceneName == gameObject.scene.name;
            return SceneManager.GetActiveScene() == gameObject.scene &&
                FindFirstObjectByType<PlayerController>() is PlayerController player && player.gameObject.scene == gameObject.scene;
        }
    }
    void Awake() { if(GetComponent<MiningHUD>()==null) gameObject.AddComponent<MiningHUD>(); }
}
