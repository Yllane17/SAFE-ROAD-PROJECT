"""
SAFE - Publication MQTT (HiveMQ)
Conserve la compatibilité avec ton système original
"""

import paho.mqtt.client as mqtt
from config import Config

_client = None

def _get_client():
    global _client
    if _client is None:
        _client = mqtt.Client("SAFE-BACKEND")
        _client.connect(Config.MQTT_BROKER)
    return _client

def publish_mqtt(value: str):
    """Publie l'état sur le topic MQTT (1=accident, 0=normal)"""
    try:
        client = _get_client()
        client.publish(Config.MQTT_TOPIC, value)
    except Exception as e:
        print(f"⚠️ MQTT : {e}")
