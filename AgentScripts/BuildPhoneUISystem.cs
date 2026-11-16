using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public static class BuildPhoneUISystem
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string HudPrefabPath = "Assets/Prefabs/PlayerHUD.prefab";
    private const string PhoneUiFolder = "Assets/Prefabs/PhoneUI";
    private const string PhoneCanvasPath = "Assets/Prefabs/PhoneUI/PhoneCanvas.prefab";
    private const string InputActionsPath = "Assets/InputActions.inputactions";

    public static string Main()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PhoneUiFolder);

        int phoneLayer = LayerMask.NameToLayer("PhoneUI");
        if (phoneLayer < 0)
            return "ERROR: PhoneUI layer missing. Check TagManager.";

        var canvasPrefab = BuildOrUpdatePhoneCanvasPrefab(phoneLayer);
        if (canvasPrefab == null)
            return "ERROR: failed to build PhoneCanvas prefab";

        StripHudPauseButtons();
        string playerResult = WirePlayerPrefab(canvasPrefab, phoneLayer);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return playerResult;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent ?? "Assets", name);
    }

    private static GameObject BuildOrUpdatePhoneCanvasPrefab(int phoneLayer)
    {
        var root = new GameObject("PhoneCanvasRoot", typeof(RectTransform));
        try
        {
            var canvasGo = new GameObject("PhoneCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(root.transform, false);
            canvasGo.layer = phoneLayer;

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(PhoneUIController.RtWidth, PhoneUIController.RtHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var canvasRt = canvasGo.GetComponent<RectTransform>();
            canvasRt.anchorMin = Vector2.zero;
            canvasRt.anchorMax = Vector2.one;
            canvasRt.offsetMin = Vector2.zero;
            canvasRt.offsetMax = Vector2.zero;

            // Rounded mask root
            var maskGo = CreateUiObject("RoundedMask", canvasGo.transform, phoneLayer);
            var maskImage = maskGo.AddComponent<Image>();
            maskImage.color = Color.white;
            maskImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
            if (maskImage.sprite == null)
                maskImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            var mask = maskGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            StretchFull(maskGo.GetComponent<RectTransform>());
            // Inset slightly so corners read as rounded against the bezel.
            var maskRt = maskGo.GetComponent<RectTransform>();
            maskRt.offsetMin = new Vector2(18f, 18f);
            maskRt.offsetMax = new Vector2(-18f, -18f);

            var pageHost = CreateUiObject("PageHost", maskGo.transform, phoneLayer);
            StretchFull(pageHost.GetComponent<RectTransform>());

            var pausePage = BuildPausePage(pageHost.transform, phoneLayer);
            var browserPage = BuildBrowserPage(pageHost.transform, phoneLayer);
            browserPage.SetActive(false);

            // Camera for RT (sibling under root so it can be found)
            var camGo = new GameObject("PhoneUICamera");
            camGo.transform.SetParent(root.transform, false);
            camGo.layer = phoneLayer;
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 1f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = 1 << phoneLayer;
            cam.depth = 50f;
            cam.enabled = false;
            canvas.worldCamera = cam;

            PrefabUtility.SaveAsPrefabAsset(root, PhoneCanvasPath);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PhoneCanvasPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject BuildPausePage(Transform parent, int layer)
    {
        var page = CreateUiObject("PhonePage_Pause", parent, layer);
        StretchFull(page.GetComponent<RectTransform>());
        var bg = page.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.08f, 0.1f, 1f);
        var pauseUi = page.AddComponent<PhonePausePageUI>();

        var title = CreateTmp("Title", page.transform, layer, "ПАУЗА", 72);
        var titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -120f);
        titleRt.sizeDelta = new Vector2(900f, 120f);
        title.alignment = TextAlignmentOptions.Center;

        var resume = CreateButton("ResumeButton", page.transform, layer, "Продолжить", new Vector2(0f, 40f));
        var quit = CreateButton("QuitButton", page.transform, layer, "Выход", new Vector2(0f, -80f));

        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            resume.GetComponent<Button>().onClick, pauseUi.ResumeGame);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            quit.GetComponent<Button>().onClick, pauseUi.QuitGame);

        return page;
    }

    private static GameObject BuildBrowserPage(Transform parent, int layer)
    {
        var page = CreateUiObject("PhonePage_BrowserFeed", parent, layer);
        StretchFull(page.GetComponent<RectTransform>());
        var bg = page.AddComponent<Image>();
        bg.color = new Color(0.95f, 0.95f, 0.97f, 1f);

        var header = CreateTmp("Header", page.transform, layer, "Лента", 56);
        header.color = new Color(0.1f, 0.1f, 0.12f, 1f);
        var headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, -40f);
        headerRt.sizeDelta = new Vector2(-80f, 90f);
        header.alignment = TextAlignmentOptions.Left;
        header.margin = new Vector4(40f, 0f, 40f, 0f);

        var scrollGo = CreateUiObject("ScrollView", page.transform, layer);
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(40f, 40f);
        scrollRt.offsetMax = new Vector2(-40f, -150f);

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = CreateUiObject("Viewport", scrollGo.transform, layer);
        StretchFull(viewport.GetComponent<RectTransform>());
        var vpImage = viewport.AddComponent<Image>();
        vpImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var content = CreateUiObject("Content", viewport.transform, layer);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.spacing = 24f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRt;

        string[] articles =
        {
            "Как пройти регистрацию на участке\n\nВозьмите паспорт и подойдите к столу регистрации. Назовите ФИО и дождитесь отметки в списке избирателей.",
            "Тайное голосование\n\nПолучив бюллетень, пройдите в кабину. Отметку ставьте только в квадрате напротив выбранного кандидата.",
            "Урна для бюллетеней\n\nСложите бюллетень и опустите в урну. Не передавайте заполненный бюллетень другим лицам.",
            "Если ошиблись\n\nОбратитесь к членам комиссии до опускания бюллетеня в урну. Испорченный бюллетень могут заменить.",
            "Наблюдатели и прозрачность\n\nНа участке могут присутствовать наблюдатели. Их задача — следить за соблюдением процедуры, а не вмешиваться в ваш выбор."
        };

        foreach (var text in articles)
        {
            var card = CreateUiObject("Article", content.transform, layer);
            var cardImage = card.AddComponent<Image>();
            cardImage.color = Color.white;
            var le = card.AddComponent<LayoutElement>();
            le.minHeight = 280f;
            le.preferredHeight = 320f;

            var body = CreateTmp("Body", card.transform, layer, text, 36);
            body.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.alignment = TextAlignmentOptions.TopLeft;
            StretchFull(body.GetComponent<RectTransform>());
            var bodyRt = body.GetComponent<RectTransform>();
            bodyRt.offsetMin = new Vector2(24f, 24f);
            bodyRt.offsetMax = new Vector2(-24f, -24f);
        }

        return page;
    }

    private static GameObject CreateButton(string name, Transform parent, int layer, string label, Vector2 anchoredPos)
    {
        var go = CreateUiObject(name, parent, layer);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(720f, 110f);

        var image = go.AddComponent<Image>();
        image.color = new Color(0.92f, 0.93f, 0.95f, 1f);
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        var tmp = CreateTmp("Label", go.transform, layer, label, 48);
        tmp.color = new Color(0.1f, 0.1f, 0.12f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        StretchFull(tmp.GetComponent<RectTransform>());
        return go;
    }

    private static TextMeshProUGUI CreateTmp(string name, Transform parent, int layer, string text, float size)
    {
        var go = CreateUiObject(name, parent, layer);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static GameObject CreateUiObject(string name, Transform parent, int layer)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = layer;
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static void StripHudPauseButtons()
    {
        var hud = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            var pause = hud.transform.Find("PauseMenuPanel");
            if (pause == null)
                return;

            // Convert to dim-only panel: remove buttons/title, keep translucent image.
            for (int i = pause.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(pause.GetChild(i).gameObject);

            var pauseUi = pause.GetComponent<PauseMenuUI>();
            if (pauseUi != null)
                Object.DestroyImmediate(pauseUi);

            var image = pause.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0f, 0f, 0f, 0.45f);
                image.raycastTarget = false;
            }

            pause.gameObject.name = "WorldDimPanel";
            pause.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(hud, HudPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hud);
        }
    }

    private static string WirePlayerPrefab(GameObject canvasPrefabAsset, int phoneLayer)
    {
        var playerRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            var camera = playerRoot.GetComponentInChildren<Camera>(true);
            if (camera == null)
                return "ERROR: Main Camera missing on Player";

            // Do not parent an EventSystem under the Player prefab — scenes already have one.
            var nestedEs = playerRoot.GetComponentInChildren<EventSystem>(true);
            if (nestedEs != null)
                Object.DestroyImmediate(nestedEs.gameObject);

            var phoneView = camera.transform.Find("PhoneView");
            if (phoneView == null)
                return "ERROR: PhoneView missing under Main Camera";

            // Screen presenter + collider
            Transform screenTf = FindDeepChild(phoneView, "Screen");
            if (screenTf == null)
                return "ERROR: Screen child missing under PhoneView";

            var presenter = screenTf.GetComponent<PhoneScreenPresenter>();
            if (presenter == null)
                presenter = screenTf.gameObject.AddComponent<PhoneScreenPresenter>();

            var soPresenter = new SerializedObject(presenter);
            soPresenter.FindProperty("screenRenderer").objectReferenceValue = screenTf.GetComponent<Renderer>();
            soPresenter.ApplyModifiedPropertiesWithoutUndo();

            var col = screenTf.GetComponent<MeshCollider>();
            if (col == null)
                col = screenTf.gameObject.AddComponent<MeshCollider>();
            col.convex = false;

            // Remove old PhoneCanvasRoot if re-running
            var existingUi = playerRoot.transform.Find("PhoneUIRoot");
            if (existingUi != null)
                Object.DestroyImmediate(existingUi.gameObject);

            var uiInstance = (GameObject)PrefabUtility.InstantiatePrefab(canvasPrefabAsset, playerRoot.transform);
            uiInstance.name = "PhoneUIRoot";
            SetLayerRecursive(uiInstance, phoneLayer);

            var phoneCanvas = uiInstance.GetComponentInChildren<Canvas>(true);
            var uiCam = uiInstance.GetComponentInChildren<Camera>(true);
            var pausePage = FindDeepChild(uiInstance.transform, "PhonePage_Pause")?.gameObject;
            var browserPage = FindDeepChild(uiInstance.transform, "PhonePage_BrowserFeed")?.gameObject;
            var pageHost = FindDeepChild(uiInstance.transform, "PageHost");

            if (phoneCanvas != null)
                phoneCanvas.worldCamera = uiCam;

            var controller = playerRoot.GetComponent<PhoneUIController>();
            if (controller == null)
                controller = playerRoot.AddComponent<PhoneUIController>();

            var bridge = playerRoot.GetComponent<PhoneScreenInputBridge>();
            if (bridge == null)
                bridge = playerRoot.AddComponent<PhoneScreenInputBridge>();

            // World dim from nested HUD
            GameObject dim = null;
            var dimTf = FindDeepChild(playerRoot.transform, "WorldDimPanel");
            if (dimTf == null)
                dimTf = FindDeepChild(playerRoot.transform, "PauseMenuPanel");
            if (dimTf != null)
            {
                dimTf.gameObject.name = "WorldDimPanel";
                dim = dimTf.gameObject;
                dim.SetActive(false);
                // Strip leftover buttons if nested instance still has them
                for (int i = dimTf.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(dimTf.GetChild(i).gameObject);
                var oldPause = dim.GetComponent<PauseMenuUI>();
                if (oldPause != null)
                    Object.DestroyImmediate(oldPause);
                var dimImage = dim.GetComponent<Image>();
                if (dimImage != null)
                {
                    dimImage.color = new Color(0f, 0f, 0f, 0.45f);
                    dimImage.raycastTarget = false;
                }
            }

            bridge.Configure(camera, uiCam, phoneCanvas, col);

            var soCtrl = new SerializedObject(controller);
            soCtrl.FindProperty("phoneView").objectReferenceValue = phoneView.gameObject;
            soCtrl.FindProperty("screenPresenter").objectReferenceValue = presenter;
            soCtrl.FindProperty("phoneCanvas").objectReferenceValue = phoneCanvas;
            soCtrl.FindProperty("phoneUiCamera").objectReferenceValue = uiCam;
            soCtrl.FindProperty("inputBridge").objectReferenceValue = bridge;
            soCtrl.FindProperty("worldDimPanel").objectReferenceValue = dim;
            soCtrl.FindProperty("pageHost").objectReferenceValue = pageHost;
            soCtrl.FindProperty("pausePage").objectReferenceValue = pausePage;
            soCtrl.FindProperty("browserFeedPage").objectReferenceValue = browserPage;
            soCtrl.FindProperty("phoneUiLayer").intValue = phoneLayer;
            var crosshairTf = FindDeepChild(playerRoot.transform, "Crosshair");
            if (crosshairTf != null)
                soCtrl.FindProperty("crosshair").objectReferenceValue = crosshairTf.gameObject;
            soCtrl.ApplyModifiedPropertiesWithoutUndo();

            var pc = playerRoot.GetComponent<PlayerController>();
            var soPc = new SerializedObject(pc);

            // Clear obsolete pauseMenuPanel if still present
            var pauseMenuProp = soPc.FindProperty("pauseMenuPanel");
            if (pauseMenuProp != null)
                pauseMenuProp.objectReferenceValue = null;

            soPc.FindProperty("worldDimPanel").objectReferenceValue = dim;
            soPc.FindProperty("phoneView").objectReferenceValue = phoneView.gameObject;
            soPc.FindProperty("phoneUiController").objectReferenceValue = controller;

            // Wire OpenBrowser InputActionReference from the input actions asset
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            InputActionReference browserRef = null;
            if (actions != null)
            {
                // Create or find a reference asset
                const string refPath = "Assets/InputActionReferences/OpenBrowser.asset";
                EnsureFolder("Assets/InputActionReferences");
                browserRef = AssetDatabase.LoadAssetAtPath<InputActionReference>(refPath);
                if (browserRef == null)
                {
                    browserRef = InputActionReference.Create(actions.FindAction("UI/OpenBrowser", throwIfNotFound: false));
                    AssetDatabase.CreateAsset(browserRef, refPath);
                }
                else
                {
                    browserRef.Set(actions.FindAction("UI/OpenBrowser", throwIfNotFound: false));
                    EditorUtility.SetDirty(browserRef);
                }
            }

            soPc.FindProperty("openBrowserActionReference").objectReferenceValue = browserRef;
            soPc.ApplyModifiedPropertiesWithoutUndo();

            // Keep HUD as Screen Space Overlay (phone UI is on RT; dim can be overlay)
            var hudCanvas = playerRoot.GetComponentInChildren<Canvas>(true);
            // Prefer PlayerHUD canvas specifically
            foreach (var c in playerRoot.GetComponentsInChildren<Canvas>(true))
            {
                if (c == phoneCanvas)
                    continue;
                c.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            PrefabUtility.SaveAsPrefabAsset(playerRoot, PlayerPrefabPath);
            return "OK: Phone UI wired on Player. P=Pause, B=BrowserFeed. Layer=" + phoneLayer;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(playerRoot);
        }
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root.name == name)
            return root;
        foreach (Transform child in root)
        {
            var found = FindDeepChild(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
