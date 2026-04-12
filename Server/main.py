# SilverCare AI Server
# FastAPI + Edge TTS (무료) + Whisper 로컬 (무료)
# 실행: cd Server && uvicorn main:app --host 0.0.0.0 --port 8000

import io
import tempfile
import asyncio
from pathlib import Path

from fastapi import FastAPI, UploadFile, File
from fastapi.responses import StreamingResponse
from pydantic import BaseModel

app = FastAPI(title="SilverCare AI Server")

# ── TTS ──────────────────────────────────────────────

class TTSRequest(BaseModel):
    text: str
    voice: str = "ko-KR-SunHiNeural"  # 한국어 여성 (노인 친화적, 또렷한 발음)
    # 다른 한국어 음성:
    # ko-KR-InJoonNeural  (남성)
    # ko-KR-HyunsuNeural  (남성)

@app.post("/tts/speak")
async def tts_speak(req: TTSRequest):
    """텍스트 → MP3 음성 변환. Unity TTSManager에서 호출."""
    import edge_tts

    communicate = edge_tts.Communicate(req.text, req.voice, rate="-10%")  # 약간 느리게 (노인 대상)

    buffer = io.BytesIO()
    async for chunk in communicate.stream():
        if chunk["type"] == "audio":
            buffer.write(chunk["data"])

    buffer.seek(0)
    return StreamingResponse(buffer, media_type="audio/mpeg")


# ── STT ──────────────────────────────────────────────

# Whisper 모델 (첫 요청 시 로딩, 이후 캐시)
_whisper_model = None

def get_whisper_model():
    global _whisper_model
    if _whisper_model is None:
        import whisper
        # base: 균형잡힌 속도/정확도 (약 140MB)
        # small: 더 정확하지만 느림 (약 460MB)
        # tiny: 가장 빠르지만 정확도 낮음 (약 70MB)
        _whisper_model = whisper.load_model("base")
        print("[STT] Whisper 'base' 모델 로딩 완료")
    return _whisper_model

@app.post("/stt/recognize")
async def stt_recognize(file: UploadFile = File(...)):
    """음성 파일 → 텍스트 변환. 노래맞추기/고스톱 음성입력용."""
    model = get_whisper_model()

    # 임시 파일에 저장 후 Whisper로 처리
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as tmp:
        content = await file.read()
        tmp.write(content)
        tmp_path = tmp.name

    try:
        result = model.transcribe(tmp_path, language="ko")
        text = result["text"].strip()
    finally:
        Path(tmp_path).unlink(missing_ok=True)

    return {"text": text}


# ── Health Check ─────────────────────────────────────

@app.get("/health")
async def health():
    return {"status": "ok", "tts": "edge-tts", "stt": "whisper-local"}


# ── 서버 직접 실행 ────────────────────────────────────

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
