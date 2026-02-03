using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace AvatarXR.UI
{
    /// <summary>
    /// Pantalla de resultados que se muestra al finalizar una sesión de entrenamiento.
    /// Muestra estadísticas del desempeño y opciones para continuar.
    /// </summary>
    public class ResultsScreen : MonoBehaviour
    {
        [Header("Panel Principal")]
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Título y Mensaje")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Image resultIcon;
        [SerializeField] private Sprite successIcon;
        [SerializeField] private Sprite failureIcon;
        
        [Header("Estadísticas")]
        [SerializeField] private TextMeshProUGUI stressInitialText;
        [SerializeField] private TextMeshProUGUI stressFinalText;
        [SerializeField] private TextMeshProUGUI turnsText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI emotionsDetectedText;
        
        [Header("Barra de Progreso de Estrés")]
        [SerializeField] private Slider stressProgressSlider;
        [SerializeField] private Image stressProgressFill;
        [SerializeField] private Gradient stressGradient;
        
        [Header("Calificación")]
        [SerializeField] private TextMeshProUGUI gradeText;
        [SerializeField] private TextMeshProUGUI feedbackText;
        
        [Header("Botones")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;
        
        [Header("Colores")]
        [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color failureColor = new Color(0.8f, 0.2f, 0.2f);
        
        [Header("Animación")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float statsAnimationDelay = 0.3f;
        
        // Datos de la sesión
        private SessionResults currentResults;
        
        [System.Serializable]
        public class SessionResults
        {
            public bool success;
            public string message;
            public int initialStress;
            public int finalStress;
            public int totalTurns;
            public float sessionTime;
            public string[] emotionsDetected;
        }
        
        private void Awake()
        {
            // Ocultar al inicio
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(false);
            }
            
            SetupButtons();
        }
        
        private void SetupButtons()
        {
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
            }
            
            if (menuButton != null)
            {
                menuButton.onClick.AddListener(OnMenuClicked);
            }
        }
        
        /// <summary>
        /// Muestra la pantalla de resultados con los datos de la sesión.
        /// </summary>
        public void ShowResults(SessionResults results)
        {
            currentResults = results;
            
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(true);
            }
            
            StartCoroutine(AnimateResultsIn());
        }
        
        /// <summary>
        /// Versión simplificada para llamar desde ConsultorioController.
        /// </summary>
        public void ShowResults(bool success, string message, int initialStress, int finalStress, 
                                int turns, float time, string[] emotions = null)
        {
            ShowResults(new SessionResults
            {
                success = success,
                message = message,
                initialStress = initialStress,
                finalStress = finalStress,
                totalTurns = turns,
                sessionTime = time,
                emotionsDetected = emotions ?? new string[0]
            });
        }
        
        private IEnumerator AnimateResultsIn()
        {
            // Fade in del canvas
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                float elapsed = 0f;
                
                while (elapsed < fadeInDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                    yield return null;
                }
                
                canvasGroup.alpha = 1f;
            }
            
            // Mostrar título
            UpdateTitle();
            yield return new WaitForSecondsRealtime(statsAnimationDelay);
            
            // Animar estadísticas
            yield return StartCoroutine(AnimateStats());
            
            // Mostrar calificación
            yield return new WaitForSecondsRealtime(statsAnimationDelay);
            UpdateGrade();
        }
        
        private void UpdateTitle()
        {
            if (titleText != null)
            {
                titleText.text = currentResults.success ? "¡SESIÓN EXITOSA!" : "SESIÓN FINALIZADA";
                titleText.color = currentResults.success ? successColor : failureColor;
            }
            
            if (messageText != null)
            {
                messageText.text = currentResults.message;
            }
            
            if (resultIcon != null)
            {
                resultIcon.sprite = currentResults.success ? successIcon : failureIcon;
                resultIcon.color = currentResults.success ? successColor : failureColor;
            }
        }
        
        private IEnumerator AnimateStats()
        {
            // Estrés inicial
            if (stressInitialText != null)
            {
                stressInitialText.text = $"Estrés Inicial: {currentResults.initialStress}/10";
            }
            yield return new WaitForSecondsRealtime(0.1f);
            
            // Estrés final con animación
            if (stressFinalText != null)
            {
                stressFinalText.text = $"Estrés Final: {currentResults.finalStress}/10";
                
                // Color basado en si bajó o subió
                if (currentResults.finalStress < currentResults.initialStress)
                {
                    stressFinalText.color = successColor;
                }
                else if (currentResults.finalStress > currentResults.initialStress)
                {
                    stressFinalText.color = failureColor;
                }
            }
            yield return new WaitForSecondsRealtime(0.1f);
            
            // Barra de progreso de estrés
            if (stressProgressSlider != null)
            {
                float targetValue = currentResults.finalStress / 10f;
                float elapsed = 0f;
                float duration = 0.5f;
                
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float value = Mathf.Lerp(currentResults.initialStress / 10f, targetValue, elapsed / duration);
                    stressProgressSlider.value = value;
                    
                    if (stressProgressFill != null && stressGradient != null)
                    {
                        stressProgressFill.color = stressGradient.Evaluate(value);
                    }
                    
                    yield return null;
                }
            }
            yield return new WaitForSecondsRealtime(0.1f);
            
            // Turnos
            if (turnsText != null)
            {
                turnsText.text = $"Interacciones: {currentResults.totalTurns}";
            }
            yield return new WaitForSecondsRealtime(0.1f);
            
            // Tiempo
            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(currentResults.sessionTime / 60f);
                int seconds = Mathf.FloorToInt(currentResults.sessionTime % 60f);
                timeText.text = $"Duración: {minutes}:{seconds:00}";
            }
            yield return new WaitForSecondsRealtime(0.1f);
            
            // Emociones detectadas
            if (emotionsDetectedText != null && currentResults.emotionsDetected != null)
            {
                if (currentResults.emotionsDetected.Length > 0)
                {
                    emotionsDetectedText.text = "Emociones: " + string.Join(", ", currentResults.emotionsDetected);
                }
                else
                {
                    emotionsDetectedText.text = "Emociones: -";
                }
            }
        }
        
        private void UpdateGrade()
        {
            if (gradeText == null) return;
            
            // Calcular calificación basada en:
            // - Reducción de estrés (40%)
            // - Número de turnos eficientes (30%)
            // - Tiempo razonable (30%)
            
            float stressReduction = (currentResults.initialStress - currentResults.finalStress) / 10f;
            float turnsScore = Mathf.Clamp01(1f - (currentResults.totalTurns - 5) / 15f); // Óptimo: 5-10 turnos
            float timeScore = Mathf.Clamp01(1f - (currentResults.sessionTime - 120f) / 600f); // Óptimo: 2-12 min
            
            float totalScore = (stressReduction * 0.4f + turnsScore * 0.3f + timeScore * 0.3f);
            
            // Convertir a letra
            string grade;
            string feedback;
            
            if (totalScore >= 0.9f)
            {
                grade = "A+";
                feedback = "¡Excelente manejo de la crisis! Comunicación empática y efectiva.";
            }
            else if (totalScore >= 0.8f)
            {
                grade = "A";
                feedback = "Muy buen desempeño. El paciente se sintió escuchado.";
            }
            else if (totalScore >= 0.7f)
            {
                grade = "B";
                feedback = "Buen trabajo. Algunas técnicas de desescalamiento fueron efectivas.";
            }
            else if (totalScore >= 0.6f)
            {
                grade = "C";
                feedback = "Aceptable. Considera practicar más técnicas de escucha activa.";
            }
            else if (totalScore >= 0.5f)
            {
                grade = "D";
                feedback = "Necesita mejora. Enfócate en validar las emociones del paciente.";
            }
            else
            {
                grade = "F";
                feedback = "La comunicación no fue efectiva. Revisa las técnicas de desescalamiento.";
            }
            
            gradeText.text = grade;
            gradeText.color = totalScore >= 0.6f ? successColor : failureColor;
            
            if (feedbackText != null)
            {
                feedbackText.text = feedback;
            }
            
            Debug.Log($"[ResultsScreen] Calificación: {grade} (Score: {totalScore:F2})");
        }
        
        private void OnRetryClicked()
        {
            Debug.Log("[ResultsScreen] Reiniciando sesión...");
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
        
        private void OnMenuClicked()
        {
            Debug.Log("[ResultsScreen] Volviendo al menú...");
            Time.timeScale = 1f;
            
            if (AvatarXR.Managers.GameManager.Instance != null)
            {
                AvatarXR.Managers.GameManager.Instance.ReturnToMainMenu();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
        }
        
        /// <summary>
        /// Oculta la pantalla de resultados.
        /// </summary>
        public void Hide()
        {
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(false);
            }
        }
    }
}
