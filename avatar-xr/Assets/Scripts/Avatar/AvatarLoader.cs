using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;

namespace AvatarXR.Avatar
{
    /// <summary>
    /// Carga y configura avatares de Ready Player Me.
    /// Actualizado para soportar Microsoft Rocketbox.
    /// </summary>
    public class AvatarLoader : MonoBehaviour
    {
        [Header("Configuración del Avatar")]
        [Tooltip("Prefab del avatar local (arrastrar desde Assets)")]
        [SerializeField] private GameObject avatarPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float distanceFromUser = 1.5f;
        
        [Header("Blendshapes ARKit (52 estándar)")]
        [SerializeField] private bool enableBlendshapes = true;
        
        [Header("Audio")]
        [SerializeField] private bool setupAudioSource = true;

        /// <summary>
        /// Establece el punto de aparición del avatar.
        /// </summary>
        public void SetSpawnPoint(Transform point)
        {
            spawnPoint = point;
        }
        
        private GameObject loadedAvatar;
        private SkinnedMeshRenderer faceMeshRenderer;
        private AudioSource avatarAudioSource;
        
        // Estado emocional
        private int currentStressLevel = 7;
        
        // Eventos
        public event Action<GameObject> OnAvatarLoaded;
        public event Action<string> OnLoadError;
        
        // Blendshape indices (se asignan en runtime)
        private int browDownLeftIndex = -1;
        private int browDownRightIndex = -1;
        private int eyeLookDownLeftIndex = -1;
        private int eyeLookDownRightIndex = -1;
        private int mouthFrownLeftIndex = -1;
        private int mouthFrownRightIndex = -1;
        private int mouthSmileLeftIndex = -1;
        private int mouthSmileRightIndex = -1;
        private int jawOpenIndex = -1;

        // Referencia al controlador de Rocketbox
        private RocketboxAvatarController rocketboxController;

        public GameObject LoadedAvatar => loadedAvatar;
        public AudioSource AvatarAudioSource => avatarAudioSource;
        public bool IsLoaded => loadedAvatar != null;

        private void Start()
        {
            LoadAvatar();
        }

        /// <summary>
        /// Carga el avatar desde el prefab local asignado.
        /// </summary>
        public void LoadAvatar()
        {
            if (avatarPrefab != null)
            {
                Debug.Log("[AvatarLoader] Cargando avatar desde prefab local...");
                LoadFromPrefab();
            }
            else
            {
                Debug.LogError("[AvatarLoader] ¡No hay prefab asignado! Arrastra el prefab del avatar al campo 'Avatar Prefab' en el Inspector.");
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
                // Instanciar el prefab local si no existe
                loadedAvatar = Instantiate(avatarPrefab);
                loadedAvatar.name = "Avatar_Paciente";
                Debug.Log("[AvatarLoader] ✅ Avatar instantiated from local prefab");
            }
            
            SetupLoadedAvatar();
        }

        private void CreatePlaceholderAvatar()
        {
            // Crear avatar placeholder básico
            loadedAvatar = new GameObject("Avatar_Placeholder");
            
            // Cuerpo
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(loadedAvatar.transform);
            body.transform.localPosition = new Vector3(0, 0.9f, 0);
            body.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            
            // Cabeza
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(loadedAvatar.transform);
            head.transform.localPosition = new Vector3(0, 1.6f, 0);
            head.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            
            // Material azul semitransparente
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.3f, 0.5f, 0.8f, 0.7f);
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            
            body.GetComponent<Renderer>().material = mat;
            head.GetComponent<Renderer>().material = mat;
            
            // Destruir colliders
            Destroy(body.GetComponent<Collider>());
            Destroy(head.GetComponent<Collider>());
            
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
                // Posición fija solicitada por usuario (Adjusted for sitting)
                loadedAvatar.transform.position = new Vector3(1.43f, 0.05f, 1.079413f);
                loadedAvatar.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }
            
            // Buscar SkinnedMeshRenderer para blendshapes
            if (enableBlendshapes)
            {
                SetupBlendshapes();
            }
            
            // Configurar AudioSource
            if (setupAudioSource)
            {
                SetupAudio();
            }

            // *** Nuevo soporte para Rocketbox / Animator ***
            Animator animator = loadedAvatar.GetComponent<Animator>();
            if (animator != null)
            {
                // Si tiene Animator (Rocketbox), añadir controlador si no existe
                rocketboxController = loadedAvatar.GetComponent<RocketboxAvatarController>();
                if (rocketboxController == null)
                {
                    rocketboxController = loadedAvatar.AddComponent<RocketboxAvatarController>();
                }
                
                // Asignar controlador de animaciones generado si no tiene uno
                if (animator.runtimeAnimatorController == null)
                {
                    Debug.LogWarning("[AvatarLoader] El avatar tiene Animator pero no Controller. Asegúrate de asignar 'PatientAnimator' al prefab o usar RocketboxSetup.");
                }

                // Inicializar estado
                rocketboxController.SetStressLevel(currentStressLevel);
            }
            
            // Lock position to prevent sinking during animations
            var posLock = loadedAvatar.GetComponent<AvatarPositionLock>();
            if (posLock == null)
            {
                posLock = loadedAvatar.AddComponent<AvatarPositionLock>();
                posLock.lockedY = 0.05f;
                posLock.lockY = true;
                Debug.Log("[AvatarLoader] ✅ AvatarPositionLock added");
            }
            
            Debug.Log("[AvatarLoader] ✅ Avatar configurado correctamente");
            OnAvatarLoaded?.Invoke(loadedAvatar);
        }

        private void SetupBlendshapes()
        {
            // Try specific names common in Rocketbox/RPM
            faceMeshRenderer = loadedAvatar.transform.Find("face")?.GetComponent<SkinnedMeshRenderer>();
            if (faceMeshRenderer == null) faceMeshRenderer = loadedAvatar.transform.Find("Body")?.GetComponent<SkinnedMeshRenderer>();
            if (faceMeshRenderer == null) faceMeshRenderer = loadedAvatar.GetComponentInChildren<SkinnedMeshRenderer>();
            
            if (faceMeshRenderer == null || faceMeshRenderer.sharedMesh == null)
            {
                Debug.LogWarning("[AvatarLoader] No se encontró SkinnedMeshRenderer para blendshapes (Buscado: face, Body, Children)");
                return;
            }
            
            Mesh mesh = faceMeshRenderer.sharedMesh;
            
            // Mapear blendshapes, intentando varios nombres comunes
            browDownLeftIndex = FindBlendShape(mesh, "browDownLeft", "Brows D L", "Brows Down Left");
            browDownRightIndex = FindBlendShape(mesh, "browDownRight", "Brows D R", "Brows Down Right");
            eyeLookDownLeftIndex = FindBlendShape(mesh, "eyeLookDownLeft", "Eyes Look D L");
            eyeLookDownRightIndex = FindBlendShape(mesh, "eyeLookDownRight", "Eyes Look D R");
            mouthFrownLeftIndex = FindBlendShape(mesh, "mouthFrownLeft", "Frown L");
            mouthFrownRightIndex = FindBlendShape(mesh, "mouthFrownRight", "Frown R");
            mouthSmileLeftIndex = FindBlendShape(mesh, "mouthSmileLeft", "Smile L");
            mouthSmileRightIndex = FindBlendShape(mesh, "mouthSmileRight", "Smile R");
            jawOpenIndex = FindBlendShape(mesh, "jawOpen", "Jaw Open", "Mouth Open");
            
            Debug.Log($"[AvatarLoader] Blendshapes mapeados. Total Mesh: {mesh.blendShapeCount}. JawIndex: {jawOpenIndex}");
        }

        private int FindBlendShape(Mesh mesh, params string[] names)
        {
            foreach (var name in names)
            {
                int index = mesh.GetBlendShapeIndex(name);
                if (index != -1) return index;
            }
            return -1;
        }

        private void SetupAudio()
        {
            avatarAudioSource = loadedAvatar.GetComponent<AudioSource>();
            if (avatarAudioSource == null)
            {
                avatarAudioSource = loadedAvatar.AddComponent<AudioSource>();
            }
            
            avatarAudioSource.spatialBlend = 1f; // Audio 3D
            avatarAudioSource.minDistance = 0.5f;
            avatarAudioSource.maxDistance = 10f;
            avatarAudioSource.playOnAwake = false;
        }

        /// <summary>
        /// Actualiza las expresiones faciales según el nivel de estrés.
        /// </summary>
        public void UpdateEmotionalState(int stressLevel)
        {
            currentStressLevel = Mathf.Clamp(stressLevel, 0, 10);
            
            // Actualizar Rocketbox Animator (Cuerpo)
            if (rocketboxController != null)
            {
                rocketboxController.SetStressLevel(currentStressLevel);
            }

            // Actualizar Blendshapes (Cara - si existen)
            if (faceMeshRenderer != null)
            {
                float intensity = currentStressLevel / 10f;
                
                // Estrés alto = ceño fruncido, mirada baja, gesto tenso
                SetBlendshapeWeight(browDownLeftIndex, intensity * 60f);
                SetBlendshapeWeight(browDownRightIndex, intensity * 60f);
                SetBlendshapeWeight(eyeLookDownLeftIndex, intensity * 40f);
                SetBlendshapeWeight(eyeLookDownRightIndex, intensity * 40f);
                
                // Estrés bajo = sonrisa
                float smileIntensity = Mathf.Clamp01(1f - intensity) * 0.5f;
                SetBlendshapeWeight(mouthSmileLeftIndex, smileIntensity * 100f);
                SetBlendshapeWeight(mouthSmileRightIndex, smileIntensity * 100f);
                
                // Frown en estrés alto
                SetBlendshapeWeight(mouthFrownLeftIndex, intensity * 40f);
                SetBlendshapeWeight(mouthFrownRightIndex, intensity * 40f);
            }
        }

        private void SetBlendshapeWeight(int index, float weight)
        {
            if (index >= 0 && faceMeshRenderer != null)
            {
                faceMeshRenderer.SetBlendShapeWeight(index, weight);
            }
        }

        /// <summary>
        /// Reproduce audio de voz con lip-sync básico.
        /// </summary>
        public void Speak(AudioClip clip, System.Action onComplete = null)
        {
            if (avatarAudioSource == null || clip == null) return;
            
            // Activar animación de hablar en Rocketbox
            if (rocketboxController != null)
            {
                rocketboxController.SetTalking(true);
            }

            avatarAudioSource.clip = clip;
            avatarAudioSource.Play();
            
            StartCoroutine(LipSyncCoroutine(clip.length, () => {
                // Al terminar de hablar
                if (rocketboxController != null)
                {
                    rocketboxController.SetTalking(false);
                }
                onComplete?.Invoke();
            }));
        }

        private IEnumerator LipSyncCoroutine(float duration, System.Action onComplete)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                // Lip-sync básico: jaw open siguiendo el audio
                // Solo si tenemos blendshapes (si no, Rocketbox hace movimiento de cuerpo solamente)
                if (avatarAudioSource.isPlaying && faceMeshRenderer != null)
                {
                    float[] samples = new float[256];
                    avatarAudioSource.GetOutputData(samples, 0);
                    
                    float sum = 0f;
                    foreach (float sample in samples)
                    {
                        sum += Mathf.Abs(sample);
                    }
                    float amplitude = sum / samples.Length * 100f;
                    
                    SetBlendshapeWeight(jawOpenIndex, Mathf.Clamp(amplitude * 50f, 0f, 100f));
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Cerrar boca al terminar
            if (faceMeshRenderer != null)
            {
                SetBlendshapeWeight(jawOpenIndex, 0f);
            }
            
            onComplete?.Invoke();
        }

        public bool IsSpeaking()
        {
            return avatarAudioSource != null && avatarAudioSource.isPlaying;
        }
    }
}
