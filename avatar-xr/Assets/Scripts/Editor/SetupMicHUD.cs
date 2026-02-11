using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class SetupMicHUD
{
    public static string Execute()
    {
        // 1. Crear Canvas Screen Space Overlay
        GameObject hudCanvas = new GameObject("Canvas_MicHUD");
        Canvas canvas = hudCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Asegurar que siempre esté encima

        CanvasScaler scaler = hudCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        hudCanvas.AddComponent<GraphicRaycaster>();

        // 2. Crear contenedor del HUD anclado a la esquina inferior izquierda
        GameObject micContainer = new GameObject("MicHUD_Container");
        micContainer.transform.SetParent(hudCanvas.transform, false);
        RectTransform containerRect = micContainer.AddComponent<RectTransform>();
        // Anclar a la esquina inferior izquierda
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(0, 0);
        containerRect.pivot = new Vector2(0, 0);
        containerRect.anchoredPosition = new Vector2(30, 30); // Margen de 30px
        containerRect.sizeDelta = new Vector2(80, 80);

        // Agregar el script MicHUDController
        var micHUDComponent = micContainer.AddComponent<AvatarXR.UI.MicHUDController>();

        // 3. Crear fondo circular (ring glow)
        GameObject ringGlow = new GameObject("RingGlow");
        ringGlow.transform.SetParent(micContainer.transform, false);
        RectTransform ringRect = ringGlow.AddComponent<RectTransform>();
        ringRect.anchorMin = new Vector2(0.5f, 0.5f);
        ringRect.anchorMax = new Vector2(0.5f, 0.5f);
        ringRect.pivot = new Vector2(0.5f, 0.5f);
        ringRect.anchoredPosition = Vector2.zero;
        ringRect.sizeDelta = new Vector2(80, 80);
        
        Image ringImage = ringGlow.AddComponent<Image>();
        ringImage.color = new Color(0.9f, 0.2f, 0.2f, 0.5f); // Rojo semitransparente por defecto
        ringImage.raycastTarget = false;
        // Usar sprite circular
        ringImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        ringImage.type = Image.Type.Simple;

        // 4. Crear fondo oscuro circular
        GameObject bgCircle = new GameObject("BgCircle");
        bgCircle.transform.SetParent(micContainer.transform, false);
        RectTransform bgRect = bgCircle.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(64, 64);
        
        Image bgImage = bgCircle.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.12f, 0.85f); // Fondo oscuro
        bgImage.raycastTarget = false;
        bgImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        bgImage.type = Image.Type.Simple;

        // 5. Crear icono del micrófono
        GameObject micIconObj = new GameObject("MicIcon");
        micIconObj.transform.SetParent(micContainer.transform, false);
        RectTransform iconRect = micIconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(40, 40);
        
        Image micImage = micIconObj.AddComponent<Image>();
        micImage.raycastTarget = false;
        micImage.preserveAspect = true;

        // Intentar cargar el sprite del micrófono
        string[] micIconPaths = new string[] {
            "Assets/_Heathen Engineering/Assets/UX/Icons/Flat Icons [Free]/Free Flat Mic Icon.png",
            "Assets/_Heathen Engineering/Assets/UX/Icons/Flat Icons [Free]/Free Flat Mic No Crossless Icon.png"
        };

        Sprite micSprite = null;
        foreach (string path in micIconPaths)
        {
            micSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (micSprite != null)
            {
                Debug.Log($"[MicHUD Setup] Sprite cargado desde: {path}");
                break;
            }
        }

        if (micSprite != null)
        {
            micImage.sprite = micSprite;
            micImage.color = new Color(0.9f, 0.2f, 0.2f, 1f); // Rojo inicial
        }
        else
        {
            // Fallback: usar un color sólido si no se encuentra el sprite
            micImage.color = new Color(0.9f, 0.2f, 0.2f, 1f);
            Debug.LogWarning("[MicHUD Setup] No se encontró el sprite del micrófono. Usando color sólido.");
        }

        // 6. Asignar referencias al componente MicHUDController via SerializedObject
        SerializedObject so = new SerializedObject(micHUDComponent);
        SerializedProperty micIconProp = so.FindProperty("micIcon");
        SerializedProperty ringGlowProp = so.FindProperty("ringGlow");
        
        if (micIconProp != null)
        {
            micIconProp.objectReferenceValue = micImage;
        }
        if (ringGlowProp != null)
        {
            ringGlowProp.objectReferenceValue = ringImage;
        }
        so.ApplyModifiedProperties();

        // 7. Conectar al ConsultorioController
        var consultorio = Object.FindObjectOfType<AvatarXR.Managers.ConsultorioController>();
        if (consultorio != null)
        {
            SerializedObject consultorioSO = new SerializedObject(consultorio);
            SerializedProperty micHUDProp = consultorioSO.FindProperty("micHUD");
            if (micHUDProp != null)
            {
                micHUDProp.objectReferenceValue = micHUDComponent;
                consultorioSO.ApplyModifiedProperties();
                Debug.Log("[MicHUD Setup] Conectado al ConsultorioController.");
            }
            else
            {
                Debug.LogWarning("[MicHUD Setup] No se encontró la propiedad 'micHUD' en ConsultorioController.");
            }
        }
        else
        {
            Debug.LogWarning("[MicHUD Setup] No se encontró ConsultorioController en la escena.");
        }

        // 8. Marcar la escena como modificada
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        Selection.activeGameObject = micContainer;

        return "HUD de micrófono creado exitosamente en Canvas_MicHUD. " +
               "Anclado a la esquina inferior izquierda. " +
               $"Sprite del mic: {(micSprite != null ? "cargado" : "fallback")}. " +
               "Conectado al ConsultorioController.";
    }
}
