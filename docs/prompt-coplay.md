# Prompt Inicial - Proyecto VR Clinical Training (avatar-v2)

## Contexto del Proyecto

Estoy desarrollando un **sistema de entrenamiento inmersivo en VR** para estudiantes de salud, donde practican habilidades de desescalamiento emocional con un avatar paciente en crisis de ansiedad. El proyecto integra **HCI, XR e Inteligencia Artificial**.

### Stack Tecnológico
- **VR/XR:** Unity 6 LTS + Meta Quest Pro + Oculus Integration SDK + XR Interaction Toolkit
- **Avatar:** Ready Player Me (SDK instalado, avatar no integrado aún)
- **Backend:** Python FastAPI
- **IA:** Whisper (STT), Google Gemini Flash (LLM), Google Cloud TTS, XGBoost (clasificación emocional)
- **Base de datos:** MongoDB (planificado)

---

## Estado Actual del Proyecto

### ✅ Completado

**Estructura del proyecto:**
```
avatar-v2/
├── backend/
│   └── main.py              # FastAPI básico con CORS configurado
├── avatar-xr/               # Proyecto Unity
│   ├── Assets/
│   │   ├── Scenes/
│   │   │   ├── MainMenu.unity      # CREADA - UI de configuración
│   │   │   └── Consultorio.unity   # CREADA - Entorno de simulación
│   │   ├── Scripts/
│   │   │   ├── Managers/
│   │   │   ├── UI/
│   │   │   ├── Avatar/
│   │   │   └── Network/
│   │   ├── Ready Player Me/        # SDK instalado
│   │   ├── Oculus/                 # Oculus Integration
│   │   └── [Assets de mobiliario]
│   └── ...
├── requirements.txt
└── .env
```

**Escena Consultorio:**
- Entorno 3D del consultorio con mobiliario (escritorio, sillas, estantes, TV, libros)
- Iluminación configurada
- OVRCameraRig / XR Origin funcionando
- Probado exitosamente en Meta Quest Pro

**Escena MainMenu:**
- Creada con UI de configuración
- Slider de volumen, toggle de barra de estrés, indicador de micrófono, botón iniciar

**Backend:**
- FastAPI con endpoints `/` y `/health`
- CORS configurado para Unity
- Dependencias en requirements.txt (Whisper, librosa, google-generativeai, etc.)

### ⏳ Pendiente / En Progreso

1. **Avatar Conversacional:**
   - Ready Player Me SDK instalado pero avatar NO integrado en escena
   - Falta: cargar avatar, configurar blendshapes (52 ARKit), lip-sync, animaciones emocionales

2. **Conexión Unity ↔ Backend:**
   - No implementada aún
   - Necesita: captura de audio, envío HTTP, recepción de respuestas

3. **Pipeline de IA en Backend:**
   - Endpoints no implementados
   - Falta: `/api/transcribe`, `/api/analyze-emotion`, `/api/generate-response`, `/api/synthesize-speech`

4. **Barra de Estrés Diegética:**
   - Script creado pero posiblemente no conectado

5. **Sistema de Sesiones:**
   - Lógica básica en scripts pero no verificada

---

## Arquitectura del Sistema (Flujo de Datos)

```
[Usuario habla] 
    → [Unity captura audio WAV] 
    → [POST /api/process-turn con audio]
    → [Backend: Whisper STT → Análisis emocional → Gemini genera respuesta → Google TTS]
    → [Respuesta JSON: {texto, audio_url, nuevo_stress, emocion}]
    → [Unity: actualiza avatar, reproduce audio, anima blendshapes, actualiza barra estrés]
```

---

## Casos de Uso Principales

| CU | Descripción | Estado |
|----|-------------|--------|
| CU1 | Configurar sesión (volumen, barra estrés visible) | UI creada, lógica parcial |
| CU2 | Realizar entrevista clínica con avatar | Escena lista, avatar pendiente |
| CU3 | Revisar resultados de sesión | No implementado |

**Condiciones de finalización de sesión:**
- ✅ Éxito: Estrés ≤ 2 después de mínimo 5 turnos
- ❌ Fracaso: Estrés = 10
- ⏱️ Timeout: 15 minutos

---

## Lo que Necesito que Hagas

1. **Primero:** Lee los archivos del proyecto usando MCP para entender el estado actual real (no solo lo que te digo)

2. **Verifica:**
   - Que las escenas MainMenu y Consultorio estén correctamente configuradas
   - Que los scripts existentes compilen y estén conectados
   - Que el OVRCameraRig esté bien configurado para Quest Pro

3. **Siguiente paso prioritario:** Integrar el avatar de Ready Player Me en la escena Consultorio:
   - Cargar un avatar desde URL o asset local
   - Posicionarlo en la silla del paciente (frente al usuario a 1.5m)
   - Configurar los blendshapes para expresiones faciales
   - Preparar el AudioSource para lip-sync

4. **Después:** Conectar Unity con el backend FastAPI para el flujo conversacional

---

## Archivos Clave que Deberías Revisar

**Unity (C#):**
- `Assets/Scripts/Managers/GameManager.cs`
- `Assets/Scripts/Managers/ConsultorioController.cs`
- `Assets/Scripts/UI/MainMenuController.cs`
- `Assets/Scripts/Avatar/AvatarPlaceholder.cs` (si existe)
- Configuración de las escenas en el Inspector

**Backend (Python):**
- `backend/main.py`
- `requirements.txt`
- `.env` (verificar que existan las variables aunque sin valores)

---

## Especificaciones Técnicas del Avatar

Según el diseño:
- **Fuente:** Ready Player Me
- **Polígonos:** ~15K tris
- **Blendshapes:** 52 ARKit standard (para expresiones faciales)
- **Rig:** Humanoid (compatible con Unity Mecanim)
- **Posición:** Sentado frente al usuario a 1.5m de distancia

**Estados emocionales del avatar:**
| Estado | Blendshapes | Voz (TTS) |
|--------|-------------|-----------|
| Ansioso (inicial, estrés 7) | browDown: 0.5, eyesLookDown: 0.4 | stability=0.4 |
| Calmándose (estrés 4-6) | browDown: 0.3, mouthSmile: 0.2 | stability=0.6 |
| Calmado (éxito, estrés ≤2) | mouthSmile: 0.3, eyesRelaxed: 0.8 | stability=0.8 |
| Hostil (estrés 9-10) | browInnerUp: 0.7, jawOpen: 0.3 | stability=0.2, volumen alto |

---

## Preguntas que Podrías Hacerme

- ¿Tienes una URL de avatar de Ready Player Me o prefieres que use uno genérico?
- ¿El micrófono del Quest está funcionando correctamente en pruebas?
- ¿Prefieres que el backend corra localmente o en un servidor?
- ¿Tienes las API keys de Google (Gemini y Cloud TTS) configuradas?

---

## Notas Adicionales

- El proyecto es para una asignatura de **Human-Computer Interaction** en la Universidad Nacional de Loja
- La evaluación incluirá cuestionario **SUS** (usabilidad) y **Presence Questionnaire** de Witmer & Singer
- El desarrollo es individual (estudiante: Washington Andres Apolo Escobar)
- Dispositivo de prueba: **Meta Quest Pro** (no Quest 3)
