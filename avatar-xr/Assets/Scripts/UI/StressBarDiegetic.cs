using UnityEngine;
using UnityEngine.UI;

namespace AvatarXR.UI
{
    /// <summary>
    /// Componente para la barra de estrés diegética que se muestra en el entorno VR.
    /// Simula un termómetro visual integrado en la pared del consultorio.
    /// </summary>
    public class StressBarDiegetic : MonoBehaviour
    {
        [Header("Configuración Visual")]
        [SerializeField] private Transform liquidFill;
        [SerializeField] private Renderer liquidRenderer;
        [SerializeField] private Renderer glassRenderer;

        [Header("Rango del Líquido")]
        [SerializeField] private float minFillY = 0f;
        [SerializeField] private float maxFillY = 1f;

        [Header("Colores por Nivel de Estrés")]
        [SerializeField] private Color lowStressColor = new Color(0.2f, 0.8f, 0.2f, 1f);     // Verde (0-3)
        [SerializeField] private Color mediumStressColor = new Color(0.9f, 0.8f, 0.1f, 1f);  // Amarillo (4-6)
        [SerializeField] private Color highStressColor = new Color(0.9f, 0.4f, 0.1f, 1f);    // Naranja (7-8)
        [SerializeField] private Color criticalStressColor = new Color(0.9f, 0.1f, 0.1f, 1f); // Rojo (9-10)

        [Header("Animación")]
        [SerializeField] private float animationDuration = 0.5f;
        [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Efectos")]
        [SerializeField] private ParticleSystem warningParticles;
        [SerializeField] private Light stressLight;
        [SerializeField] private float lightIntensityMultiplier = 0.5f;

        [Header("Canvas UI (Alternativa)")]
        [SerializeField] private Slider uiSlider;
        [SerializeField] private Image uiFillImage;
        [SerializeField] private TMPro.TextMeshProUGUI levelText;

        private int currentLevel = 7;
        private float targetFillAmount = 0.7f;
        private float currentFillAmount = 0.7f;
        private Color targetColor;
        private Coroutine animationCoroutine;

        // Material property IDs para eficiencia
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            UpdateVisuals(7); // Nivel inicial según diseño
        }

        /// <summary>
        /// Actualiza el nivel de estrés mostrado en la barra.
        /// </summary>
        /// <param name="level">Nivel de estrés de 0 a 10</param>
        /// <param name="animate">Si se debe animar la transición</param>
        public void SetStressLevel(int level, bool animate = true)
        {
            level = Mathf.Clamp(level, 0, 10);
            
            if (level == currentLevel) return;

            currentLevel = level;
            targetFillAmount = level / 10f;
            targetColor = GetColorForLevel(level);

            if (animate)
            {
                if (animationCoroutine != null)
                {
                    StopCoroutine(animationCoroutine);
                }
                animationCoroutine = StartCoroutine(AnimateToTarget());
            }
            else
            {
                currentFillAmount = targetFillAmount;
                UpdateVisuals(level);
            }

            // Efectos de alerta para niveles críticos
            UpdateAlertEffects(level);
        }

        private System.Collections.IEnumerator AnimateToTarget()
        {
            float startFillAmount = currentFillAmount;
            Color startColor = GetColorForLevel(Mathf.RoundToInt(startFillAmount * 10));
            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = easingCurve.Evaluate(elapsed / animationDuration);

                currentFillAmount = Mathf.Lerp(startFillAmount, targetFillAmount, t);
                Color currentColor = Color.Lerp(startColor, targetColor, t);

                UpdateVisualsInternal(currentFillAmount, currentColor);

                yield return null;
            }

            currentFillAmount = targetFillAmount;
            UpdateVisuals(currentLevel);
        }

        private void UpdateVisuals(int level)
        {
            Color color = GetColorForLevel(level);
            UpdateVisualsInternal(level / 10f, color);

            // Actualizar texto
            if (levelText != null)
            {
                levelText.text = $"{level}/10";
                levelText.color = color;
            }
        }

        private void UpdateVisualsInternal(float fillAmount, Color color)
        {
            // Actualizar objeto 3D (termómetro físico)
            if (liquidFill != null)
            {
                Vector3 scale = liquidFill.localScale;
                scale.y = Mathf.Lerp(minFillY, maxFillY, fillAmount);
                liquidFill.localScale = scale;
            }

            if (liquidRenderer != null)
            {
                liquidRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ColorProperty, color);
                propertyBlock.SetColor(EmissionColorProperty, color * 0.3f);
                liquidRenderer.SetPropertyBlock(propertyBlock);
            }

            // Actualizar UI Canvas (alternativa)
            if (uiSlider != null)
            {
                uiSlider.value = fillAmount;
            }

            if (uiFillImage != null)
            {
                uiFillImage.color = color;
            }

            // Actualizar luz ambiental
            if (stressLight != null)
            {
                stressLight.color = color;
                stressLight.intensity = fillAmount * lightIntensityMultiplier;
            }
        }

        private Color GetColorForLevel(int level)
        {
            if (level <= 3) return lowStressColor;
            if (level <= 6) return mediumStressColor;
            if (level <= 8) return highStressColor;
            return criticalStressColor;
        }

        private void UpdateAlertEffects(int level)
        {
            // Activar partículas de advertencia en niveles críticos
            if (warningParticles != null)
            {
                if (level >= 9)
                {
                    if (!warningParticles.isPlaying)
                    {
                        warningParticles.Play();
                    }
                }
                else
                {
                    if (warningParticles.isPlaying)
                    {
                        warningParticles.Stop();
                    }
                }
            }
        }

        /// <summary>
        /// Hace parpadear la barra para llamar la atención.
        /// </summary>
        public void Pulse()
        {
            StartCoroutine(PulseRoutine());
        }

        private System.Collections.IEnumerator PulseRoutine()
        {
            Color originalColor = GetColorForLevel(currentLevel);
            Color pulseColor = Color.white;

            for (int i = 0; i < 2; i++)
            {
                UpdateVisualsInternal(currentFillAmount, pulseColor);
                yield return new WaitForSeconds(0.1f);
                UpdateVisualsInternal(currentFillAmount, originalColor);
                yield return new WaitForSeconds(0.1f);
            }
        }

        /// <summary>
        /// Muestra/oculta la barra de estrés.
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

#if UNITY_EDITOR
        [ContextMenu("Test Level 2 (Success)")]
        private void TestLevel2() => SetStressLevel(2);

        [ContextMenu("Test Level 5 (Medium)")]
        private void TestLevel5() => SetStressLevel(5);

        [ContextMenu("Test Level 7 (High)")]
        private void TestLevel7() => SetStressLevel(7);

        [ContextMenu("Test Level 10 (Critical)")]
        private void TestLevel10() => SetStressLevel(10);
#endif
    }
}
