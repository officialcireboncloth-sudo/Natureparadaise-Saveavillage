#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NatureParadise.EditorTools
{
    [InitializeOnLoad]
    public static class MapGameplayMigrationUtility
    {
        private const string TestingScenePath = "Assets/Nature  Paradaise/Map/Scenes/Testing/TestingScene.unity";
        private const string MapScenePath = "Assets/Nature  Paradaise/Map/Scenes/World/Map.unity";
        private const string PreviewScenePath = "Assets/Nature  Paradaise/Map/Scenes/World/Map_GameplayPreview.unity";
        private const string TemporaryScenePath = "Assets/Nature  Paradaise/Map/Scenes/World/__MapGameplayMigrationTemp.unity";
        private const string SessionKey = "NatureParadise.MapGameplayMigration.Attempted";

        private static readonly string[] GameplayRootsToAlign =
        {
            "10_PLAYER",
            "20_CAMERA_LIGHTING",
            "30_WORLD",
            "40_SPAWN_POINTS"
        };

        static MapGameplayMigrationUtility()
        {
            EditorApplication.delayCall += TryAutomaticMigration;
        }

        [MenuItem("Nature Paradise/Map/Generate Gameplay Map Preview")]
        private static void MigrateFromMenu()
        {
            Migrate(force: true);
        }

        public static void MigrateFromCommandLine()
        {
            Migrate(force: true);
        }

        [MenuItem("Nature Paradise/Map/Apply Gameplay Preview To Main Map")]
        public static void PromotePreviewToMainMap()
        {
            if (!File.Exists(PreviewScenePath))
                throw new FileNotFoundException("Map_GameplayPreview.unity belum dibuat.", PreviewScenePath);

            File.Copy(PreviewScenePath, MapScenePath, overwrite: true);
            AssetDatabase.ImportAsset(MapScenePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            Debug.Log("[Map Migration] Map utama sudah diperbarui dari Map_GameplayPreview. GUID Map.unity tetap dipertahankan.");
        }

        private static void TryAutomaticMigration()
        {
            if (SessionState.GetBool(SessionKey, false))
                return;

            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryAutomaticMigration;
                return;
            }

            SessionState.SetBool(SessionKey, true);
            Migrate(force: false);
        }

        private static void Migrate(bool force)
        {
            if (!File.Exists(TestingScenePath) || !File.Exists(MapScenePath))
            {
                Debug.LogError("[Map Migration] TestingScene atau Map tidak ditemukan.");
                return;
            }

            if (!force && MapAlreadyContainsGameplay())
            {
                Debug.Log("[Map Migration] Map sudah berisi hierarchy gameplay. Migrasi dilewati.");
                return;
            }

            Scene migratedScene = default;

            try
            {
                EditorSceneManager.SaveOpenScenes();
                CleanupTemporarySceneAsset();

                if (!AssetDatabase.CopyAsset(TestingScenePath, TemporaryScenePath))
                    throw new InvalidOperationException("Gagal membuat salinan kerja TestingScene.");

                AssetDatabase.Refresh();

                migratedScene = EditorSceneManager.OpenScene(TemporaryScenePath, OpenSceneMode.Additive);
                Scene mapSourceScene = SceneManager.GetSceneByPath(MapScenePath);
                if (!mapSourceScene.isLoaded)
                    mapSourceScene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Additive);

                float testingGroundY = FindGroundHeight(migratedScene, Vector3.zero, terrainName: "Terrain_Testing", fallback: 0f);
                List<GameObject> mapContent = CloneMapContent(mapSourceScene, migratedScene);
                float mapGroundY = FindGroundHeight(migratedScene, Vector3.zero, terrainName: "Terrain_Map", fallback: testingGroundY);

                AlignGameplayToMapGround(migratedScene, mapGroundY - testingGroundY);
                RemoveTestingOnlyContent(migratedScene);
                OrganizeHierarchy(migratedScene, mapContent);

                SceneManager.SetActiveScene(migratedScene);
                EditorSceneManager.CloseScene(mapSourceScene, removeScene: true);

                if (!EditorSceneManager.SaveScene(migratedScene, PreviewScenePath, saveAsCopy: false))
                    throw new InvalidOperationException("Unity gagal menyimpan hasil migrasi ke Map_GameplayPreview.unity.");

                CleanupTemporarySceneAsset();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorSceneManager.CloseScene(migratedScene, removeScene: true);

                Debug.Log("[Map Migration] Preview selesai: buka Map_GameplayPreview untuk meninjau gameplay dan hierarchy baru.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                CleanupTemporarySceneAsset();
            }
        }

        private static bool MapAlreadyContainsGameplay()
        {
            if (!File.Exists(PreviewScenePath))
                return false;

            string yaml = File.ReadAllText(PreviewScenePath);
            return yaml.Contains("m_Name: 00_SYSTEMS") &&
                   yaml.Contains("m_Name: 10_PLAYER") &&
                   yaml.Contains("m_Name: 30_WORLD") &&
                   yaml.Contains("m_Name: 50_UI");
        }

        private static List<GameObject> CloneMapContent(Scene source, Scene destination)
        {
            var result = new List<GameObject>();

            foreach (GameObject root in source.GetRootGameObjects())
            {
                if (root.name == "Core&Lighting")
                    continue;

                GameObject clone = UnityEngine.Object.Instantiate(root);
                clone.name = root.name;
                SceneManager.MoveGameObjectToScene(clone, destination);

                if (clone.name == "Terrain")
                    clone.name = "Terrain_Map";

                result.Add(clone);
            }

            return result;
        }

        private static void AlignGameplayToMapGround(Scene scene, float verticalOffset)
        {
            if (Mathf.Abs(verticalOffset) < 0.001f)
                return;

            foreach (string rootName in GameplayRootsToAlign)
            {
                GameObject root = FindRoot(scene, rootName);
                if (root != null)
                    root.transform.position += Vector3.up * verticalOffset;
            }
        }

        private static void RemoveTestingOnlyContent(Scene scene)
        {
            DestroyIfPresent(FindRoot(scene, "90_TESTING_WORKSPACE"));
            DestroyIfPresent(FindRoot(scene, "Terrain"));
            DestroyIfPresent(FindRoot(scene, "TFP_Leafy_Plant_01A_TerrainDetail"));

            GameObject world = FindRoot(scene, "30_WORLD");
            if (world == null)
                return;

            Transform testingTerrain = FindDeepChild(world.transform, "Terrain_Testing");
            if (testingTerrain != null)
                UnityEngine.Object.DestroyImmediate(testingTerrain.gameObject);
        }

        private static void OrganizeHierarchy(Scene scene, List<GameObject> mapContent)
        {
            GameObject systems = GetOrCreateRoot(scene, "00_SYSTEMS");
            GameObject player = GetOrCreateRoot(scene, "10_PLAYER");
            GameObject cameraLighting = GetOrCreateRoot(scene, "20_CAMERA_LIGHTING");
            GameObject world = GetOrCreateRoot(scene, "30_WORLD");
            GameObject spawnPoints = GetOrCreateRoot(scene, "40_SPAWN_POINTS");
            GameObject ui = GetOrCreateRoot(scene, "50_UI");

            SetSiblingOrder(systems, player, cameraLighting, world, spawnPoints, ui);

            Transform environment = GetOrCreateChild(world.transform, "Environment");
            Transform buildings = GetOrCreateChild(world.transform, "Buildings");
            Transform farming = GetOrCreateChild(world.transform, "Farming");
            Transform animals = GetOrCreateChild(world.transform, "Animals");
            Transform npcs = GetOrCreateChild(world.transform, "NPCs");
            SetSiblingOrder(environment.gameObject, buildings.gameObject, farming.gameObject, animals.gameObject, npcs.gameObject);

            Transform vegetation = GetOrCreateChild(environment, "Vegetation");
            Transform rocks = GetOrCreateChild(environment, "Rocks");

            foreach (GameObject content in mapContent.Where(item => item != null))
            {
                if (content.name == "Terrain_Map")
                {
                    content.transform.SetParent(environment, worldPositionStays: true);
                    content.transform.SetSiblingIndex(0);
                    continue;
                }

                if (content.name == "Object")
                {
                    Transform manMade = FindDirectChild(content.transform, "ManMade");
                    Transform nature = FindDirectChild(content.transform, "Nature");

                    if (manMade != null)
                    {
                        manMade.name = "Map_Existing";
                        manMade.SetParent(buildings, worldPositionStays: true);
                    }

                    if (nature != null)
                    {
                        nature.name = "Map_Nature";
                        nature.SetParent(vegetation, worldPositionStays: true);
                    }

                    while (content.transform.childCount > 0)
                        content.transform.GetChild(0).SetParent(environment, worldPositionStays: true);

                    UnityEngine.Object.DestroyImmediate(content);
                    continue;
                }

                content.transform.SetParent(rocks, worldPositionStays: true);
            }

            RenameIfPresent(world.transform, "Carpenter_Dummy_Editable", "Carpenter_Editable");
            RenameIfPresent(world.transform, "FertilizerProcessor_Dummy", "FertilizerProcessor_Editable");
            RenameIfPresent(world.transform, "HoeMark", "HoeMark_EditorGuide");

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static float FindGroundHeight(Scene scene, Vector3 position, string terrainName, float fallback)
        {
            Terrain preferred = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Terrain>(includeInactive: true))
                .FirstOrDefault(terrain => terrain.gameObject.name == terrainName);

            if (preferred != null && ContainsXZ(preferred, position))
                return preferred.SampleHeight(position) + preferred.transform.position.y;

            Terrain containing = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Terrain>(includeInactive: true))
                .FirstOrDefault(terrain => ContainsXZ(terrain, position));

            return containing != null
                ? containing.SampleHeight(position) + containing.transform.position.y
                : fallback;
        }

        private static bool ContainsXZ(Terrain terrain, Vector3 position)
        {
            if (terrain == null || terrain.terrainData == null)
                return false;

            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            return position.x >= origin.x && position.x <= origin.x + size.x &&
                   position.z >= origin.z && position.z <= origin.z + size.z;
        }

        private static GameObject GetOrCreateRoot(Scene scene, string name)
        {
            GameObject existing = FindRoot(scene, name);
            if (existing != null)
                return existing;

            var created = new GameObject(name);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static Transform GetOrCreateChild(Transform parent, string name)
        {
            Transform child = FindDirectChild(parent, name);
            if (child != null)
                return child;

            var created = new GameObject(name);
            created.transform.SetParent(parent, worldPositionStays: false);
            return created.transform;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (child.name == name)
                    return child;
            }

            return null;
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;

                Transform nested = FindDeepChild(child, name);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static void RenameIfPresent(Transform parent, string oldName, string newName)
        {
            Transform child = FindDeepChild(parent, oldName);
            if (child != null)
                child.name = newName;
        }

        private static void SetSiblingOrder(params GameObject[] objects)
        {
            for (int index = 0; index < objects.Length; index++)
                objects[index].transform.SetSiblingIndex(index);
        }

        private static void DestroyIfPresent(GameObject gameObject)
        {
            if (gameObject != null)
                UnityEngine.Object.DestroyImmediate(gameObject);
        }

        private static void CleanupTemporarySceneAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TemporaryScenePath) != null)
                AssetDatabase.DeleteAsset(TemporaryScenePath);
        }
    }
}
#endif
