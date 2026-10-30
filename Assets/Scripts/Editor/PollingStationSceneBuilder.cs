using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds greybox sports-hall layout (see docs/references/SchoolSportsHall/specs.md)
/// and player/HUD prefabs for PollingStation scene.
/// </summary>
public static class PollingStationSceneBuilder
{
    const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    const string NewScenePath = "Assets/Scenes/PollingStation.unity";
    const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    const string PlayerHudPrefabPath = "Assets/Prefabs/PlayerHUD.prefab";
    const string EventSystemPrefabPath = "Assets/Prefabs/EventSystem.prefab";

    // Specs: length 24 m (long walls), width 12 m (ends), height 6 m
    const float RoomWidth = 12f;
    const float RoomLength = 24f;
    const float RoomHeight = 6f;
    const float WallThickness = 0.3f;

    // Windows (one long wall): 1.5 × ~2.75, sill ~2.2 m, 6 openings
    const int WindowCount = 6;
    const float WindowWidth = 1.5f;
    const float WindowHeight = 2.75f;
    const float WindowSill = 2.2f;

    // Doors (opposite long wall)
    const float MainDoorW = 1.7f;
    const float MainDoorH = 2.1f;
    const float SingleDoorW = 0.95f;
    const float SingleDoorH = 2.1f;
    const float DoorLeafThickness = 0.06f;

    // Human-scale player (was CC height 2 / radius 0.5 — felt oversized in hall)
    const float PlayerHeight = 1.75f;
    const float PlayerRadius = 0.32f;
    const float PlayerEyeHeight = 1.55f;

    [MenuItem("Voting Sim/Build Polling Station Greybox Scene")]
    public static void BuildFromMenu() => Build();

    /// <summary>Entry: unity run -- -executeMethod PollingStationSceneBuilder.Build</summary>
    public static void Build()
    {
        EnsureFolders();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
        {
            if (!CreatePrefabsFromSampleScene())
            {
                Debug.LogError("[PollingStationSceneBuilder] Failed to create prefabs from SampleScene.");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                return;
            }
        }

        ScalePlayerPrefab();
        CreatePollingStationScene();
        AddSceneToBuildSettings(NewScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PollingStationSceneBuilder] Done. Prefabs + scene at {NewScenePath}");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    static bool CreatePrefabsFromSampleScene()
    {
        var sample = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);

        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("Player not found in SampleScene.");
            return false;
        }

        PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);

        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
            PrefabUtility.SaveAsPrefabAsset(canvas, PlayerHudPrefabPath);

        var eventSystem = GameObject.Find("EventSystem");
        if (eventSystem != null)
            PrefabUtility.SaveAsPrefabAsset(eventSystem, EventSystemPrefabPath);

        EditorSceneManager.CloseScene(sample, true);
        return AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) != null;
    }

    static void ScalePlayerPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

        var cc = root.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.height = PlayerHeight;
            cc.radius = PlayerRadius;
            cc.center = new Vector3(0f, PlayerHeight * 0.5f, 0f);
            cc.skinWidth = 0.06f;
            cc.stepOffset = 0.25f;
        }

        var capsule = root.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            capsule.height = PlayerHeight;
            capsule.radius = PlayerRadius;
            capsule.center = new Vector3(0f, PlayerHeight * 0.5f, 0f);
        }

        // Default capsule mesh is 2 m tall, radius 0.5
        var body = root.transform.Find("Body");
        if (body != null)
        {
            float yScale = PlayerHeight / 2f;
            float xzScale = PlayerRadius / 0.5f;
            body.localPosition = new Vector3(0f, PlayerHeight * 0.5f, 0f);
            body.localScale = new Vector3(xzScale, yScale, xzScale);
        }

        var eyes = root.transform.Find("Eyes");
        if (eyes != null)
        {
            eyes.localPosition = new Vector3(0f, PlayerEyeHeight, 0.35f);
            eyes.localScale = new Vector3(0.28f, 0.1f, 0.1f);
        }

        var cam = root.transform.Find("Main Camera");
        if (cam != null)
            cam.localPosition = new Vector3(0f, PlayerEyeHeight, 0.15f);

        var hold = root.transform.Find("HoldPosition");
        if (hold != null)
            hold.localPosition = new Vector3(0.28f, PlayerEyeHeight - 0.35f, 0.7f);

        var drop = root.transform.Find("DropFromInventory");
        if (drop != null)
            drop.localPosition = new Vector3(0f, 0.85f, 0.6f);

        PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log($"Scaled Player prefab to height={PlayerHeight}, radius={PlayerRadius}");
    }

    static void CreatePollingStationScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateLighting();

        var matFloor = LoadMat("Assets/Materials/LightGrey.mat");
        var matWall = LoadMat("Assets/Materials/White.mat");
        var matDoor = LoadMat("Assets/Materials/Black.mat");

        var root = new GameObject("Greybox_Hall");
        float halfW = RoomWidth * 0.5f;
        float halfL = RoomLength * 0.5f;
        float t = WallThickness;

        CreateBox(root.transform, "Floor",
            new Vector3(0f, -t * 0.5f, 0f),
            new Vector3(RoomWidth, t, RoomLength),
            matFloor);

        CreateBox(root.transform, "Ceiling",
            new Vector3(0f, RoomHeight + t * 0.5f, 0f),
            new Vector3(RoomWidth, t, RoomLength),
            matWall);

        // Short end walls (12 m) — solid
        CreateBox(root.transform, "Wall_South",
            new Vector3(0f, RoomHeight * 0.5f, -halfL - t * 0.5f),
            new Vector3(RoomWidth, RoomHeight, t), matWall);
        CreateBox(root.transform, "Wall_North",
            new Vector3(0f, RoomHeight * 0.5f, halfL + t * 0.5f),
            new Vector3(RoomWidth, RoomHeight, t), matWall);

        // West long wall (−X): windows only
        float westX = -halfW - t * 0.5f;
        BuildWindowWallAlongZ(root.transform, "Wall_West", westX, matWall);

        // East long wall (+X): all doors
        float eastX = halfW + t * 0.5f;
        BuildDoorWallAlongZ(root.transform, "Wall_East", eastX, matWall, matDoor);

        // Markers
        CreateEmptyMarker(root.transform, "SpawnPoint", Vector3.zero);
        CreateEmptyMarker(root.transform, "MainEntranceMarker",
            new Vector3(halfW - 1.5f, 0f, -halfL + 1.5f));
        CreateEmptyMarker(root.transform, "EmergencyExitMarker",
            new Vector3(halfW - 1.5f, 0f, halfL - 1.5f));
        CreateEmptyMarker(root.transform, "CoachDoorMarker",
            new Vector3(halfW - 1.5f, 0f, -2.5f));
        CreateEmptyMarker(root.transform, "InventoryDoorMarker",
            new Vector3(halfW - 1.5f, 0f, -4.5f));

        SpawnPlayerAndHud();

        EditorSceneManager.SaveScene(scene, NewScenePath);
        Debug.Log($"Saved scene {NewScenePath}");
    }

    /// <summary>
    /// 6 equally spaced windows on a longitudinal wall (along Z).
    /// </summary>
    static void BuildWindowWallAlongZ(Transform parent, string prefix, float x, Material mat)
    {
        float halfL = RoomLength * 0.5f;
        // float t = WallThickness;
        float usable = RoomLength - 2f; // margins from corners
        float step = usable / (WindowCount + 1);

        var centers = new float[WindowCount];
        for (int i = 0; i < WindowCount; i++)
            centers[i] = -halfL + 1f + step * (i + 1);

        BuildSegmentedWallAlongZ(parent, prefix, x, mat, WindowWidth, WindowHeight, WindowSill, centers);
    }

    /// <summary>
    /// Doors on opposite long wall:
    /// main entrance SW corner, emergency NE corner, coach + inventory near center toward main.
    /// Door leaves = simple boxes (no detail). Openings are walkable (leaf colliders off).
    /// </summary>
    static void BuildDoorWallAlongZ(Transform parent, string prefix, float x, Material wallMat, Material doorMat)
    {
        float halfL = RoomLength * 0.5f;

        // Centers along Z (main near -Z corner, emergency near +Z, service nearer main)
        float mainZ = -halfL + 1.2f + MainDoorW * 0.5f;
        float inventoryZ = -4.5f;
        float coachZ = -2.5f;
        float emergencyZ = halfL - 1.2f - SingleDoorW * 0.5f;

        var openings = new[]
        {
            new Opening { Name = "MainEntrance", CenterZ = mainZ, Width = MainDoorW, Height = MainDoorH },
            new Opening { Name = "Inventory", CenterZ = inventoryZ, Width = SingleDoorW, Height = SingleDoorH },
            new Opening { Name = "Coach", CenterZ = coachZ, Width = SingleDoorW, Height = SingleDoorH },
            new Opening { Name = "Emergency", CenterZ = emergencyZ, Width = SingleDoorW, Height = SingleDoorH },
        };

        System.Array.Sort(openings, (a, b) => a.CenterZ.CompareTo(b.CenterZ));

        float cursor = -halfL;
        int solidIdx = 0;
        float t = WallThickness;

        foreach (var op in openings)
        {
            float z0 = op.CenterZ - op.Width * 0.5f;
            float z1 = op.CenterZ + op.Width * 0.5f;

            float solidLen = z0 - cursor;
            if (solidLen > 0.05f)
            {
                CreateBox(parent, $"{prefix}_Solid_{solidIdx++}",
                    new Vector3(x, RoomHeight * 0.5f, cursor + solidLen * 0.5f),
                    new Vector3(t, RoomHeight, solidLen), wallMat);
            }

            // Lintel above door
            float lintelH = RoomHeight - op.Height;
            if (lintelH > 0.05f)
            {
                CreateBox(parent, $"{prefix}_{op.Name}_Lintel",
                    new Vector3(x, op.Height + lintelH * 0.5f, op.CenterZ),
                    new Vector3(t, lintelH, op.Width), wallMat);
            }

            // Door leaf — simple parallelepiped in the opening (visual only)
            float leafX = x - Mathf.Sign(x) * (t * 0.5f + DoorLeafThickness * 0.5f + 0.02f);
            var leaf = CreateBox(parent, $"Door_{op.Name}",
                new Vector3(leafX, op.Height * 0.5f, op.CenterZ),
                new Vector3(DoorLeafThickness, op.Height - 0.04f, op.Width - 0.08f),
                doorMat);
            var col = leaf.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            cursor = z1;
        }

        float endLen = halfL - cursor;
        if (endLen > 0.05f)
        {
            CreateBox(parent, $"{prefix}_Solid_{solidIdx}",
                new Vector3(x, RoomHeight * 0.5f, cursor + endLen * 0.5f),
                new Vector3(t, RoomHeight, endLen), wallMat);
        }
    }

    struct Opening
    {
        public string Name;
        public float CenterZ;
        public float Width;
        public float Height;
    }

    static void BuildSegmentedWallAlongZ(
        Transform parent, string namePrefix, float x, Material mat,
        float openW, float openH, float sillH, float[] centersZ)
    {
        float t = WallThickness;
        float halfL = RoomLength * 0.5f;
        System.Array.Sort(centersZ);
        float cursor = -halfL;
        int part = 0;

        foreach (var cz in centersZ)
        {
            float z0 = cz - openW * 0.5f;
            float z1 = cz + openW * 0.5f;

            float solidLen = z0 - cursor;
            if (solidLen > 0.05f)
            {
                CreateBox(parent, $"{namePrefix}_Solid_{part++}",
                    new Vector3(x, RoomHeight * 0.5f, cursor + solidLen * 0.5f),
                    new Vector3(t, RoomHeight, solidLen), mat);
            }

            // Below sill
            CreateBox(parent, $"{namePrefix}_Sill_{part}",
                new Vector3(x, sillH * 0.5f, cz),
                new Vector3(t, sillH, openW), mat);

            // Above window
            float aboveH = RoomHeight - (sillH + openH);
            if (aboveH > 0.05f)
            {
                CreateBox(parent, $"{namePrefix}_Header_{part}",
                    new Vector3(x, sillH + openH + aboveH * 0.5f, cz),
                    new Vector3(t, aboveH, openW), mat);
            }

            part++;
            cursor = z1;
        }

        float tail = halfL - cursor;
        if (tail > 0.05f)
        {
            CreateBox(parent, $"{namePrefix}_Solid_{part}",
                new Vector3(x, RoomHeight * 0.5f, cursor + tail * 0.5f),
                new Vector3(t, RoomHeight, tail), mat);
        }
    }

    static void SpawnPlayerAndHud()
    {
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        var playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        playerInstance.name = "Player";
        playerInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        GameObject hudInstance = null;
        var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerHudPrefabPath);
        if (hudPrefab != null)
        {
            hudInstance = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
            hudInstance.name = "Canvas";
        }

        var esPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EventSystemPrefabPath);
        if (esPrefab != null)
        {
            var es = (GameObject)PrefabUtility.InstantiatePrefab(esPrefab);
            es.name = "EventSystem";
        }

        WirePlayerUi(playerInstance, hudInstance);
    }

    static void WirePlayerUi(GameObject player, GameObject canvas)
    {
        if (player == null || canvas == null) return;

        var controller = player.GetComponent<PlayerController>();
        if (controller == null) return;

        var so = new SerializedObject(controller);

        var pause = FindChildByName(canvas.transform, "PauseMenuPanel");
        if (pause != null)
            so.FindProperty("pauseMenuPanel").objectReferenceValue = pause.gameObject;

        var promptTf = FindChildByName(canvas.transform, "InteractionPromptText");
        TMPro.TextMeshProUGUI prompt = null;
        if (promptTf != null)
            prompt = promptTf.GetComponent<TMPro.TextMeshProUGUI>();
        if (prompt == null)
            prompt = canvas.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);

        if (prompt != null)
            so.FindProperty("interactionPromptText").objectReferenceValue = prompt;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    static Transform FindChildByName(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name) return child;
        }
        return null;
    }

    static void CreateLighting()
    {
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = Color.white;
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;
        // Sun through west windows
        lightGo.transform.rotation = Quaternion.Euler(35f, 110f, 0f);

        var urpLightType = System.Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalAdditionalLightData, Unity.RenderPipelines.Universal.Runtime");
        if (urpLightType != null && lightGo.GetComponent(urpLightType) == null)
            lightGo.AddComponent(urpLightType);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.55f, 0.6f);
        RenderSettings.ambientEquatorColor = new Color(0.35f, 0.35f, 0.38f);
        RenderSettings.ambientGroundColor = new Color(0.15f, 0.15f, 0.15f);

        var volumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
            "Assets/Settings/SampleSceneProfile.asset");
        if (volumeProfile != null)
        {
            var volGo = new GameObject("Global Volume");
            var volume = volGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = volumeProfile;
        }
    }

    static Material LoadMat(string path) => AssetDatabase.LoadAssetAtPath<Material>(path);

    static GameObject CreateBox(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        go.transform.localScale = size;
        if (mat != null)
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static void CreateEmptyMarker(Transform parent, string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
    }

    static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
        {
            if (s.path == scenePath) return;
        }

        var list = new EditorBuildSettingsScene[scenes.Length + 1];
        for (int i = 0; i < scenes.Length; i++)
            list[i] = scenes[i];
        list[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = list;
    }
}
