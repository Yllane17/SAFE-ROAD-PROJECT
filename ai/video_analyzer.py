# ════════════════════════════════════════════════════════════════════
#  SAFE ROAD — ai/video_analyzer.py  (CORRIGÉ)
#  Envoie Email+SMS exactement 2 fois au total par vidéo
# ════════════════════════════════════════════════════════════════════

import cv2
import os
from datetime import datetime
from ai.model      import load_model, predict_frame
from notifications.sms import send_accident_sms
from config        import Config

_model = None

def get_model():
    global _model
    if _model is None:
        _model = load_model()
    return _model


def analyser_video(filepath: str, filename: str) -> dict:
    model = get_model()

    cap = cv2.VideoCapture(filepath)
    if not cap.isOpened():
        return {
            "succes": False, "message": "Impossible d'ouvrir la video",
            "accidents": [], "nb_accidents": 0, "nb_accidents_valides": 0,
            "nb_fausses_alertes": 0, "sms_envoye": False,
            "nb_alertes_envoyees": 0, "fichier": filename,
        }

    total_frames            = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
    fps                     = cap.get(cv2.CAP_PROP_FPS) or 25
    accidents               = []
    nb_alertes_envoyees     = 0
    derniere_frame_accident = -60

    print(f"\nAnalyse : {filename} ({total_frames} frames)")

    frame_idx = 0
    while True:
        success, frame = cap.read()
        if not success:
            break
        frame_idx += 1
        if frame_idx % 5 != 0:
            continue

        is_accident, confidence = predict_frame(model, frame)

        if is_accident and (frame_idx - derniere_frame_accident) > 60:
            timestamp_sec = round(frame_idx / fps, 1)
            time_fmt      = f"{int(timestamp_sec // 60)}min {int(timestamp_sec % 60)}s"
            detected_at   = datetime.now().strftime("%H:%M:%S")

            accidents.append({
                "frame": frame_idx, "time_sec": timestamp_sec,
                "time_fmt": time_fmt, "confidence": confidence,
                "detected_at": detected_at,
            })
            derniere_frame_accident = frame_idx
            print(f"Accident #{len(accidents)} a {time_fmt} — {confidence}%")

            # Envoie max 2 alertes au total
            if nb_alertes_envoyees < 2:
                if send_accident_sms(confidence, detected_at, 1):
                    nb_alertes_envoyees += 1
                    print(f"Alerte {nb_alertes_envoyees}/2 envoyee")

    cap.release()
    try:
        os.remove(filepath)
    except Exception:
        pass

    valides  = [a for a in accidents if a["confidence"] >= 95]
    fausses  = [a for a in accidents if a["confidence"] < 95]

    print(f"Analyse terminee. En attente de la prochaine video...\n")

    return {
        "succes":               True,
        "fichier":              filename,
        "total_frames":         total_frames,
        "accidents":            accidents,
        "nb_accidents":         len(accidents),
        "nb_accidents_valides": len(valides),
        "nb_fausses_alertes":   len(fausses),
        "sms_envoye":           nb_alertes_envoyees > 0,
        "nb_alertes_envoyees":  nb_alertes_envoyees,
        "message": (
            f"{len(accidents)} accident(s) detecte(s) — {nb_alertes_envoyees} alerte(s) envoyee(s)"
            if accidents else "Aucun accident detecte"
        )
    }
