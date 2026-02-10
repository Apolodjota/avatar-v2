from fastapi import APIRouter, UploadFile, File, HTTPException, Query
from pydantic import BaseModel
from datetime import datetime
import os
import uuid

router = APIRouter()

# Servicios inicializados de forma lazy
_whisper = None
_emotion_clf = None
_llm = None
_tts = None

def get_services():
    """Inicializa los servicios de IA de forma lazy."""
    global _whisper, _emotion_clf, _llm, _tts
    
    # Whisper: Intentar cargar real, si falla usar mock
    if _whisper is None:
        print("=" * 60)
        print("🔧 Inicializando Whisper STT...")
        try:
            from services.whisper_stt import get_whisper_service
            print("📦 Módulo whisper_stt importado")
            _whisper = get_whisper_service()
            print("✅ WhisperSTT real cargado exitosamente")
        except Exception as e:
            print(f"❌ Error cargando Whisper real: {type(e).__name__}: {e}")
            import traceback
            traceback.print_exc()
            print("⚠️ Usando MockWhisper para pruebas")
            try:
                from services.mock_whisper import get_mock_whisper_service
                _whisper = get_mock_whisper_service()
                print("✅ MockWhisper cargado")
            except Exception as e2:
                print(f"❌ Error cargando MockWhisper: {e2}")
                _whisper = None
        print("=" * 60)
    
    if _emotion_clf is None:
        try:
            from services.emotion_classifier import EmotionClassifier
            _emotion_clf = EmotionClassifier()
        except Exception as e:
            print(f"⚠️ Error cargando EmotionClassifier: {e}")
            _emotion_clf = None
    
    if _llm is None:
        try:
            from services.llm_service import LLMService
            _llm = LLMService()
        except Exception as e:
            print(f"⚠️ Error cargando LLMService: {e}")
            _llm = None
    
    # TTS: Usar simple_tts (no requiere credenciales)
    if _tts is None:
        try:
            from services.simple_tts import SimpleTTSService
            _tts = SimpleTTSService()
            print("✅ Usando SimpleTTSService (gTTS)")
        except Exception as e:
            print(f"⚠️ Error cargando SimpleTTS: {e}")
            # Intentar TTS original si existe
            try:
                from services.tts_service import TTSService
                _tts = TTSService()
            except:
                _tts = None
    
    return _whisper, _emotion_clf, _llm, _tts

class ConversationTurn(BaseModel):
    stress_level: int
    conversation_history: list
    turn_count: int

@router.post("/process-audio")
async def process_user_audio(
    audio: UploadFile = File(...),
    session_id: str = Query(None),
    stress_level: int = 7,
    turn_count: int = 0
):
    """
    Endpoint principal: procesar audio del usuario y generar respuesta del avatar.
    
    Pipeline:
    1. Guardar audio temporal
    2. Transcribir con Whisper
    3. Clasificar emoción
    4. Actualizar nivel de estrés
    5. Generar respuesta con LLM
    6. Sintetizar voz
    7. Retornar todo
    """
    # Guardar audio temporal
    temp_id = str(uuid.uuid4())
    audio_path = f"temp/{temp_id}.wav"
    os.makedirs("temp", exist_ok=True)
    
    with open(audio_path, "wb") as f:
        f.write(await audio.read())
    
    try:
        # Obtener servicios
        whisper, emotion_clf, llm, tts = get_services()
        
        if whisper is None:
            raise HTTPException(status_code=503, detail="Servicio Whisper no disponible")
        
        # 1. Transcribir
        print(f"🎤 Transcribiendo audio: {audio_path}")
        transcription = whisper.transcribe(audio_path)
        user_text = transcription["text"]
        print(f"📝 Transcripción: '{user_text}'")
        
        # 2. Clasificar emoción
        if emotion_clf is not None:
            try:
                emotion_result = emotion_clf.classify(audio_path)
                user_emotion = emotion_result["emotion"]
                emotion_confidence = emotion_result["confidence"]
            except Exception as e:
                print(f"⚠️ Error en clasificación de emoción: {e}")
                user_emotion = "neutro"
                emotion_confidence = 0.5
        else:
            user_emotion = "neutro"
            emotion_confidence = 0.5
        
        # 3. Actualizar estrés basado en emoción
        stress_delta = {
            "empatico": -2,
            "hostil": +2,
            "neutro": 0,
            "ansioso": +1
        }.get(user_emotion, 0)
        
        new_stress = max(0, min(10, stress_level + stress_delta))
        
        # 4. Generar respuesta del avatar
        if llm is not None:
            try:
                avatar_response = llm.generate_response(
                    user_input=user_text,
                    stress_level=new_stress,
                    conversation_history=[],  # TODO: pasar historial real
                    turn_count=turn_count + 1
                )
            except Exception as e:
                print(f"⚠️ Error en LLM: {e}")
                avatar_response = "Entiendo lo que me dices... necesito un momento para pensar."
        else:
            avatar_response = "Entiendo lo que me dices... necesito un momento para pensar."
        
        # 5. Sintetizar voz
        audio_output_filename = f"{temp_id}_response.mp3"
        audio_output_path = f"temp/{audio_output_filename}"
        
        try:
            if tts is not None:
                tts.synthesize(
                    text=avatar_response,
                    stress_level=new_stress,
                    output_path=audio_output_path
                )
            else:
                audio_output_filename = None
        except Exception as e:
            print(f"⚠️ Error en TTS: {e}")
            audio_output_filename = None
        
        # 6. Guardar turno en base de datos
        if session_id:
            try:
                from services.database import get_db
                db = await get_db()
                await db.save_turn({
                    "session_id": session_id,
                    "turn_number": turn_count + 1,
                    "user_transcription": user_text,
                    "user_emotion": user_emotion,
                    "emotion_confidence": emotion_confidence,
                    "stress_before": stress_level,
                    "stress_after": new_stress,
                    "avatar_response": avatar_response,
                    "audio_file": audio_output_filename
                })
            except Exception as e:
                print(f"⚠️ No se pudo guardar turno en BD: {e}")
        
        # 7. Retornar resultados
        print(f"✅ Procesamiento completado: '{user_text}' -> '{avatar_response}'")
        return {
            "transcription": user_text,
            "user_emotion": user_emotion,
            "emotion_confidence": emotion_confidence,
            "stress_level_previous": stress_level,
            "stress_level_new": new_stress,
            "avatar_response_text": avatar_response,
            "audio_url": f"/static/{audio_output_filename}" if audio_output_filename else None,
            "turn_number": turn_count + 1
        }
        
    except HTTPException:
        raise
    except Exception as e:
        print(f"❌ ERROR CRÍTICO en /process-audio: {type(e).__name__}: {e}")
        import traceback
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=f"{type(e).__name__}: {str(e)}")
        
    finally:
        # Limpiar audio de entrada
        if os.path.exists(audio_path):
            os.remove(audio_path)

@router.get("/session/start")
async def start_session(user_id: str = Query(None)):
    """Iniciar nueva sesión de entrenamiento."""
    session_id = str(uuid.uuid4())
    initial_stress = int(os.getenv("STRESS_INITIAL_LEVEL", 7))
    timestamp = datetime.utcnow()
    
    # Persistir en base de datos
    try:
        from services.database import get_db
        db = await get_db()
        await db.create_session({
            "session_id": session_id,
            "user_id": user_id,
            "initial_stress": initial_stress,
            "total_turns": 0
        })
        print(f"✅ Sesión creada en BD: {session_id}")
    except Exception as e:
        print(f"⚠️ No se pudo guardar sesión en BD: {e}")
    
    return {
        "session_id": session_id,
        "initial_stress": initial_stress,
        "timestamp": timestamp.isoformat()
    }


@router.post("/session/{session_id}/end")
async def end_session(session_id: str, result: str = "abandoned", final_stress: int = 5):
    """Finaliza una sesión de entrenamiento."""
    try:
        from services.database import get_db
        db = await get_db()
        success = await db.end_session(session_id, result, final_stress)
        
        if success:
            return {"status": "ok", "message": f"Sesión {session_id} finalizada como {result}"}
        else:
            return {"status": "warning", "message": "Sesión no encontrada en BD"}
    except Exception as e:
        return {"status": "error", "message": str(e)}


@router.get("/session/{session_id}")
async def get_session(session_id: str):
    """Obtiene información de una sesión."""
    try:
        from services.database import get_db
        db = await get_db()
        session = await db.get_session(session_id)
        
        if session:
            return session
        else:
            raise HTTPException(status_code=404, detail="Sesión no encontrada")
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.get("/session/{session_id}/turns")
async def get_session_turns(session_id: str):
    """Obtiene los turnos de conversación de una sesión."""
    try:
        from services.database import get_db
        db = await get_db()
        turns = await db.get_session_turns(session_id)
        return {"session_id": session_id, "turns": turns}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.get("/stats/{user_id}")
async def get_user_stats(user_id: str):
    """Obtiene estadísticas de un usuario."""
    try:
        from services.database import get_db
        db = await get_db()
        stats = await db.get_user_stats(user_id)
        return stats
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


class SynthesizeRequest(BaseModel):
    text: str
    stress_level: int = 5


@router.post("/synthesize-text")
async def synthesize_text(request: SynthesizeRequest):
    """
    Sintetizar texto a voz sin necesidad de audio de entrada.
    Útil para el saludo inicial del avatar.
    """
    import uuid
    
    temp_id = str(uuid.uuid4())
    audio_output_filename = f"{temp_id}_tts.mp3"
    audio_output_path = f"temp/{audio_output_filename}"
    os.makedirs("temp", exist_ok=True)
    
    try:
        _, _, _, tts = get_services()
        
        if tts is None:
            raise HTTPException(status_code=503, detail="Servicio TTS no disponible")
        
        try:
            if tts is not None:
                tts.synthesize(
                    text=request.text,
                    stress_level=request.stress_level,
                    output_path=audio_output_path
                )
            else:
                audio_output_filename = None
        except Exception as e:
            print(f"⚠️ Error en TTS (synthesize-text): {e}")
            audio_output_filename = None
        
        return {
            "text": request.text,
            "audio_url": f"/static/{audio_output_filename}" if audio_output_filename else None,
            "stress_level": request.stress_level
        }
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))