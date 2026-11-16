using UnityEditor;
using UnityEngine;

public static class FixPhoneViewPose
{
    public static string Main()
    {
        const string playerPath = "Assets/Prefabs/Player.prefab";
        var playerRoot = PrefabUtility.LoadPrefabContents(playerPath);
        try
        {
            var camera = playerRoot.GetComponentInChildren<Camera>(true);
            if (camera == null || camera.name != "Main Camera")
            {
                // Prefer the gameplay camera named Main Camera
                foreach (var cam in playerRoot.GetComponentsInChildren<Camera>(true))
                {
                    if (cam.name == "Main Camera")
                    {
                        camera = cam;
                        break;
                    }
                }
            }

            if (camera == null)
                return "ERROR: Main Camera missing";

            var phoneView = camera.transform.Find("PhoneView");
            if (phoneView == null)
                return "ERROR: PhoneView missing";

            // Screen mesh faces +Y; -90 X points it at the camera (-local Z). Centered in view.
            phoneView.localPosition = new Vector3(0f, 0f, 0.45f);
            phoneView.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            phoneView.localScale = Vector3.one * 2.4f;
            phoneView.gameObject.SetActive(false);

            // Keep PlayerController.phoneView reference
            var pc = playerRoot.GetComponent<PlayerController>();
            var so = new SerializedObject(pc);
            so.FindProperty("phoneView").objectReferenceValue = phoneView.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();

            var ui = playerRoot.GetComponent<PhoneUIController>();
            if (ui != null)
            {
                var soUi = new SerializedObject(ui);
                soUi.FindProperty("phoneView").objectReferenceValue = phoneView.gameObject;
                soUi.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(playerRoot, playerPath);
            return "OK: PhoneView centered, facing camera, pos=(0,0,0.45) rot=(-90,0,0) scale=2.4";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(playerRoot);
        }
    }
}
