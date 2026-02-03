#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace AvatarXR.Editor
{
    /// <summary>
    /// Herramienta de editor para crear rápidamente la estructura de las escenas.
    /// Accesible desde el menú Tools > Avatar XR
    /// </summary>
    public class SceneSetupTools : EditorWindow
    {
        [MenuItem("Tools/Avatar XR/Crear Estructura MainMenu")]
        public static void CreateMainMenuStructure()
        {
            if (!EditorUtility.DisplayDialog("Crear MainMenu",
                "Esto creará la estructura básica del menú principal.\n\n¿Continuar?",
                "Crear", "Cancelar"))
            {
                return;
            }

            // Crear GameManager
            CreateGameManager();

            // Crear Canvas
            GameObject canvas = CreateVRCanvas("MainMenuCanvas", new Vector3(0, 1.5f, 2f));
            
            // Añadir CanvasGroup para fade
            canvas.AddComponent<CanvasGroup>();

            // Crear panel de fondo
            GameObject background = CreateUIPanel(canvas.transform, "Panel_Background", 
                new Vector2(500, 350), new Color(0.1f, 0.1f, 0.15f, 0.9f));

            // Crear controles
            CreateVolumeSlider(background.transform);
            CreateStressBarToggle(background.transform);
            CreateMicrophoneIndicator(background.transform);
            CreateStartButton(background.transform);

            // Añadir controlador
            var controller = canvas.AddComponent<UI.MainMenuController>();
            
            Debug.Log("[SceneSetup] Estructura de MainMenu creada. Asigna las referencias en el Inspector.");
            Selection.activeGameObject = canvas;
        }

        [MenuItem("Tools/Avatar XR/Crear Estructura Consultorio")]
        public static void CreateConsultorioStructure()
        {
            if (!EditorUtility.DisplayDialog("Crear Consultorio",
                "Esto creará la estructura básica del consultorio.\n\n¿Continuar?",
                "Crear", "Cancelar"))
            {
                return;
            }

            // Crear spawn point del usuario
            GameObject spawnPoint = new GameObject("UserSpawnPoint");
            spawnPoint.transform.position = new Vector3(0, 0, 2);
            spawnPoint.transform.rotation = Quaternion.Euler(0, 180, 0);

            // Crear placeholder del avatar
            GameObject avatar = new GameObject("AvatarPaciente");
            avatar.transform.position = new Vector3(0, 0, 0.5f);
            avatar.transform.rotation = Quaternion.Euler(0, 180, 0);
            avatar.AddComponent<Avatar.AvatarPlaceholder>();

            // Crear barra de estrés
            GameObject stressBar = CreateStressBarDiegetic();

            // Crear indicador de micrófono
            GameObject micIndicator = CreateMicrophoneIndicatorHUD();

            // Crear menú de pausa
            GameObject pauseMenu = CreatePauseMenu();

            // Crear SessionManager
            GameObject sessionManager = new GameObject("SessionManager");
            var controller = sessionManager.AddComponent<Managers.ConsultorioController>();

            Debug.Log("[SceneSetup] Estructura de Consultorio creada. Asigna las referencias en el Inspector.");
            Selection.activeGameObject = sessionManager;
        }

        [MenuItem("Tools/Avatar XR/Añadir OVRCameraRig")]
        public static void AddOVRCameraRig()
        {
            // Buscar el prefab
            string[] guids = AssetDatabase.FindAssets("OVRCameraRig t:Prefab");
            
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Error",
                    "No se encontró el prefab OVRCameraRig.\n\nAsegúrate de tener Oculus Integration instalado.",
                    "OK");
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                // Eliminar cámara existente
                Camera existingCamera = FindObjectOfType<Camera>();
                if (existingCamera != null && existingCamera.gameObject.name == "Main Camera")
                {
                    DestroyImmediate(existingCamera.gameObject);
                }

                GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                rig.transform.position = Vector3.zero;
                rig.transform.rotation = Quaternion.identity;
                
                Selection.activeGameObject = rig;
                Debug.Log("[SceneSetup] OVRCameraRig añadido a la escena.");
            }
        }

        private static void CreateGameManager()
        {
            // Verificar si ya existe
            var existing = FindObjectOfType<Managers.GameManager>();
            if (existing != null)
            {
                Debug.Log("[SceneSetup] GameManager ya existe en la escena.");
                return;
            }

            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<Managers.GameManager>();
        }

        private static GameObject CreateVRCanvas(string name, Vector3 position)
        {
            GameObject canvasObj = new GameObject(name);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rt = canvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 350);
            rt.position = position;
            rt.localScale = new Vector3(0.002f, 0.002f, 0.002f);

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            return canvasObj;
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;

            Image img = panel.AddComponent<Image>();
            img.color = color;

            return panel;
        }

        private static void CreateVolumeSlider(Transform parent)
        {
            // Container
            GameObject container = new GameObject("VolumeControl");
            container.transform.SetParent(parent, false);
            RectTransform containerRT = container.AddComponent<RectTransform>();
            containerRT.anchoredPosition = new Vector2(0, 80);
            containerRT.sizeDelta = new Vector2(300, 50);

            // Label
            GameObject label = new GameObject("Text_VolumeLabel");
            label.transform.SetParent(container.transform, false);
            TextMeshProUGUI labelTMP = label.AddComponent<TextMeshProUGUI>();
            labelTMP.text = "Volumen";
            labelTMP.fontSize = 18;
            labelTMP.alignment = TextAlignmentOptions.Left;
            RectTransform labelRT = label.GetComponent<RectTransform>();
            labelRT.anchoredPosition = new Vector2(-100, 0);
            labelRT.sizeDelta = new Vector2(100, 30);

            // Slider (simplificado - Unity creará los hijos automáticamente si usas el menú)
            GameObject sliderObj = new GameObject("Slider_Volume");
            sliderObj.transform.SetParent(container.transform, false);
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0.75f;
            RectTransform sliderRT = sliderObj.GetComponent<RectTransform>();
            sliderRT.anchoredPosition = new Vector2(50, 0);
            sliderRT.sizeDelta = new Vector2(150, 20);

            // Value text
            GameObject valueText = new GameObject("Text_VolumeValue");
            valueText.transform.SetParent(container.transform, false);
            TextMeshProUGUI valueTMP = valueText.AddComponent<TextMeshProUGUI>();
            valueTMP.text = "75%";
            valueTMP.fontSize = 16;
            valueTMP.alignment = TextAlignmentOptions.Right;
            RectTransform valueRT = valueText.GetComponent<RectTransform>();
            valueRT.anchoredPosition = new Vector2(130, 0);
            valueRT.sizeDelta = new Vector2(50, 30);
        }

        private static void CreateStressBarToggle(Transform parent)
        {
            GameObject container = new GameObject("StressBarControl");
            container.transform.SetParent(parent, false);
            RectTransform containerRT = container.AddComponent<RectTransform>();
            containerRT.anchoredPosition = new Vector2(0, 20);
            containerRT.sizeDelta = new Vector2(300, 50);

            // Label
            GameObject label = new GameObject("Text_StressBarLabel");
            label.transform.SetParent(container.transform, false);
            TextMeshProUGUI labelTMP = label.AddComponent<TextMeshProUGUI>();
            labelTMP.text = "Barra de Estrés Visible";
            labelTMP.fontSize = 16;
            labelTMP.alignment = TextAlignmentOptions.Left;
            RectTransform labelRT = label.GetComponent<RectTransform>();
            labelRT.anchoredPosition = new Vector2(-50, 0);
            labelRT.sizeDelta = new Vector2(200, 30);

            // Toggle
            GameObject toggleObj = new GameObject("Toggle_StressBar");
            toggleObj.transform.SetParent(container.transform, false);
            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = true;
            RectTransform toggleRT = toggleObj.GetComponent<RectTransform>();
            toggleRT.anchoredPosition = new Vector2(120, 0);
            toggleRT.sizeDelta = new Vector2(30, 30);
        }

        private static void CreateMicrophoneIndicator(Transform parent)
        {
            GameObject container = new GameObject("Panel_MicStatus");
            container.transform.SetParent(parent, false);
            RectTransform containerRT = container.AddComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0, 0);
            containerRT.anchorMax = new Vector2(0, 0);
            containerRT.pivot = new Vector2(0, 0);
            containerRT.anchoredPosition = new Vector2(20, 20);
            containerRT.sizeDelta = new Vector2(200, 40);

            // Indicator circle
            GameObject indicator = new GameObject("Image_MicIndicator");
            indicator.transform.SetParent(container.transform, false);
            Image img = indicator.AddComponent<Image>();
            img.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            RectTransform indicatorRT = indicator.GetComponent<RectTransform>();
            indicatorRT.anchoredPosition = new Vector2(15, 0);
            indicatorRT.sizeDelta = new Vector2(25, 25);

            // Status text
            GameObject statusText = new GameObject("Text_MicStatus");
            statusText.transform.SetParent(container.transform, false);
            TextMeshProUGUI statusTMP = statusText.AddComponent<TextMeshProUGUI>();
            statusTMP.text = "Micrófono: Verificando...";
            statusTMP.fontSize = 14;
            statusTMP.alignment = TextAlignmentOptions.Left;
            statusTMP.color = Color.gray;
            RectTransform statusRT = statusText.GetComponent<RectTransform>();
            statusRT.anchoredPosition = new Vector2(110, 0);
            statusRT.sizeDelta = new Vector2(150, 30);
        }

        private static void CreateStartButton(Transform parent)
        {
            GameObject buttonObj = new GameObject("Button_StartSession");
            buttonObj.transform.SetParent(parent, false);
            
            Image img = buttonObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.6f, 0.9f, 1f);
            
            Button button = buttonObj.AddComponent<Button>();
            button.interactable = false; // Deshabilitado hasta verificar micrófono

            RectTransform buttonRT = buttonObj.GetComponent<RectTransform>();
            buttonRT.anchorMin = new Vector2(0.5f, 0);
            buttonRT.anchorMax = new Vector2(0.5f, 0);
            buttonRT.pivot = new Vector2(0.5f, 0);
            buttonRT.anchoredPosition = new Vector2(0, 30);
            buttonRT.sizeDelta = new Vector2(200, 50);

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = "INICIAR SESIÓN";
            buttonText.fontSize = 20;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
        }

        private static GameObject CreateStressBarDiegetic()
        {
            GameObject canvasObj = CreateVRCanvas("Canvas_StressBar", new Vector3(-2, 1.5f, 0));
            canvasObj.transform.rotation = Quaternion.Euler(0, 90, 0);

            RectTransform rt = canvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 250);

            // Background
            GameObject bg = CreateUIPanel(canvasObj.transform, "Panel_Background", 
                new Vector2(80, 250), new Color(0.15f, 0.15f, 0.2f, 0.9f));

            // Slider placeholder
            GameObject sliderObj = new GameObject("Slider_StressBar");
            sliderObj.transform.SetParent(bg.transform, false);
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.direction = Slider.Direction.BottomToTop;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0.7f;
            RectTransform sliderRT = sliderObj.GetComponent<RectTransform>();
            sliderRT.sizeDelta = new Vector2(40, 200);

            // Level text
            GameObject levelText = new GameObject("Text_Level");
            levelText.transform.SetParent(bg.transform, false);
            TextMeshProUGUI levelTMP = levelText.AddComponent<TextMeshProUGUI>();
            levelTMP.text = "7/10";
            levelTMP.fontSize = 18;
            levelTMP.alignment = TextAlignmentOptions.Center;
            RectTransform levelRT = levelText.GetComponent<RectTransform>();
            levelRT.anchoredPosition = new Vector2(0, -115);
            levelRT.sizeDelta = new Vector2(60, 25);

            canvasObj.AddComponent<UI.StressBarDiegetic>();

            return canvasObj;
        }

        private static GameObject CreateMicrophoneIndicatorHUD()
        {
            // Crear un pequeño canvas adjunto al tracking space
            GameObject canvasObj = new GameObject("Canvas_MicIndicator");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rt = canvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 30);
            rt.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            // Indicator
            GameObject ring = new GameObject("Image_MicRing");
            ring.transform.SetParent(canvasObj.transform, false);
            Image img = ring.AddComponent<Image>();
            img.color = Color.gray;
            RectTransform ringRT = ring.GetComponent<RectTransform>();
            ringRT.sizeDelta = new Vector2(25, 25);
            ringRT.anchoredPosition = new Vector2(-30, 0);

            // Text
            GameObject text = new GameObject("Text_MicState");
            text.transform.SetParent(canvasObj.transform, false);
            TextMeshProUGUI tmp = text.AddComponent<TextMeshProUGUI>();
            tmp.text = "CERRADO";
            tmp.fontSize = 12;
            tmp.alignment = TextAlignmentOptions.Left;
            RectTransform textRT = text.GetComponent<RectTransform>();
            textRT.sizeDelta = new Vector2(70, 25);
            textRT.anchoredPosition = new Vector2(20, 0);

            return canvasObj;
        }

        private static GameObject CreatePauseMenu()
        {
            GameObject canvasObj = CreateVRCanvas("Canvas_PauseMenu", new Vector3(0, 1.5f, 1));
            canvasObj.SetActive(false); // Inicialmente oculto

            // Overlay
            GameObject overlay = CreateUIPanel(canvasObj.transform, "Panel_Overlay",
                new Vector2(1000, 1000), new Color(0, 0, 0, 0.5f));

            // Menu panel
            GameObject menu = CreateUIPanel(canvasObj.transform, "Panel_Menu",
                new Vector2(300, 250), new Color(0.15f, 0.15f, 0.2f, 0.95f));

            // Title
            GameObject title = new GameObject("Text_Title");
            title.transform.SetParent(menu.transform, false);
            TextMeshProUGUI titleTMP = title.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "Sesión Pausada";
            titleTMP.fontSize = 24;
            titleTMP.alignment = TextAlignmentOptions.Center;
            RectTransform titleRT = title.GetComponent<RectTransform>();
            titleRT.anchoredPosition = new Vector2(0, 90);
            titleRT.sizeDelta = new Vector2(280, 40);

            // Buttons
            CreateMenuButton(menu.transform, "Button_Resume", "Reanudar", new Vector2(0, 30));
            CreateMenuButton(menu.transform, "Button_Restart", "Reiniciar", new Vector2(0, -30));
            CreateMenuButton(menu.transform, "Button_Exit", "Salir", new Vector2(0, -90));

            return canvasObj;
        }

        private static void CreateMenuButton(Transform parent, string name, string text, Vector2 position)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            Image img = buttonObj.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.3f, 1f);

            Button button = buttonObj.AddComponent<Button>();

            RectTransform buttonRT = buttonObj.GetComponent<RectTransform>();
            buttonRT.anchoredPosition = position;
            buttonRT.sizeDelta = new Vector2(200, 45);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 18;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
        }
    }
}
#endif
