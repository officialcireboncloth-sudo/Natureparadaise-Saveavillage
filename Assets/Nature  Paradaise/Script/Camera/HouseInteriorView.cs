using System.Collections.Generic;
using UnityEngine;

/// <summary>Uses the scene-authored perspective camera for the active house layout.</summary>
[DefaultExecutionOrder(1000)]
public sealed class HouseInteriorView : MonoBehaviour
{
    [SerializeField] List<Camera> levelViews = new();
    Camera gameplayCamera;
    TopDownCameraFollow worldFollow;
    bool captured, followEnabled, wasOrthographic;
    float worldSize, worldFov;
    Vector3 worldPosition;
    Quaternion worldRotation;
    public Camera ActiveView => levelViews.Find(view => view != null && view.gameObject.activeInHierarchy);
    public void Configure(List<Camera> views) => levelViews = views;

    void LateUpdate()
    {
        var transition = SceneTransitionManager.Instance;
        if(transition == null || !transition.IsInsideInterior || transition.CurrentInteriorSceneName != gameObject.scene.name)
        {
            RestoreWorldView();
            return;
        }
        var view = ActiveView;
        if(view == null) return;
        if(!captured)
        {
            gameplayCamera = Camera.main;
            if(gameplayCamera == null) return;
            worldFollow = gameplayCamera.GetComponent<TopDownCameraFollow>();
            followEnabled = worldFollow != null && worldFollow.enabled;
            wasOrthographic = gameplayCamera.orthographic;
            worldSize = gameplayCamera.orthographicSize;
            worldFov = gameplayCamera.fieldOfView;
            worldPosition = gameplayCamera.transform.position;
            worldRotation = gameplayCamera.transform.rotation;
            if(worldFollow != null) worldFollow.enabled = false;
            captured = true;
        }
        gameplayCamera.transform.SetPositionAndRotation(view.transform.position, view.transform.rotation);
        gameplayCamera.orthographic = false;
        float aspectScale = Mathf.Max(1f, (16f / 9f) / Mathf.Max(.1f, gameplayCamera.aspect));
        gameplayCamera.fieldOfView = 2f * Mathf.Atan(Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * .5f) * aspectScale) * Mathf.Rad2Deg;
    }

    void OnDisable() => RestoreWorldView();
    void RestoreWorldView()
    {
        if(!captured || gameplayCamera == null) return;
        gameplayCamera.orthographic = wasOrthographic;
        gameplayCamera.orthographicSize = worldSize;
        gameplayCamera.fieldOfView = worldFov;
        gameplayCamera.transform.SetPositionAndRotation(worldPosition,worldRotation);
        if(worldFollow != null)
        {
            worldFollow.enabled = followEnabled;
            worldFollow.SetTarget(worldFollow.Target, true);
        }
        captured = false;
    }
}
