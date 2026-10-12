"""
SAFE - Envoi SMS via Africa's Talking
Couvre le Cameroun — Mode Sandbox gratuit pour les tests
"""

import africastalking
from config import Config

import smtplib
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart
from config import Config

# Initialisation Africa's Talking
africastalking.initialize(Config.AT_USERNAME, Config.AT_API_KEY)
sms = africastalking.SMS


def send_accident_sms(confidence: float, timestamp: str, nb_accidents: int = 1):
    """
    Envoie un SMS d'urgence aux numéros configurés (Police + SAMU)
    - confidence   : niveau de confiance IA (ex: 97.3)
    - timestamp    : heure de détection
    - nb_accidents : nombre d'accidents détectés dans la vidéo
    """
    if nb_accidents > 1:
        message = (
            f"ALERTE ACCIDENT SAFE\n"
            f"{nb_accidents} collisions detectees !\n"
            f"Confiance max : {confidence}%\n"
            f"Heure : {timestamp}\n"
            f"Intervention requise immediatement."
        )
    else:
        message = (
            f"ALERTE ACCIDENT SAFE\n"
            f"Collision detectee !\n"
            f"Confiance IA : {confidence}%\n"
            f"Heure : {timestamp}\n"
            f"Intervention requise immediatement."
        )

    try:
        response = sms.send(message, Config.EMERGENCY_NUMBERS)
        print(f"✅ SMS envoyés : {response}")
        return True
    except Exception as e:
        print(f"❌ Erreur envoi SMS : {e}")
        return False



def send_accident_sms(confidence: float, timestamp: str,nb_accidents = 1):
    sujet = "ALERTE ACCIDENT SAFE"
    corps = (
        f"ALERTE !! \n"
        f"{'%d collisions détectées !'%nb_accidents if nb_accidents>1 else 'Collision détectée !'}\n"
        f"Confiance IA : {confidence}%\n"
        f"Heure : {timestamp}\n\n"
        f"Intervention requise immediatement."
    )
    
    try:
        print(f"📧 Tentative envoi à {len(Config.EMAIL_DESTINATAIRES)} destinataires...")
        
        with smtplib.SMTP("smtp.gmail.com", 587) as server:
            server.ehlo()
            server.starttls()
            server.ehlo()
            server.login(Config.EMAIL_EXPEDITEUR, Config.EMAIL_MOT_DE_PASSE)
            print("✅ Connexion Gmail OK")

            for i, destinataire in enumerate(Config.EMAIL_DESTINATAIRES):
                try:
                    print(f"📤 Envoi {i+1}/{len(Config.EMAIL_DESTINATAIRES)} → {destinataire}")
                    
                    msg = MIMEMultipart()
                    msg["From"]    = Config.EMAIL_EXPEDITEUR
                    msg["To"]      = destinataire
                    msg["Subject"] = f"ALERTE : {sujet}"
                    msg.attach(MIMEText(corps, "plain", "utf-8"))

                    server.sendmail(
                        Config.EMAIL_EXPEDITEUR,
                        destinataire,
                        msg.as_string()
                    )
                    print(f"✅ Email {i+1} envoyé à {destinataire}")

                except Exception as e_dest:
                    # ✅ Si 1 destinataire échoue, on continue avec les suivants
                    print(f"❌ Échec pour {destinataire} : {e_dest}")
                    continue

    except Exception as e:
        print(f"❌ Erreur connexion Gmail : {e}")