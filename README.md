# SAFE Backend — Guide de démarrage

## Structure des fichiers

```
safe_backend/
├── app.py                          ← Point d'entrée (lance tout)
├── config.py                       ← ⚠️  À CONFIGURER EN PREMIER
├── requirements.txt
├── uploads/                        ← Vidéos uploadées (créé automatiquement)
├── ai/
│   ├── model.py                    ← Modèle CNN (architecture originale)
│   ├── detector.py                 ← Boucle détection caméra/fichier fixe
│   ├── video_analyzer.py           ← ⭐ Analyse vidéo uploadée
│   └── car_accident_model.weights.h5  ← ⚠️  COPIE TON FICHIER ICI
├── api/
│   └── routes.py                   ← Tous les endpoints REST + upload
└── notifications/
    ├── sms.py                      ← Envoi SMS Africa's Talking
    └── mqtt.py                     ← Publication MQTT
```

## Étape 1 — Copier ton fichier de poids IA

```
Copie  car_accident_model.weights.h5  →  dans le dossier  ai/
```

## Étape 2 — Configurer config.py

```python
AT_USERNAME = "sandbox"           # Ton username Africa's Talking
AT_API_KEY  = "METS_TA_CLE_ICI"  # Ta clé API Sandbox

EMERGENCY_NUMBERS = [
    "+237XXXXXXXXX",  # Numéro Police
    "+237XXXXXXXXX",  # Numéro SAMU
]
```

## Étape 3 — Installer les dépendances

```bash
pip install -r requirements.txt
```

## Étape 4 — Lancer le backend

```bash
python app.py
```

## API complète

| Méthode | Route              | Description                              |
|---------|--------------------|------------------------------------------|
| GET     | /status            | Statut du système                        |
| GET     | /video_feed        | Stream MJPEG (caméra/fichier configuré)  |
| GET     | /analysis_feed     | Stream MJPEG pendant l'analyse upload    |
| POST    | /upload            | ⭐ Uploader une vidéo pour analyse       |
| GET     | /analysis_status   | Progression de l'analyse en cours        |
| GET     | /alerts            | Historique des alertes                   |
| GET     | /config            | Configuration actuelle                   |
| POST    | /config            | Modifier la config                       |

## WebSocket — Événements temps réel

| Événement          | Quand                              | Données                              |
|--------------------|------------------------------------|--------------------------------------|
| accident_found     | Accident détecté dans la vidéo     | { frame, time_fmt, confidence }      |
| analysis_progress  | Progression de l'analyse           | { progress, frame, total, accidents }|
| analysis_done      | Analyse terminée                   | { accidents, sms_sent }              |
| analysis_error     | Erreur pendant l'analyse           | { message }                          |
| accident           | Accident sur caméra live           | { id, timestamp, confidence }        |

## Comment tester l'upload (sans application)

```bash
curl -X POST http://localhost:5000/upload \
  -F "video=@/chemin/vers/ta/video.mp4"
```
