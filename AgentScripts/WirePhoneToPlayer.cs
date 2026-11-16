using UnityEditor;
using UnityEngine;

public static class WirePhoneToPlayer
{
    public static string Main()
    {
        const string playerPath = "Assets/Prefabs/Player.prefab";
        const string phonePath = "Assets/Art/Shared/Prefabs/Phone.prefab";

        var playerRoot = PrefabUtility.LoadPrefabContents(playerPath);
        try
        {
            var camera = playerRoot.GetComponentInChildren<Camera>(true);
            if (camera == null)
                return "ERROR: Main Camera not found on Player prefab";

            var existing = camera.transform.Find("PhoneView");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var phoneAsset = AssetDatabase.LoadAssetAtPath<GameObject>(phonePath);
            if (phoneAsset == null)
                return "ERROR: Phone prefab missing at " + phonePath;

            var phoneInstance = (GameObject)PrefabUtility.InstantiatePrefab(phoneAsset, camera.transform);
            phoneInstance.name = "PhoneView";
            phoneInstance.SetActive(false);

            // Screen faces +Y on the model; -90 X faces the camera. Centered in view.
            phoneInstance.transform.localPosition = new Vector3(0f, 0f, 0.45f);
            phoneInstance.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            phoneInstance.transform.localScale = Vector3.one * 2.4f;

            var controller = playerRoot.GetComponent<PlayerController>();
            if (controller == null)
                return "ERROR: PlayerController missing";

            var so = new SerializedObject(controller);
            so.FindProperty("phoneView").objectReferenceValue = phoneInstance;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(playerRoot, playerPath);
            return "OK: PhoneView parented to Main Camera and wired to PlayerController.phoneView";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(playerRoot);
        }
    }
}
