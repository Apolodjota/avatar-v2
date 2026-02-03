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
    
    if _whisper is None:
        try:
            from services.whisper_stt import get_whisper_service
            _whisper = get_whisper_service()
        except Exception as e:
            print(f"⚠️ Error cargando Whisper: {e}")
            _whisper = None
    
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
    
    if _tts is None:
        try:
            from services.tts_service import TTSService
            _tts = TTSService()
        except Exception as e:
            print(f"⚠️ Error cargando TTSService: {e}")
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
        transcription = whisper.transcribe(audio_path)
        user_text = transcription["text"]
        
        # 2. Clasificar emoción
        if emotion_clf is not None:
            emotion_result = emotion_clf.classify(audio_path)
            user_emotion = emotion_result["emotion"]
            emotion_confidence = emotion_result["confidence"]
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
            avatar_response = llm.generate_response(
                user_input=user_text,
                stress_level=new_stress,
                conversation_history=[],  # TODO: pasar historial real
                turn_count=turn_count + 1
            )
        else:
            avatar_response = "Entiendo lo que me dices... necesito un momento para pensar."
        
        # 5. Sintetizar voz
        audio_output_filename = f"{temp_id}_response.mp3"
        audio_output_path = f"temp/{audio_output_filename}"
        if tts is not None:
            tts.synthesize(
                text=avatar_response,
                stress_level=new_stress,
                output_path=audio_output_path
            )
        else:
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