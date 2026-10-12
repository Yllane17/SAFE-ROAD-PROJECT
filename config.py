"""
SAFE - Configuration centrale du système
Modifie ce fichier selon ton environnement
"""
import email 

class Config:
    # ─── SERVEUR ──────────────────────────────────────────────
    PORT = 5000

    # ─── AFRIQUE'S TALKING (SMS) ───────────────────────────────
    # 1. Crée ton compte sur https://africastalking.com
    # 2. En mode Sandbox : username = "sandbox", API_KEY = ta clé sandbox
    # 3. En mode Production : mets ton vrai username et clé API
    AT_USERNAME = "sandbox"          # Remplace par ton username
    AT_API_KEY  = "atsk_48909c3be846c0186e9b0e5bb11b3353dfa233eb19a4ff860b32908e38dd9dfd43d3d1af" # Remplace par ta clé API Sandbox

    # ─── NUMÉROS D'URGENCE (Cameroun) ─────────────────────────
    # En mode Sandbox, mets le numéro enregistré dans ton dashboard AT
    EMERGENCY_NUMBERS = [
        "+237652997019",   # Numéro Police / Gendarmerie
        "+237692016172",   # Numéro SAMU / Pompiers
    ]

    # ─── MODÈLE IA ────────────────────────────────────────────
    MODEL_WEIGHTS = "ai/car_accident_model.weights.h5"
    CONFIDENCE_THRESHOLD = 0.93   # Seuil de détection (93%)
    IMAGE_SIZE = (128, 128)

    # ─── SOURCE VIDÉO ─────────────────────────────────────────
    # "camera" → webcam locale
    # "file"   → fichier vidéo .mp4
    VIDEO_SOURCE_TYPE = "file"
    VIDEO_SOURCE_PATH = "ai/test_video.mp4"  # Remplace par ton fichier vidéo

    # ─── MQTT (optionnel) ─────────────────────────────────────
    MQTT_BROKER  = "broker.hivemq.com"
    MQTT_TOPIC   = "CAR-AT"

    # ─── EMAIL ALERTE (GMAIL) ─────────────────────────────────────
    EMAIL_EXPEDITEUR = "yllanedave43@gmail.com"
    EMAIL_MOT_DE_PASSE = "ldps gjqt qgfl erpz"
    EMAIL_DESTINATAIRES = [
        "yllanedave43@gmail.com",
        "jinnlay67@gmail.com",
        "daniellelynnselle@gmail.com",
        "falmata.dev@gmail.com",
        "saqido07@gmail.com",
    ]
    