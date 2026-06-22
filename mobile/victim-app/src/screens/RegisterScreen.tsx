import React, { useState } from "react";
import { View, Text, TextInput, Pressable, StyleSheet, ScrollView, Alert } from "react-native";
import {
  useAudioRecorder, RecordingPresets, requestRecordingPermissionsAsync, setAudioModeAsync,
} from "expo-audio"; // expo-av was removed in SDK 55; see mobile/README.md
import { VictimApi } from "../lib/api";
import { requiredError, nameError, textError, mobileError, otpError, normalizeMobile } from "../lib/validate";

const NAVY = "#004B8E";
const ORANGE = "#F68026";
const STATES = ["Telangana", "AndhraPradesh"] as const;

/** Onboarding wizard with inline validation. Steps: details -> OTP to parent ->
 *  contacts (name + phone) -> record the distress phrase. */
export default function RegisterScreen({ onActive }: { onActive: (id: number) => void }) {
  const [step, setStep] = useState(1);
  const [id, setId] = useState<number | null>(null);
  const [busy, setBusy] = useState(false);
  const [errors, setErrors] = useState<Record<string, string | null>>({});

  // step 1
  const [f, setF] = useState({
    fullName: "", mobile: "", address: "", state: "Telangana", district: "",
    guardianName: "", guardianPhone: "",
  });
  const set = (k: string) => (v: string) => setF((s) => ({ ...s, [k]: v }));

  // step 2
  const [otp, setOtp] = useState("");
  // step 3
  const [contacts, setContacts] = useState([
    { name: "", relation: "Mother", phone: "" },
    { name: "", relation: "Sibling", phone: "" },
    { name: "", relation: "Friend", phone: "" },
  ]);
  // step 4
  const [samples, setSamples] = useState(0);
  const recorder = useAudioRecorder(RecordingPresets.HIGH_QUALITY);
  const [recording, setRecording] = useState(false);

  function check(map: Record<string, string | null>) {
    setErrors(map);
    return Object.values(map).every((e) => !e);
  }

  async function submitDetails() {
    const map = {
      fullName: nameError(f.fullName), mobile: mobileError(f.mobile),
      address: textError(f.address, 5, 200), district: requiredError(f.district),
      guardianName: nameError(f.guardianName), guardianPhone: mobileError(f.guardianPhone),
    };
    if (!check(map)) return;
    setBusy(true);
    try {
      const p = await VictimApi.register({
        fullName: f.fullName, mobile: normalizeMobile(f.mobile), address: f.address,
        state: f.state, district: f.district,
        guardianName: f.guardianName, guardianPhone: normalizeMobile(f.guardianPhone),
      });
      setId(p.id);
      await VictimApi.sendOtp(p.id);
      setStep(2);
    } catch (e) { Alert.alert("Could not register", String(e)); }
    finally { setBusy(false); }
  }

  async function verifyOtp() {
    if (!check({ otp: otpError(otp) }) || !id) return;
    setBusy(true);
    try { await VictimApi.verifyOtp(id, otp.trim()); setStep(3); }
    catch (e) { Alert.alert("Verification failed", String(e)); }
    finally { setBusy(false); }
  }

  async function resendOtp() {
    if (!id) return;
    try { await VictimApi.sendOtp(id); Alert.alert("Sent", "A new OTP was sent."); }
    catch (e) { Alert.alert("Please wait", String(e)); }
  }

  async function submitContacts() {
    const map: Record<string, string | null> = {};
    contacts.forEach((c, i) => {
      map[`c${i}name`] = nameError(c.name);
      map[`c${i}phone`] = mobileError(c.phone);
    });
    if (!check(map) || !id) return;
    setBusy(true);
    try {
      await VictimApi.addContacts(id, contacts.map((c) => ({ ...c, phone: normalizeMobile(c.phone) })));
      setStep(4);
    } catch (e) { Alert.alert("Could not save", String(e)); }
    finally { setBusy(false); }
  }

  async function recordSample() {
    try {
      if (!recording) {
        const perm = await requestRecordingPermissionsAsync();
        if (!perm.granted) { Alert.alert("Microphone needed", "Please allow microphone access."); return; }
        await setAudioModeAsync({ allowsRecording: true });
        await recorder.prepareToRecordAsync();
        recorder.record();
        setRecording(true);
      } else {
        await recorder.stop();
        setRecording(false);
        const uri = recorder.uri!;
        await VictimApi.enrollVoice(id!, uri, samples + 1);
        const next = samples + 1;
        setSamples(next);
        if (next >= 3) { Alert.alert("All set", "Registration complete."); onActive(id!); }
      }
    } catch (e) { Alert.alert("Recording error", String(e)); }
  }

  return (
    <ScrollView contentContainerStyle={styles.wrap} keyboardShouldPersistTaps="handled">
      <Text style={styles.h1}>Register</Text>
      <Text style={styles.step}>Step {step} of 4</Text>

      {step === 1 && (
        <>
          <Field label="Full name" value={f.fullName} onChangeText={set("fullName")} error={errors.fullName} maxLength={60} />
          <Field label="Mobile" value={f.mobile} onChangeText={set("mobile")} keyboardType="phone-pad" maxLength={10} error={errors.mobile} />
          <Field label="Address" value={f.address} onChangeText={set("address")} error={errors.address} maxLength={200} />
          <Text style={styles.label}>State</Text>
          <View style={styles.row}>
            {STATES.map((s) => (
              <Pressable key={s} onPress={() => set("state")(s)} style={[styles.toggle, f.state === s && styles.toggleOn]}>
                <Text style={[styles.toggleText, f.state === s && styles.toggleTextOn]}>
                  {s === "AndhraPradesh" ? "Andhra Pradesh" : s}
                </Text>
              </Pressable>
            ))}
          </View>
          <Field label="District" value={f.district} onChangeText={set("district")} error={errors.district} />
          <Field label="Parent / guardian name" value={f.guardianName} onChangeText={set("guardianName")} error={errors.guardianName} maxLength={60} />
          <Field label="Parent / guardian phone" value={f.guardianPhone} onChangeText={set("guardianPhone")} keyboardType="phone-pad" maxLength={10} error={errors.guardianPhone} />
          <Primary title="Continue" onPress={submitDetails} busy={busy} />
        </>
      )}

      {step === 2 && (
        <>
          <Text style={styles.note}>We sent a 6-digit OTP to the parent's phone. Enter it to verify.</Text>
          <Field label="OTP" value={otp} onChangeText={setOtp} keyboardType="number-pad" error={errors.otp} maxLength={6} />
          <Primary title="Verify" onPress={verifyOtp} busy={busy} />
          <Pressable onPress={resendOtp}><Text style={styles.linkText}>Resend OTP</Text></Pressable>
        </>
      )}

      {step === 3 && (
        <>
          <Text style={styles.note}>Register 3 trusted people with their phone numbers. The system calls them automatically to verify an alert.</Text>
          {contacts.map((c, i) => (
            <View key={i} style={styles.contact}>
              <Text style={styles.relation}>{c.relation}</Text>
              <Field label="Name" value={c.name} onChangeText={(t) => upd(i, "name", t)} error={errors[`c${i}name`]} maxLength={60} />
              <Field label="Phone" value={c.phone} onChangeText={(t) => upd(i, "phone", t)} keyboardType="phone-pad" maxLength={10} error={errors[`c${i}phone`]} />
            </View>
          ))}
          <Primary title="Save contacts" onPress={submitContacts} busy={busy} />
        </>
      )}

      {step === 4 && (
        <>
          <Text style={styles.note}>Record your distress phrase 3 times ({samples}/3 done).</Text>
          <Pressable style={styles.record} onPress={recordSample}>
            <Text style={styles.recordText}>{recording ? "Stop & save" : "Record sample"}</Text>
          </Pressable>
        </>
      )}
    </ScrollView>
  );

  function upd(i: number, key: string, val: string) {
    setContacts((cs) => cs.map((c, idx) => (idx === i ? { ...c, [key]: val } : c)));
  }
}

function Field({ label, error, ...rest }: any) {
  return (
    <View style={{ width: "100%", marginBottom: 10 }}>
      <Text style={styles.label}>{label}</Text>
      <TextInput {...rest} style={[styles.input, error && styles.inputErr]} placeholderTextColor="#aab" />
      {!!error && <Text style={styles.errText}>{error}</Text>}
    </View>
  );
}
function Primary({ title, onPress, busy }: { title: string; onPress: () => void; busy?: boolean }) {
  return (
    <Pressable style={[styles.primary, busy && { opacity: 0.6 }]} onPress={onPress} disabled={busy}>
      <Text style={styles.primaryText}>{busy ? "Please wait…" : title}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  wrap: { padding: 24, backgroundColor: "#fff", flexGrow: 1 },
  h1: { fontSize: 26, fontWeight: "800", color: NAVY },
  step: { color: ORANGE, fontWeight: "700", marginBottom: 16 },
  note: { color: "#556", marginBottom: 12, lineHeight: 20 },
  label: { fontSize: 12, color: "#667", marginBottom: 4 },
  input: { borderWidth: 1, borderColor: "#ccd5de", borderRadius: 8, padding: 10, fontSize: 14 },
  inputErr: { borderColor: "#d6453d", backgroundColor: "#fff6f5" },
  errText: { color: "#d6453d", fontSize: 12, marginTop: 3 },
  row: { flexDirection: "row", gap: 8, marginBottom: 10 },
  toggle: { flex: 1, borderWidth: 1, borderColor: "#ccd5de", borderRadius: 8, padding: 10, alignItems: "center" },
  toggleOn: { backgroundColor: NAVY, borderColor: NAVY },
  toggleText: { color: "#445", fontSize: 13 },
  toggleTextOn: { color: "#fff", fontWeight: "700" },
  contact: { borderTopWidth: 1, borderTopColor: "#eef1f5", paddingTop: 10, marginBottom: 6 },
  relation: { fontWeight: "700", color: "#445", marginBottom: 4 },
  primary: { backgroundColor: NAVY, borderRadius: 8, padding: 14, alignItems: "center", marginTop: 12 },
  primaryText: { color: "#fff", fontWeight: "700" },
  linkText: { color: NAVY, textAlign: "center", marginTop: 12, textDecorationLine: "underline" },
  record: { backgroundColor: ORANGE, borderRadius: 8, padding: 16, alignItems: "center" },
  recordText: { color: "#fff", fontWeight: "800" },
});
