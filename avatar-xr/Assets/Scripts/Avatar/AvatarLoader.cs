using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;

namespace AvatarXR.Avatar
{
    /// <summary>
    /// Carga y configura avatares de Ready Player Me.
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
            // Instanciar el prefab local
            loadedAvatar = Instantiate(avatarPrefab);
            loadedAvatar.name = "Avatar_Paciente";
            
            Debug.Log("[AvatarLoader] ✅ Avatar instanciado desde prefab local");
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
                // Posición por defecto: frente al usuario
                loadedAvatar.transform.position = Camera.main.transform.position + 
                    Camera.main.transform.forward * distanceFromUser;
                loadedAvatar.transform.LookAt(Camera.main.transform);
                loadedAvatar.transform.rotation = Quaternion.Euler(0, loadedAvatar.transform.rotation.eulerAngles.y + 180, 0);
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
            
            Debug.Log("[AvatarLoader] ✅ Avatar configurado correctamente");
            OnAvatarLoaded?.Invoke(loadedAvatar);
        }

        private void SetupBlendshapes()
        {
            faceMeshRenderer = loadedAvatar.GetComponentInChildren<SkinnedMeshRenderer>();
            
            if (faceMeshRenderer == null || faceMeshRenderer.sharedMesh == null)
            {
                Debug.LogWarning("[AvatarLoader] No se encontró SkinnedMeshRenderer para blendshapes");
                return;
            }
            
            Mesh mesh = faceMeshRenderer.sharedMesh;
            
            // Mapear blendshapes ARKit estándar de Ready Player Me
            browDownLeftIndex = mesh.GetBlendShapeIndex("browDownLeft");
            browDownRightIndex = mesh.GetBlendShapeIndex("browDownRight");
            eyeLookDownLeftIndex = mesh.GetBlendShapeIndex("eyeLookDownLeft");
            eyeLookDownRightIndex = mesh.GetBlendShapeIndex("eyeLookDownRight");
            mouthFrownLeftIndex = mesh.GetBlendShapeIndex("mouthFrownLeft");
            mouthFrownRightIndex = mesh.GetBlendShapeIndex("mouthFrownRight");
            mouthSmileLeftIndex = mesh.GetBlendShapeIndex("mouthSmileLeft");
            mouthSmileRightIndex = mesh.GetBlendShapeIndex("mouthSmileRight");
            jawOpenIndex = mesh.GetBlendShapeIndex("jawOpen");
            
            Debug.Log($"[AvatarLoader] Blendshapes mapeados. Total: {mesh.blendShapeCount}");
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
            
            if (faceMeshRenderer == null) return;
            
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
            
            avatarAudioSource.clip = clip;
            avatarAudioSource.Play();
            
            StartCoroutine(LipSyncCoroutine(clip.length, onComplete));
        }

        private IEnumerator LipSyncCoroutine(float duration, System.Action onComplete)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                // Lip-sync básico: jaw open siguiendo el audio
                if (avatarAudioSource.isPlaying)
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
            SetBlendshapeWeight(jawOpenIndex, 0f);
            
            onComplete?.Invoke();
        }

        public bool IsSpeaking()
        {
            return avatarAudioSource != null && avatarAudioSource.isPlaying;
        }
    }
}
