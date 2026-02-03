"""
Servicio de base de datos MongoDB para persistencia de sesiones.
"""
import os
from datetime import datetime
from typing import Optional, List, Dict, Any
from motor.motor_asyncio import AsyncIOMotorClient
from pydantic import BaseModel, Field
from bson import ObjectId

# Configuración
MONGODB_URL = os.getenv("MONGODB_URL", "mongodb://localhost:27017")
DB_NAME = os.getenv("DB_NAME", "vr_training")


class PyObjectId(str):
    """ObjectId compatible con Pydantic."""
    @classmethod
    def __get_validators__(cls):
        yield cls.validate

    @classmethod
    def validate(cls, v, field=None):
        if not ObjectId.is_valid(v):
            raise ValueError("Invalid ObjectId")
        return str(v)


class SessionModel(BaseModel):
    """Modelo de sesión de entrenamiento."""
    id: Optional[str] = Field(alias="_id", default=None)
    session_id: str
    user_id: Optional[str] = None
    created_at: datetime = Field(default_factory=datetime.utcnow)
    ended_at: Optional[datetime] = None
    initial_stress: int = 7
    final_stress: Optional[int] = None
    total_turns: int = 0
    result: Optional[str] = None  # "success", "failure", "timeout", "abandoned"
    duration_seconds: Optional[float] = None
    
    class Config:
        populate_by_name = True
        json_encoders = {ObjectId: str}


class ConversationTurnModel(BaseModel):
    """Modelo de turno de conversación."""
    id: Optional[str] = Field(alias="_id", default=None)
    session_id: str
    turn_number: int
    timestamp: datetime = Field(default_factory=datetime.utcnow)
    
    # Audio del usuario
    user_transcription: str
    user_emotion: str
    emotion_confidence: float
    
    # Estado
    stress_before: int
    stress_after: int
    
    # Respuesta del avatar
    avatar_response: str
    audio_file: Optional[str] = None
    
    class Config:
        populate_by_name = True
        json_encoders = {ObjectId: str}


class DatabaseService:
    """Servicio singleton para operaciones de base de datos."""
    
    _instance = None
    _client: Optional[AsyncIOMotorClient] = None
    _db = None
    
    def __new__(cls):
        if cls._instance is None:
            cls._instance = super().__new__(cls)
        return cls._instance
    
    async def connect(self):
        """Conecta a MongoDB."""
        if self._client is None:
            try:
                self._client = AsyncIOMotorClient(MONGODB_URL)
                self._db = self._client[DB_NAME]
                # Verificar conexión
                await self._client.admin.command('ping')
                print(f"✅ Conectado a MongoDB: {DB_NAME}")
            except Exception as e:
                print(f"❌ Error conectando a MongoDB: {e}")
                self._client = None
                self._db = None
    
    async def disconnect(self):
        """Desconecta de MongoDB."""
        if self._client:
            self._client.close()
            self._client = None
            self._db = None
    
    @property
    def is_connected(self) -> bool:
        return self._client is not None and self._db is not None
    
    # === Operaciones de Sesión ===
    
    async def create_session(self, session_data: Dict[str, Any]) -> Optional[str]:
        """Crea una nueva sesión."""
        if not self.is_connected:
            await self.connect()
        if not self.is_connected:
            return None
        
        session_data["created_at"] = datetime.utcnow()
        result = await self._db.sessions.insert_one(session_data)
        return str(result.inserted_id)
    
    async def get_session(self, session_id: str) -> Optional[Dict]:
        """Obtiene una sesión por su ID."""
        if not self.is_connected:
            await self.connect()
        if not self.is_connected:
            return None
        
        session = await self._db.sessions.find_one({"session_id": session_id})
        if session:
            session["_id"] = str(session["_id"])
        return session
    
    async def update_session(self, session_id: str, updates: Dict[str, Any]) -> bool:
        """Actualiza una sesión existente."""
        if not self.is_connected:
            await self.connect()
        if not self.is_connected:
            return False
        
        result = await self._db.sessions.update_one(
            {"session_id": session_id},
            {"$set": updates}
        )
        return result.modified_count > 0
    
    async def end_session(self, session_id: str, result: str, final_stress: int) -> bool:
        """Finaliza una sesión."""
        session = await self.get_session(session_id)
        if not session:
            return False
        
        duration = (datetime.utcnow() - session["created_at"]).total_seconds()
        
        return await self.update_session(session_id, {
            "ended_at": datetime.utcnow(),
            "final_stress": final_stress,
            "result": result,
            "duration_seconds": duration
        })
    
    # === Operaciones de Turnos ===
    
    async def save_turn(self, turn_data: Dict[str, Any]) -> Optional[str]:
        """Guarda un turno de conversación."""
        if not self.is_connected:
            await self.connect()
        if not self.is_connected:
            return None
        
        turn_data["timestamp"] = datetime.utcnow()
        result = await self._db.turns.insert_one(turn_data)
        
        # Actualizar contador de turnos en la sesión
        await self._db.sessions.update_one(
            {"session_id": turn_data["session_id"]},
            {"$inc": {"total_turns": 1}}
        )
        
        return str(result.inserted_id)
    
    async def get_session_turns(self, session_id: str) -> List[Dict]:
        """Obtiene todos los turnos de una sesión."""
        if not self.is_connected:
            await self.connect()
        if not self.is_connected:
            return []
        
        cursor = self._db.turns.find({"session_id": session_id}).sort("turn_number", 1)
        turns = await cursor.to_list(length=100)
        
        for turn in turns:
            turn["_id"] = str(turn["_id"])
        
        return turns
    
    # === Estadísticas ===
    
    async def get_user_stats(self, user_id: str) -> Dict[str, Any]:
        """Obtiene estadísticas de un usuario."""
        if not self.is_connected:
            await self.connect()
        if not self.is_connected:
            return {}
        
        pipeline = [
            {"$match": {"user_id": user_id, "ended_at": {"$ne": None}}},
            {"$group": {
                "_id": "$user_id",
                "total_sessions": {"$sum": 1},
                "successes": {"$sum": {"$cond": [{"$eq": ["$result", "success"]}, 1, 0]}},
                "avg_duration": {"$avg": "$duration_seconds"},
                "avg_final_stress": {"$avg": "$final_stress"}
            }}
        ]
        
        cursor = self._db.sessions.aggregate(pipeline)
        results = await cursor.to_list(length=1)
        
        if results:
            stats = results[0]
            stats["success_rate"] = stats["successes"] / stats["total_sessions"] if stats["total_sessions"] > 0 else 0
            return stats
        
        return {"total_sessions": 0, "successes": 0, "success_rate": 0}


# Instancia singleton
db_service = DatabaseService()


async def get_db() -> DatabaseService:
    """Obtiene la instancia del servicio de base de datos."""
    if not db_service.is_connected:
        await db_service.connect()
    return db_service
