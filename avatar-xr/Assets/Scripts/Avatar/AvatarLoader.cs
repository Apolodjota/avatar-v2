using UnityEngine;
using System;
using System.Collections;

namespace AvatarXR.Avatar
{
    /// <summary>
    /// Carga y configura avatares de Microsoft Rocketbox.
    /// v2: Eliminada dependencia de blendshapes (Rocketbox tiene 0).
    ///     Expresiones 100% vía Animator (Mixamo animations).
    /// </summary>
    public class AvatarLoader : MonoBehaviour
    {
        [Header("Configuración del Avatar")]
        [Tooltip("Prefab del avatar local (arrastrar desde Assets)")]
        [SerializeField] private GameObject avatarPrefab;
        [SerializeField] private Transform spawnPoint;
        
        [Header("Audio")]
        [SerializeField] private bool setupAudioSource = true;

        public void SetSpawnPoint(Transform point)
        {
            spawnPoint = point;
        }
        
        private GameObject loadedAvatar;
        private AudioSource avatarAudioSource;
        
        // Estado emocional
        private int currentStressLevel = 7;
        
        // Eventos
        public event Action<GameObject> OnAvatarLoaded;
        public event Action<string> OnLoadError;

        // Referencia al controlador de Rocketbox
        private RocketboxAvatarController rocketboxController;

        public GameObject LoadedAvatar => loadedAvatar;
        public AudioSource AvatarAudioSource => avatarAudioSource;
        public bool IsLoaded => loadedAvatar != null;

        private void Start()
        {
            LoadAvatar();
        }

        public void LoadAvatar()
        {
            if (avatarPrefab != null)
            {
                Debug.Log("[AvatarLoader] Cargando avatar desde prefab local...");
                LoadFromPrefab();
            }
            else
            {
                Debug.LogError("[AvatarLoader] ¡No hay prefab asignado!");
                OnLoadError?.Invoke("No avatar prefab assigned");
            }
        }
        
        private void LoadFromPrefab()
        {
            // Intentar encontrar avatar existente en la escena primero
            GameObject existingAvatar = GameObject.Find("Male_Adult_01");
            if (existingAvatar == null) existingAvatar = GameObject.Find("Avatar_Paciente");

            if (existingAvatar != null)
            {
                loadedAvatar = existingAvatar;
                Debug.Log($"[AvatarLoader] ✅ Found existing avatar in scene: {loadedAvatar.name}");
            }
            else
            {
                loadedAvatar = Instantiate(avatarPrefab);
                loadedAvatar.name = "Avatar_Paciente";
                Debug.Log("[AvatarLoader] ✅ Avatar instantiated from local prefab");
            }
            
            SetupLoadedAvatar();
        }

        private void SetupLoadedAvatar()
        {
            if (loadedAvatar == null) return;
            
            // Posicionar avatar
            if (spawnPoint != null)
            {
                loadedAvatar.transform.position = spawnPoint.position;
                loadedAvatar.transform.rotation = spawnPoint.rotation;
            }
            else
            {
                loadedAvatar.transform.position = new Vector3(1.43f, 0.05f, 1.079413f);
                loadedAvatar.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }
            
            // Configurar AudioSource
            if (setupAudioSource)
            {
                SetupAudio();
            }

            // Configurar Animator (Rocketbox + Mixamo)
            Animator animator = loadedAvatar.GetComponent<Animator>();
            if (animator != null)
            {
                rocketboxController = loadedAvatar.GetComponent<RocketboxAvatarController>();
                if (rocketboxController == null)
                {
                    rocketboxController = loadedAvatar.AddComponent<RocketboxAvatarController>();
                }
                
                if (animator.runtimeAnimatorController == null)
                {
                    Debug.LogWarning("[AvatarLoader] El avatar tiene Animator pero no Controller. Asigna 'PatientAnimator'.");
                }
                else
                {
                    Debug.Log($"[AvatarLoader] ✅ Animator Controller: {animator.runtimeAnimatorController.name}");
                }

                rocketboxController.SetStressLevel(currentStressLevel);
                Debug.Log("[AvatarLoader] ✅ RocketboxAvatarController configurado");
            }
            else
            {
                Debug.LogWarning("[AvatarLoader] ⚠️ No se encontró Animator en el avatar");
            }
            
            // Lock position to prevent sinking
            // AvatarPositionLock usa sus valores por defecto (lockedY=0.05, lockY=true)
            if (loadedAvatar.GetComponent<AvatarPositionLock>() == null)
            {
                loadedAvatar.AddComponent<AvatarPositionLock>();
            }
            
            // Log blendshape status para diagnóstico
            LogBlendshapeStatus();
            
            Debug.Log("[AvatarLoader] ✅ Avatar configurado correctamente");
            OnAvatarLoaded?.Invoke(loadedAvatar);
        }

        private void LogBlendshapeStatus()
        {
            var renderers = loadedAvatar.GetComponentsInChildren<SkinnedMeshRenderer>();
            int totalBlendshapes = 0;
            foreach (var r in renderers)
            {
                if (r.sharedMesh != null)
                    totalBlendshapes += r.sharedMesh.blendShapeCount;
            }
            
            if (totalBlendshapes == 0)
            {
                Debug.Log("[AvatarLoader] ℹ️ Modelo sin blendshapes. Expresiones 100% vía Animator (Mixamo).");
            }
            else
            {
                Debug.Log($"[AvatarLoader] ℹ️ Modelo tiene {totalBlendshapes} blendshapes.");
            }
        }

        private void SetupAudio()
        {
            avatarAudioSource = loadedAvatar.GetComponent<AudioSource>();
            if (avatarAudioSource == null)
            {
                avatarAudioSource = loadedAvatar.AddComponent<AudioSource>();
            }
            
            avatarAudioSource.spatialBlend = 1f;
            avatarAudioSource.minDistance = 0.5f;
            avatarAudioSource.maxDistance = 10f;
            avatarAudioSource.playOnAwake = false;
        }

        /// <summary>
        /// Actualiza las animaciones emocionales según el nivel de estrés.
        /// Todo se maneja vía Animator (EmotionID + StressLevel parameters).
        /// </summary>
        public void UpdateEmotionalState(int stressLevel)
        {
            currentStressLevel = Mathf.Clamp(stressLevel, 0, 10);
            
            if (rocketboxController != null)
            {
                rocketboxController.SetStressLevel(currentStressLevel);
                Debug.Log($"[AvatarLoader] Emoción actualizada via Animator: stress={currentStressLevel}");
            }
        }

        /// <summary>
        /// Reproduce audio de voz con animación de habla via Animator.
        /// </summary>
        public void Speak(AudioClip clip, System.Action onComplete = null)
        {
            if (avatarAudioSource == null || clip == null)
            {
                onComplete?.Invoke();
                return;
            }
            
            // Activar animación de hablar via Animator
            if (rocketboxController != null)
            {
                rocketboxController.SetTalking(true);
            }

            avatarAudioSource.clip = clip;
            avatarAudioSource.Play();
            
            StartCoroutine(WaitForSpeechEnd(clip.length, () => {
                if (rocketboxController != null)
                {
                    rocketboxController.SetTalking(false);
                }
                onComplete?.Invoke();
            }));
        }

        private IEnumerator WaitForSpeechEnd(float duration, System.Action onComplete)
        {
            yield return new WaitForSeconds(duration + 0.1f);
            onComplete?.Invoke();
        }

        public bool IsSpeaking()
        {
            return avatarAudioSource != null && avatarAudioSource.isPlaying;
        }
    }
}
