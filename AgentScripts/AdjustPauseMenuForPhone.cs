using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class AdjustPauseMenuForPhone
{
    public static string Main()
    {
        const string hudPath = "Assets/Prefabs/PlayerHUD.prefab";
        var root = PrefabUtility.LoadPrefabContents(hudPath);
        try
        {
            var pause = root.transform.Find("PauseMenuPanel");
            if (pause == null)
                return "ERROR: PauseMenuPanel not found";

            var overlay = pause.GetComponent<Image>();
            if (overlay != null)
            {
                var c = overlay.color;
                c.a = 0.45f;
                overlay.color = c;
            }

            var title = pause.Find("Text (TMP)") as RectTransform;
            if (title != null)
            {
                title.anchorMin = new Vector2(0f, 1f);
                title.anchorMax = new Vector2(0f, 1f);
                title.pivot = new Vector2(0f, 1f);
                title.anchoredPosition = new Vector2(48f, -48f);
                title.sizeDelta = new Vector2(420f, 80f);
            }

            var resume = pause.Find("ResumeButton") as RectTransform;
            if (resume != null)
            {
                resume.anchorMin = new Vector2(0f, 0.5f);
                resume.anchorMax = new Vector2(0f, 0.5f);
                resume.pivot = new Vector2(0f, 0.5f);
                resume.anchoredPosition = new Vector2(48f, 40f);
                resume.sizeDelta = new Vector2(280f, 64f);
            }

            var quit = pause.Find("QuitButton") as RectTransform;
            if (quit != null)
            {
                quit.anchorMin = new Vector2(0f, 0.5f);
                quit.anchorMax = new Vector2(0f, 0.5f);
                quit.pivot = new Vector2(0f, 0.5f);
                quit.anchoredPosition = new Vector2(48f, -40f);
                quit.sizeDelta = new Vector2(280f, 64f);
            }

            PrefabUtility.SaveAsPrefabAsset(root, hudPath);
            return "OK: Pause menu shifted left for phone visibility";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
