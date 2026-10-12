"""
SAFE - Modèle CNN de détection de collision
Architecture identique à ton projet original (main1.py)
Tu n'as rien à modifier ici - copie juste car_accident_model.weights.h5 dans ce dossier
"""

import cv2
import numpy as np
from keras.models import Sequential
from keras.layers import Conv2D, BatchNormalization, MaxPooling2D, Flatten, Dense, Dropout
from config import Config

CLASS_NAMES = ['Accident', 'Not-Accident']

def build_cnn():
    """Architecture CNN identique à ton projet original"""
    input_shape = (128, 128, 1)
    model = Sequential()

    model.add(Conv2D(32, kernel_size=(3,3), activation='relu', padding='Same', input_shape=input_shape))
    model.add(BatchNormalization())
    model.add(Conv2D(32, kernel_size=(3,3), activation='relu', padding='Same'))
    model.add(BatchNormalization())
    model.add(MaxPooling2D(pool_size=(2,2)))

    model.add(Conv2D(64, (3,3), activation='relu', padding='Same'))
    model.add(BatchNormalization())
    model.add(Conv2D(64, (3,3), activation='relu', padding='Same'))
    model.add(BatchNormalization())
    model.add(MaxPooling2D(pool_size=(2,2)))

    model.add(Conv2D(128, kernel_size=(3,3), activation='relu', padding='Same'))
    model.add(BatchNormalization())
    model.add(MaxPooling2D(pool_size=(2,2)))
    model.add(Conv2D(128, (3,3), activation='relu', padding='Same'))
    model.add(BatchNormalization())
    model.add(MaxPooling2D(pool_size=(2,2)))

    model.add(Flatten())
    model.add(Dense(128, activation='relu'))
    model.add(Dropout(0.2))
    model.add(Dense(64, activation='relu'))
    model.add(Dropout(0.5))
    model.add(Dense(2, activation='softmax'))

    return model


def load_model():
    """Charge le modèle avec les poids entraînés"""
    print("⏳ Chargement du modèle IA...")
    model = build_cnn()
    model.load_weights(Config.MODEL_WEIGHTS)
    print("✅ Modèle IA chargé avec succès")
    return model


def predict_frame(model, frame):
    """
    Analyse une frame et retourne (is_accident, confidence)
    Identique à la logique de ton main1.py original
    """
    gray  = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    image = cv2.resize(gray, Config.IMAGE_SIZE)
    data  = image.astype("float") / 255.0
    data  = np.expand_dims(data, axis=0)
    data  = np.expand_dims(data, axis=-1)

    pred       = model.predict(data, verbose=0)[0]
    class_idx  = pred.argmax()
    confidence = float(pred[class_idx])

    is_accident = (class_idx == 0 and confidence >= Config.CONFIDENCE_THRESHOLD)
    return is_accident, round(confidence * 100, 2)


def annotate_frame(frame, is_accident, confidence):
    """Ajoute le texte de résultat sur la frame vidéo"""
    if is_accident:
        label = f"ACCIDENT  {confidence}%"
        color = (0, 0, 255)   # Rouge
        cv2.rectangle(frame, (0, 0), (340, 45), (0, 0, 0), -1)
        cv2.putText(frame, label, (5, 32), cv2.FONT_HERSHEY_DUPLEX, 1, color, 2)
    else:
        label = f"Normal  {confidence}%"
        color = (0, 200, 0)   # Vert
        cv2.rectangle(frame, (0, 0), (280, 45), (0, 0, 0), -1)
        cv2.putText(frame, label, (5, 32), cv2.FONT_HERSHEY_DUPLEX, 1, color, 2)
    return frame
