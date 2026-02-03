# Guía de Configuración de Escenas - VR Clinical Training

Esta guía detalla cómo configurar las escenas **MainMenu** y **Consultorio** para tu proyecto de entrenamiento clínico en VR con Meta Quest Pro.

---

## 📁 Estructura de Scripts

Primero, crea la siguiente estructura de carpetas dentro de `Assets/Scripts/`:

```
Assets/
└── Scripts/
    ├── Managers/
    │   ├── GameManager.cs
    │   └── ConsultorioController.cs
    ├── UI/
    │   ├── MainMenuController.cs
    │   └── StressBarDiegetic.cs
    ├── XR/
    │   └── XRRigSetup.cs
    └── Avatar/
        └── AvatarPlaceholder.cs
```

Copia los scripts proporcionados a sus respectivas carpetas.

---

## 🎬 Escena 1: MainMenu

### Paso 1: Crear la Escena

1. **File > New Scene** (selecciona "Basic (Built-in)")
2. **File > Save As** → `Assets/Scenes/MainMenu.unity`

### Paso 2: Configurar OVRCameraRig

1. Elimina la `Main Camera` existente
2. **Busca el prefab OVRCameraRig:**
   - En Project: `Assets/Oculus/VR/Prefabs/OVRCameraRig.prefab`
   - Arrástralo a la jerarquía
3. **Posiciona el rig:**
   - Position: `(0, 0, 0)`
   - Rotation: `(0, 0, 0)`

### Paso 3: Crear el Canvas UI para VR

1. **GameObject > UI > Canvas**
2. Configura el Canvas:
   - **Render Mode:** `World Space`
   - **Event Camera:** Arrastra `OVRCameraRig/TrackingSpace/CenterEyeAnchor`
   - **Width:** `1`
   - **Height:** `0.7`
   - **Position:** `(0, 1.5, 2)` (2 metros frente al usuario, a altura de ojos)
   - **Scale:** `(0.002, 0.002, 0.002)`
3. Añade `Canvas Group` component al Canvas (para el fade)

### Paso 4: Crear UI del Menú Principal

Dentro del Canvas, crea la siguiente jerarquía:

```
Canvas (MainMenuCanvas)
├── Panel_Background
│   └── Image (semi-transparente)
├── Panel_AvatarPreview (centro)
│   └── RawImage o placeholder para preview del avatar
├── Panel_Controls (derecha)
│   ├── Text_Title ("Configuración de Sesión")
│   ├── VolumeControl
│   │   ├── Text_VolumeLabel ("Volumen")
│   │   ├── Slider_Volume
│   │   └── Text_VolumeValue ("75%")
│   └── StressBarControl
│       ├── Text_StressBarLabel ("Barra de Estrés Visible")
│       └── Toggle_StressBar
├── Panel_MicStatus (abajo izquierda)
│   ├── Image_MicIndicator (círculo de color)
│   └── Text_MicStatus ("Micrófono: Verificando...")
└── Button_StartSession (centro-abajo)
    └── Text ("INICIAR SESIÓN")
```

### Paso 5: Configurar Componentes UI

**Slider de Volumen:**
- Min Value: `0`
- Max Value: `1`
- Value: `0.75`
- Whole Numbers: `false`

**Toggle de Barra de Estrés:**
- Is On: `true`

**Image del Indicador de Micrófono:**
- Width/Height: `30x30`
- Color inicial: Gris `(0.5, 0.5, 0.5, 1)`

**Botón de Inicio:**
- Font Size: `24`
- Colores:
  - Normal: `(0.2, 0.6, 0.9, 1)` (azul)
  - Highlighted: `(0.3, 0.7, 1, 1)`
  - Pressed: `(0.1, 0.5, 0.8, 1)`
  - Disabled: `(0.5, 0.5, 0.5, 0.5)`

### Paso 6: Añadir GameManager

1. Crea un **GameObject vacío** llamado `GameManager`
2. Añade el script `GameManager.cs`
3. Este objeto persistirá entre escenas

### Paso 7: Añadir MainMenuController

1. Selecciona el Canvas
2. Añade el script `MainMenuController.cs`
3. **Asigna las referencias** en el Inspector:
   - `volumeSlider` → Slider_Volume
   - `stressBarToggle` → Toggle_StressBar
   - `microphoneIndicator` → Image_MicIndicator
   - `startSessionButton` → Button_StartSession
   - `volumeValueText` → Text_VolumeValue
   - `microphoneStatusText` → Text_MicStatus
   - `menuCanvasGroup` → Canvas Group del Canvas

### Paso 8: Añadir XR Interaction

1. Añade `OVRRaycaster` al Canvas para interacción con puntero VR
2. Configura el `EventSystem` si no existe:
   - **GameObject > UI > Event System**
   - Añade `OVRInputModule` y desactiva `StandaloneInputModule`

### Paso 9: Iluminación Básica

1. **GameObject > Light > Directional Light**
   - Rotation: `(50, -30, 0)`
   - Intensity: `0.8`
   - Color: Blanco cálido

---

## 🏥 Escena 2: Consultorio

### Paso 1: Crear la Escena Base

1. **File > New Scene**
2. **File > Save As** → `Assets/Scenes/Consultorio.unity`
3. **O bien**, duplica tu escena actual del consultorio (SampleScene)

### Paso 2: Configurar OVRCameraRig

1. Si no existe, añade `OVRCameraRig` prefab
2. **Crear punto de spawn del usuario:**
   - GameObject vacío `UserSpawnPoint`
   - Position: Donde se sentará el terapeuta (ej: `(0, 0, 2)`)
   - Rotation: Mirando hacia el avatar/paciente
3. Añade el script `XRRigSetup.cs` al OVRCameraRig
4. Asigna `UserSpawnPoint` en el Inspector

### Paso 3: Posicionar el Avatar Placeholder

1. Crea un **GameObject vacío** llamado `AvatarPaciente`
2. **Posición:** Frente al usuario, a 1.5m de distancia
   - Si el usuario está en `(0, 0, 2)`, el avatar en `(0, 0, 0.5)`
3. Añade el script `AvatarPlaceholder.cs`
4. El script creará automáticamente una visualización temporal

### Paso 4: Crear la Barra de Estrés Diegética

**Opción A: Usando Canvas en World Space (Recomendado para empezar)**

1. **GameObject > UI > Canvas**
2. Configura:
   - **Render Mode:** `World Space`
   - **Position:** En la pared izquierda del consultorio (ej: `(-2, 1.5, 0)`)
   - **Rotation:** `(0, 90, 0)` (mirando hacia el usuario)
   - **Scale:** `(0.003, 0.003, 0.003)`
3. Dentro del Canvas:
   ```
   Canvas_StressBar
   ├── Panel_Background (marco del termómetro)
   ├── Slider_StressBar
   │   └── Fill Area
   │       └── Fill (Image con gradiente)
   └── Text_Level ("7/10")
   ```
4. Añade `StressBarDiegetic.cs` al Canvas
5. Configura el **Gradient** en el Inspector:
   - 0%: Verde `(0.2, 0.8, 0.2)`
   - 50%: Amarillo `(0.9, 0.8, 0.1)`
   - 75%: Naranja `(0.9, 0.4, 0.1)`
   - 100%: Rojo `(0.9, 0.1, 0.1)`

**Opción B: Usando objeto 3D (Más inmersivo)**

1. Crea un termómetro 3D con cilindros/cubos
2. Usa shaders con MaterialPropertyBlock para animar el color

### Paso 5: Crear Indicador de Micrófono (No Diegético)

1. Crea un Canvas **Screen Space - Overlay** o adjunto al OVRCameraRig
2. Posición: Esquina inferior del campo visual
3. Elementos:
   - Círculo/Aro de color (Image)
   - Texto de estado (opcional)
4. Añade a `ConsultorioController` las referencias

### Paso 6: Crear Menú de Pausa

1. Crea un Canvas World Space
2. Posición: `(0, 1.5, 1)` (frente al usuario cuando pausa)
3. Inicialmente **desactivado** (`SetActive(false)`)
4. Elementos:
   ```
   Canvas_PauseMenu
   ├── Panel_Overlay (oscurece el fondo, alpha 0.5)
   ├── Panel_Menu
   │   ├── Text_Title ("Sesión Pausada")
   │   ├── Button_Resume ("Reanudar")
   │   ├── Button_Restart ("Reiniciar")
   │   └── Button_Exit ("Salir")
   ```

### Paso 7: Añadir ConsultorioController

1. Crea **GameObject vacío** `SessionManager`
2. Añade `ConsultorioController.cs`
3. Asigna todas las referencias en el Inspector:
   - `avatarPlaceholder` → AvatarPaciente
   - `stressBarObject` → Canvas_StressBar
   - `stressBarSlider` → Slider_StressBar
   - `pauseMenuCanvas` → Canvas_PauseMenu
   - etc.

### Paso 8: Configurar Lighting

Para un consultorio realista:

1. **Directional Light** (luz principal de ventana):
   - Rotation: `(50, -30, 0)`
   - Intensity: `1.0`
   - Color: Blanco cálido `(255, 244, 229)`
   - Shadow Type: `Soft Shadows`

2. **Point Light** (lámpara del consultorio):
   - Position: Sobre el escritorio
   - Range: `5`
   - Intensity: `0.5`
   - Color: Blanco cálido

3. **Ambient Light** (Edit > Render Pipeline > Lighting):
   - Source: `Color`
   - Ambient Color: `(200, 200, 210)` (gris azulado suave)
   - Intensity: `0.3`

### Paso 9: Configurar Audio Espacial

1. En el **OVRCameraRig**, verifica:
   - `OVRManager` > `Enable Audio Spatialization`: ✓
2. En el `AudioSource` del avatar:
   - `Spatial Blend`: `1` (100% 3D)
   - `Doppler Level`: `0`
   - `Spread`: `0`
   - `Min Distance`: `1`
   - `Max Distance`: `10`

---

## ⚙️ Build Settings

### Configurar las Escenas

1. **File > Build Settings**
2. Añade las escenas en este orden:
   - `Scenes/MainMenu` (index 0)
   - `Scenes/Consultorio` (index 1)

### Player Settings para Meta Quest Pro

1. **Edit > Project Settings > Player**
2. **Android tab:**
   - Company Name: Tu nombre
   - Product Name: "VR Clinical Training"
   - Minimum API Level: `API level 29`
   - Target API Level: `Automatic (highest installed)`
3. **XR Plug-in Management:**
   - Oculus: ✓

---

## 🧪 Testing

### En el Editor

1. Usa `XR Device Simulator` o conecta el Quest via Link
2. Para probar sin VR:
   - Puedes usar el ratón para simular la mirada
   - Los botones UI funcionan con click normal

### En el Dispositivo

1. **Build and Run** con el Quest conectado
2. O genera APK y transfiere manualmente

---

## 📋 Checklist Final

### MainMenu.unity
- [ ] OVRCameraRig posicionado
- [ ] Canvas World Space con UI
- [ ] GameManager en la escena
- [ ] MainMenuController configurado
- [ ] OVRRaycaster en Canvas
- [ ] EventSystem con OVRInputModule

### Consultorio.unity
- [ ] OVRCameraRig con XRRigSetup
- [ ] Mobiliario del consultorio (ya tienes)
- [ ] AvatarPlaceholder posicionado
- [ ] Barra de estrés diegética
- [ ] Indicador de micrófono
- [ ] Menú de pausa
- [ ] ConsultorioController configurado
- [ ] Iluminación configurada
- [ ] Audio espacial configurado

---

## 🔗 Siguiente Paso

Una vez configuradas las escenas, el siguiente paso será:
1. Integrar el avatar de Ready Player Me
2. Conectar con el backend FastAPI
3. Implementar captura de audio y comunicación con el servidor

¿Necesitas ayuda con algún paso específico?
