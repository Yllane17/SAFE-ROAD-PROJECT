# ════════════════════════════════════════════════════════════════════
#  SAFE ROAD — app.py
#  Backend simplifié : attend les vidéos uploadées par l'interface
#  Pas de lecture automatique de caméra ou fichier fixe
# ════════════════════════════════════════════════════════════════════

import os
from flask import Flask, request, jsonify
from flask_cors import CORS
from werkzeug.utils import secure_filename
from ai.video_analyzer import analyser_video
from config import Config

app = Flask(__name__)
CORS(app, origins="*")

UPLOAD_FOLDER      = "uploads"
ALLOWED_EXTENSIONS = {"mp4", "avi", "mov", "mkv"}
os.makedirs(UPLOAD_FOLDER, exist_ok=True)


def extension_autorisee(filename: str) -> bool:
    return "." in filename and \
           filename.rsplit(".", 1)[1].lower() in ALLOWED_EXTENSIONS


# ── GET /status ───────────────────────────────────────────────────────
@app.route("/status")
def status():
    return jsonify({
        "running":      True,
        "model_loaded": True,
        "message":      "SAFE ROAD Backend prêt — En attente d'une vidéo"
    })


# ── POST /upload ──────────────────────────────────────────────────────
@app.route("/upload", methods=["POST"])
def upload():
    """
    Reçoit une vidéo, l'analyse et retourne le résultat directement.
    L'interface n'a pas besoin de WebSocket — elle attend la réponse HTTP.
    """
    if "video" not in request.files:
        return jsonify({"succes": False, "message": "Aucun fichier reçu"}), 400

    fichier = request.files["video"]

    if fichier.filename == "" or not extension_autorisee(fichier.filename):
        return jsonify({
            "succes":  False,
            "message": "Format non supporté. Utilise : mp4, avi, mov, mkv"
        }), 400

    # Sauvegarde temporaire
    filename = secure_filename(fichier.filename)
    filepath = os.path.join(UPLOAD_FOLDER, filename)
    fichier.save(filepath)

    taille_mb = round(os.path.getsize(filepath) / 1024 / 1024, 1)
    print(f"📥 Vidéo reçue : {filename} ({taille_mb} MB)")

    # Analyse synchrone — retourne le résultat quand c'est terminé
    resultat = analyser_video(filepath, filename)

    return jsonify(resultat)


# ── GET /alerts ───────────────────────────────────────────────────────
_historique = []

@app.route("/alerts")
def alerts():
    return jsonify(_historique)


if __name__ == "__main__":
    print("=" * 50)
    print("  SAFE ROAD — Backend démarré")
    print("  En attente de vidéos depuis l'interface...")
    print(f"  URL : http://0.0.0.0:{Config.PORT}")
    print("=" * 50)
    app.run(host="0.0.0.0", port=Config.PORT, debug=False)
