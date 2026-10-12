"""
SAFE - Thread de capture vidéo et boucle de détection
Ce fichier gère la capture OpenCV et appelle le modèle IA sur chaque frame
"""

import cv2
import threading
import time
from datetime import datetime

from ai.model import load_model, predict_frame, annotate_frame
from notifications.sms import send_accident_sms
from notifications.mqtt import publish_mqtt
from config import Config

# ─── ÉTAT GLOBAL (partagé avec le stream vidéo) ───────────────────────────────
output_frame = None
frame_lock   = threading.Lock()
alerts_log   = []          # Historique des alertes
system_status = {
    "running": False,
    "model_loaded": False,
    "source": Config.VIDEO_SOURCE_PATH,
    "last_prediction": None,
}


def get_output_frame():
    return output_frame, frame_lock


def get_alerts():
    return alerts_log


def get_status():
    return system_status


def start_detector(socketio):
    """Lance la boucle de détection dans un thread séparé"""
    global output_frame, system_status

    # Chargement du modèle
    model = load_model()
    system_status["model_loaded"] = True
    system_status["running"] = True

    notification_sent = False   # Evite les SMS répétés pour le même accident

    # ── Choix de la source vidéo ──────────────────────────────────────────────
    if Config.VIDEO_SOURCE_TYPE == "camera":
        cap = cv2.VideoCapture(0)
        print("📷 Source vidéo : Webcam")
    else:
        cap = cv2.VideoCapture(Config.VIDEO_SOURCE_PATH)
        print(f"🎬 Source vidéo : {Config.VIDEO_SOURCE_PATH}")

    if not cap.isOpened():
        print("❌ Erreur : impossible d'ouvrir la source vidéo")
        system_status["running"] = False
        return

    print("🔍 Détection démarrée...")

    while True:
        success, frame = cap.read()

        # Si fin de fichier vidéo → recommencer depuis le début
        if not success:
            cap.set(cv2.CAP_PROP_POS_FRAMES, 0)
            notification_sent = False
            continue

        # ── Prédiction IA ────────────────────────────────────────────────────
        is_accident, confidence = predict_frame(model, frame)
        system_status["last_prediction"] = {
            "is_accident": is_accident,
            "confidence": confidence,
            "time": datetime.now().strftime("%H:%M:%S")
        }

        # ── Annotation visuelle de la frame ──────────────────────────────────
        frame = annotate_frame(frame, is_accident, confidence)

        # ── Mise à jour du frame global (pour le stream MJPEG) ───────────────
        with frame_lock:
            output_frame = cv2.resize(frame, (640, 480)).copy()

        # ── Déclenchement des alertes si accident détecté ────────────────────
        if is_accident and not notification_sent:
            timestamp = datetime.now().strftime("%d/%m/%Y %H:%M:%S")

            # Sauvegarde dans l'historique
            alert = {
                "id": len(alerts_log) + 1,
                "timestamp": timestamp,
                "confidence": confidence,
                "source": Config.VIDEO_SOURCE_PATH,
            }
            alerts_log.append(alert)

            # Envoi SMS via Africa's Talking
            send_accident_sms(confidence, timestamp)

            # Notification WebSocket → toutes les apps connectées
            socketio.emit("accident", alert)
            print(f"🚨 ACCIDENT détecté ! Confiance : {confidence}% — Alertes envoyées")

            # Publication MQTT
            publish_mqtt("1")

            notification_sent = True

        elif not is_accident:
            if notification_sent:
                publish_mqtt("0")
            notification_sent = False

        time.sleep(0.03)   # ~30 FPS max

    cap.release()
