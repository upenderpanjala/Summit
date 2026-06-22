import React, { useEffect, useState } from "react";
import { View, Text, Pressable, StyleSheet, Alert, ActivityIndicator } from "react-native";
import {
  useAudioRecorder, RecordingPresets, requestRecordingPermissionsAsync, setAudioModeAsync,
} from "expo-audio"; // expo-av was removed in SDK 55; see mobile/README.md
import * as Location from "expo-location";
import { VictimApi } from "../lib/api";

const NAVY = "#004B8E";
const ORANGE = "#F68026";

/**
 * SOS screen. Press-and-hold to record the distress phrase; on release the audio
 * + last-known location are sent to the backend, which raises a DistressIncident.
 * The system then auto-calls the contacts to verify. A plain panic button is the
 * guaranteed fallback.
 */
export default function SosScreen({ victimId }: { victimId: number }) {
  const recorder = useAudioRecorder(RecordingPresets.HIGH_QUALITY);
  const [recording, setRecording] = useState(false);
  const [busy, setBusy] = useState(false);
  const [incidentId, setIncidentId] = useState<number | null>(null);
  const [status, setStatus] = useState<string>("");

  useEffect(() => {
    (async () => {
      await requestRecordingPermissionsAsync();
      await Location.requestForegroundPermissionsAsync();
    })();
  }, []);

  async function getLocation() {
    try {
      const { status } = await Location.getForegroundPermissionsAsync();
      if (status !== "granted") return undefined;
      const pos = await Location.getCurrentPositionAsync({});
      return { lat: pos.coords.latitude, lng: pos.coords.longitude };
    } catch {
      return undefined;
    }
  }

  async function startRecording() {
    try {
      await setAudioModeAsync({ allowsRecording: true });
      await recorder.prepareToRecordAsync();
      recorder.record();
      setRecording(true);
    } catch (e) {
      Alert.alert("Mic error", String(e));
    }
  }

  async function stopAndSend() {
    if (!recording) return sendSos(null);
    setBusy(true);
    try {
      await recorder.stop();
      setRecording(false);
      await sendSos(recorder.uri ?? null);
    } finally {
      setBusy(false);
    }
  }

  async function sendSos(audioUri: string | null) {
    setBusy(true);
    try {
      const loc = await getLocation();
      const res: any = await VictimApi.sos(victimId, audioUri, loc?.lat, loc?.lng);
      setIncidentId(res.id);
      setStatus(res.status);
      Alert.alert("Alert sent", "The system is calling your contacts to verify.");
    } catch (e) {
      Alert.alert("Could not send", String(e));
    } finally {
      setBusy(false);
    }
  }

  async function cancel() {
    if (!incidentId) return;
    try {
      await VictimApi.cancel(incidentId);
      setStatus("Cancelled");
    } catch (e) {
      Alert.alert("Cannot cancel", "The alert may already be confirmed.");
    }
  }


  return (
    <View style={styles.wrap}>
      <Text style={styles.title}>Emergency</Text>
      <Text style={styles.sub}>Hold to record your distress phrase, or tap the panic button.</Text>

      <Pressable
        onPressIn={startRecording}
        onPressOut={stopAndSend}
        style={({ pressed }) => [styles.sos, pressed && { opacity: 0.85 }]}
      >
        {busy ? <ActivityIndicator color="#fff" /> : <Text style={styles.sosText}>HOLD{"\n"}TO ALERT</Text>}
      </Pressable>

      <Pressable style={styles.panic} onPress={() => sendSos(null)}>
        <Text style={styles.panicText}>PANIC (no voice)</Text>
      </Pressable>

      {!!status && (
        <View style={styles.statusBox}>
          <Text style={styles.statusText}>Status: {status}</Text>
          {(status === "Raised" || status === "UnderVerification") && (
            <Pressable onPress={cancel}>
              <Text style={styles.cancel}>It was a mistake — cancel</Text>
            </Pressable>
          )}
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { flex: 1, alignItems: "center", justifyContent: "center", padding: 24, backgroundColor: "#fff" },
  title: { fontSize: 28, fontWeight: "800", color: NAVY },
  sub: { color: "#555", textAlign: "center", marginVertical: 12 },
  sos: {
    width: 220, height: 220, borderRadius: 110, backgroundColor: ORANGE,
    alignItems: "center", justifyContent: "center", marginTop: 16,
    shadowColor: ORANGE, shadowOpacity: 0.5, shadowRadius: 16, elevation: 8,
  },
  sosText: { color: "#fff", fontSize: 22, fontWeight: "800", textAlign: "center" },
  panic: { marginTop: 24, borderWidth: 2, borderColor: NAVY, borderRadius: 8, paddingVertical: 12, paddingHorizontal: 24 },
  panicText: { color: NAVY, fontWeight: "700" },
  statusBox: { marginTop: 24, alignItems: "center" },
  statusText: { fontSize: 16, fontWeight: "600", color: NAVY },
  cancel: { marginTop: 8, color: "#b00", textDecorationLine: "underline" },
});
