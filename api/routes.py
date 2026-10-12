"""
SAFE - Routes API REST
Consommées par les 3 applications (Web, Flutter, C#)
"""

import cv2
import os
import threading
from flask import Response, jsonify, request
from werkzeug.utils import secure_filename

from ai.detector import get_output_frame, get_alerts, get_status
from ai.video_analyzer import analyze_video, get_analysis_state, get_analysis_frame
from config import Config

UPLOAD_FOLDER      = "uploads"
ALLOWED_EXTENSIONS = {"mp4", "avi", "mov", "mkv"}
os.makedirs(UPLOAD_FOLDER, exist_ok=True)


def allowed_file(filename):
    return "." in filename and filename.rsplit(".", 1)[1].lower() in ALLOWED_EXTENSIONS


def generate_stream(frame_getter):
    """Génère le flux MJPEG frame par frame"""
    while True:
        frame, lock = frame_getter()
        if frame is None:
            import time; time.sleep(0.05); continue
        with lock:
            success, encoded = cv2.imencode(".jpg", frame)
            if not success:
                continue
        yield (
            b"--frame\r\n"
            b"Content-Type: image/jpeg\r\n\r\n"
            + bytearray(encoded)
            + b"\r\n"
        )


def register_routes(app, socketio):

    # ── GET /status ───────────────────────────────────────────────────────────
    @app.route("/status")
    def status():
        """Statut général du système SAFE"""
        return jsonify(get_status())

    # ── GET /video_feed ───────────────────────────────────────────────────────
    @app.route("/video_feed")
    def video_feed():
        """Stream vidéo MJPEG — caméra/fichier configuré"""
        return Response(
            generate_stream(get_output_frame),
            mimetype="multipart/x-mixed-replace; boundary=frame"
        )

    # ── GET /analysis_feed ────────────────────────────────────────────────────
    @app.route("/analysis_feed")
    def analysis_feed():
        """Stream MJPEG pendant l'analyse d'une vidéo uploadée"""
        return Response(
            generate_stream(get_analysis_frame),
            mimetype="multipart/x-mixed-replace; boundary=frame"
        )

    # ── POST /upload ──────────────────────────────────────────────────────────
    @app.route("/upload", methods=["POST"])
    def upload_video():
        """
        Reçoit une vidéo uploadée et lance l'analyse IA dans un thread séparé.
        Retourne immédiatement — la progression est envoyée via WebSocket.

        Form-data : { "video": <fichier .mp4/.avi/.mov/.mkv> }
        """
        # Vérification qu'une analyse n'est pas déjà en cours
        state = get_analysis_state()
        if state["status"] == "analyzing":
            return jsonify({
                "error": "Une analyse est déjà en cours. Attends qu'elle se termine."
            }), 409

        # Vérification du fichier
        if "video" not in request.files:
            return jsonify({"error": "Aucun fichier vidéo reçu"}), 400

        file = request.files["video"]
        if file.filename == "":
            return jsonify({"error": "Nom de fichier vide"}), 400

        if not allowed_file(file.filename):
            return jsonify({
                "error": "Format non supporté. Utilise : mp4, avi, mov, mkv"
            }), 400

        # Sauvegarde temporaire
        filename = secure_filename(file.filename)
        filepath = os.path.join(UPLOAD_FOLDER, filename)
        file.save(filepath)
        print(f"📥 Vidéo reçue : {filename} ({os.path.getsize(filepath) // 1024} KB)")

        # Lancement de l'analyse dans un thread séparé
        thread = threading.Thread(
            target=analyze_video,
            args=(filepath, filename, socketio),
            daemon=True
        )
        thread.start()

        return jsonify({
            "success":  True,
            "message":  f"Analyse démarrée pour '{filename}'",
            "filename": filename,
            "stream":   "/analysis_feed",
            "websocket_events": [
                "analysis_progress",  # Progression %
                "accident_found",     # Accident détecté dans la vidéo
                "analysis_done",      # Analyse terminée
                "analysis_error",     # Erreur
            ]
        })

    # ── GET /analysis_status ──────────────────────────────────────────────────
    @app.route("/analysis_status")
    def analysis_status():
        """Retourne l'état actuel de l'analyse en cours"""
        return jsonify(get_analysis_state())

    # ── GET /alerts ───────────────────────────────────────────────────────────
    @app.route("/alerts")
    def alerts():
        """Historique de toutes les alertes détectées"""
        return jsonify(get_alerts())

    # ── GET /config ───────────────────────────────────────────────────────────
    @app.route("/config", methods=["GET"])
    def get_config():
        """Retourne la configuration actuelle"""
        return jsonify({
            "numbers":     Config.EMERGENCY_NUMBERS,
            "threshold":   Config.CONFIDENCE_THRESHOLD,
            "source_type": Config.VIDEO_SOURCE_TYPE,
            "source_path": Config.VIDEO_SOURCE_PATH,
        })

    # ── POST /config ──────────────────────────────────────────────────────────
    @app.route("/config", methods=["POST"])
    def update_config():
        """
        Met à jour la configuration à chaud
        Body JSON : { "numbers": [...], "threshold": 0.93 }
        """
        data = request.get_json()
        if not data:
            return jsonify({"error": "Body JSON requis"}), 400

        if "numbers" in data:
            Config.EMERGENCY_NUMBERS = data["numbers"]
            print(f"📞 Numéros mis à jour : {Config.EMERGENCY_NUMBERS}")

        if "threshold" in data:
            Config.CONFIDENCE_THRESHOLD = float(data["threshold"])
            print(f"🎯 Seuil mis à jour : {Config.CONFIDENCE_THRESHOLD}")

        return jsonify({"success": True, "config": {
            "numbers":   Config.EMERGENCY_NUMBERS,
            "threshold": Config.CONFIDENCE_THRESHOLD,
        }})

    print("✅ Routes API enregistrées :")
    print("   GET  /status")
    print("   GET  /video_feed")
    print("   GET  /analysis_feed")
    print("   POST /upload          ← Upload vidéo pour analyse")
    print("   GET  /analysis_status ← Progression analyse")
    print("   GET  /alerts")
    print("   GET  /config")
    print("   POST /config")
    print("   WS   accident_found   ← Accident détecté dans vidéo")
    print("   WS   analysis_progress← Progression %")
    print("   WS   analysis_done    ← Analyse terminée")
