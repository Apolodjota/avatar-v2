using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

namespace AvatarXR.Editor
{
    /// <summary>
    /// Script de editor para crear el menú de configuración en ConsultorioScene.
    /// Ejecutar desde Unity: Tools > AvatarXR > Create Config Menu
    /// </summary>
    public class CreateConfigMenu : EditorWindow
    {
        [MenuItem("Tools/AvatarXR/Create Config Menu in Scene")]
        public static void CreateMenu()
        {
            // Crear Canvas
            GameObject canvasGO = new GameObject("MainMenuCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Configurar Canvas para VR
            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(800, 600);
            canvasRect.localScale = Vector3.one * 0.002f;
            canvasRect.position = new Vector3(0, 1.5f, 2f);
            
            // Agregar CanvasGroup para fade
            CanvasGroup canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            
            // Panel de fondo
            GameObject panel = CreatePanel(canvasGO.transform, "BackgroundPanel", new Color(0.1f, 0.1f, 0.15f, 0.95f));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            // Título
            GameObject title = CreateText(panel.transform, "Title", "CONFIGURACIÓN DE SESIÓN", 48, TextAlignmentOptions.Center);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.85f);
            titleRect.anchorMax = new Vector2(1, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            
            // Slider de Volumen
            GameObject volumeLabel = CreateText(panel.transform, "VolumeLabel", "Volumen:", 24, TextAlignmentOptions.Left);
            SetRectTransform(volumeLabel, 0.05f, 0.7f, 0.3f, 0.78f);
            
            GameObject volumeSlider = CreateSlider(panel.transform, "VolumeSlider");
            SetRectTransform(volumeSlider, 0.35f, 0.7f, 0.75f, 0.78f);
            
            GameObject volumeValue = CreateText(panel.transform, "VolumeValueText", "75%", 24, TextAlignmentOptions.Center);
            SetRectTransform(volumeValue, 0.78f, 0.7f, 0.95f, 0.78f);
            
            // Slider de Estrés Inicial
            GameObject stressLabel = CreateText(panel.transform, "StressLabel", "Estrés Inicial:", 24, TextAlignmentOptions.Left);
            SetRectTransform(stressLabel, 0.05f, 0.55f, 0.3f, 0.63f);
            
            GameObject stressSlider = CreateSlider(panel.transform, "InitialStressSlider");
            SetRectTransform(stressSlider, 0.35f, 0.55f, 0.75f, 0.63f);
            
            GameObject stressValue = CreateText(panel.transform, "InitialStressValueText", "50%", 24, TextAlignmentOptions.Center);
            SetRectTransform(stressValue, 0.78f, 0.55f, 0.95f, 0.63f);
            
            // Toggle Barra de Estrés
            GameObject toggleLabel = CreateText(panel.transform, "ToggleLabel", "Mostrar Barra de Estrés:", 24, TextAlignmentOptions.Left);
            SetRectTransform(toggleLabel, 0.05f, 0.4f, 0.5f, 0.48f);
            
            GameObject stressToggle = CreateToggle(panel.transform, "StressBarToggle");
            SetRectTransform(stressToggle, 0.55f, 0.4f, 0.65f, 0.48f);
            
            // Indicador de Micrófono
            GameObject micLabel = CreateText(panel.transform, "MicLabel", "Micrófono:", 24, TextAlignmentOptions.Left);
            SetRectTransform(micLabel, 0.05f, 0.25f, 0.3f, 0.33f);
            
            GameObject micIndicator = CreateImage(panel.transform, "MicrophoneIndicator", new Color(0.8f, 0.2f, 0.2f, 1f));
            SetRectTransform(micIndicator, 0.35f, 0.26f, 0.38f, 0.32f);
            
            GameObject micStatus = CreateText(panel.transform, "MicrophoneStatusText", "No detectado", 20, TextAlignmentOptions.Left);
            SetRectTransform(micStatus, 0.4f, 0.25f, 0.7f, 0.33f);
            micStatus.GetComponent<TextMeshProUGUI>().color = new Color(0.8f, 0.2f, 0.2f, 1f);
            
            // Botón Iniciar
            GameObject startButton = CreateButton(panel.transform, "StartSessionButton", "INICIAR SESIÓN");
            SetRectTransform(startButton, 0.25f, 0.05f, 0.75f, 0.18f);
            startButton.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.3f, 1f);
            
            // Agregar MainMenuController
            canvasGO.AddComponent<AvatarXR.UI.MainMenuController>();
            
            // Seleccionar el canvas creado
            Selection.activeGameObject = canvasGO;
            
            Debug.Log("✅ Menú de configuración creado. Conecta las referencias en MainMenuController.");
            EditorUtility.DisplayDialog("Menú Creado", 
                "El menú de configuración ha sido creado.\n\n" +
                "Pasos siguientes:\n" +
                "1. Conecta las referencias en MainMenuController\n" +
                "2. Conecta mainMenuCanvas en ConsultorioController (SessionManager)\n" +
                "3. Guarda la escena", "OK");
        }
        
        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image img = panel.AddComponent<Image>();
            img.color = color;
            return panel;
        }
        
        private static GameObject CreateText(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return textGO;
        }
        
        private static GameObject CreateSlider(Transform parent, string name)
        {
            GameObject sliderGO = new GameObject(name);
            sliderGO.transform.SetParent(parent, false);
            
            // Background
            GameObject bg = CreatePanel(sliderGO.transform, "Background", new Color(0.3f, 0.3f, 0.3f, 1f));
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGO.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);
            
            GameObject fill = CreatePanel(fillArea.transform, "Fill", new Color(0.3f, 0.7f, 0.4f, 1f));
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            // Handle
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderGO.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);
            
            GameObject handle = CreatePanel(handleArea.transform, "Handle", Color.white);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);
            
            // Slider component
            Slider slider = sliderGO.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0.75f;
            
            return sliderGO;
        }
        
        private static GameObject CreateToggle(Transform parent, string name)
        {
            GameObject toggleGO = new GameObject(name);
            toggleGO.transform.SetParent(parent, false);
            
            // Background
            GameObject bg = CreatePanel(toggleGO.transform, "Background", new Color(0.3f, 0.3f, 0.3f, 1f));
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            // Checkmark
            GameObject checkmark = CreatePanel(bg.transform, "Checkmark", new Color(0.3f, 0.8f, 0.4f, 1f));
            RectTransform checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;
            
            Toggle toggle = toggleGO.AddComponent<Toggle>();
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic = checkmark.GetComponent<Image>();
            toggle.isOn = true;
            
            return toggleGO;
        }
        
        private static GameObject CreateButton(Transform parent, string name, string text)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);
            
            Image img = buttonGO.AddComponent<Image>();
            img.color = new Color(0.2f, 0.5f, 0.8f, 1f);
            
            Button button = buttonGO.AddComponent<Button>();
            button.targetGraphic = img;
            
            GameObject textGO = CreateText(buttonGO.transform, "Text", text, 28, TextAlignmentOptions.Center);
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            return buttonGO;
        }
        
        private static GameObject CreateImage(Transform parent, string name, Color color)
        {
            GameObject imgGO = new GameObject(name);
            imgGO.transform.SetParent(parent, false);
            Image img = imgGO.AddComponent<Image>();
            img.color = color;
            return imgGO;
        }
        
        private static void SetRectTransform(GameObject go, float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY)
        {
            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rect.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
