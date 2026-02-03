import os
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from dotenv import load_dotenv

# Cargar variables de entorno
load_dotenv()

# Crear directorio temp si no existe
os.makedirs("temp", exist_ok=True)

app = FastAPI(title="VR Clinical Training API", version="1.0.0")

# Configurar CORS para Unity
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # En producción, especificar origins
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Importar e incluir el router de IA
from api.routes import router as api_router
app.include_router(api_router, prefix="/api", tags=["IA"])

# Servir archivos estáticos (audio generado)
app.mount("/static", StaticFiles(directory="temp"), name="static")

@app.get("/")
def read_root():
    return {"status": "API funcionando", "version": "1.0.0"}

@app.get("/health")
def health_check():
    return {"status": "healthy"}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)