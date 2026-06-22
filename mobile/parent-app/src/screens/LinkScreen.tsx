import React, { useState } from "react";
import { View, Text, TextInput, Pressable, StyleSheet, Alert, ScrollView } from "react-native";
import { ParentApi, ContactRelation, ParentLink } from "../lib/api";
import { nameError, mobileError, normalizeMobile } from "../lib/validate";

const NAVY = "#004B8E";
const ORANGE = "#F68026";
const RELATIONS: ContactRelation[] = ["Mother", "Father", "Guardian", "Sibling", "Friend"];

/**
 * Link the parent app to the victim. Registration succeeds only if the phone
 * the parent enters is one the victim nominated (guardian or a listed contact);
 * otherwise the backend returns approved=false (pending approval).
 */
export default function LinkScreen({ onLinked }: { onLinked: (link: ParentLink, phone: string) => void }) {
  const [victimMobile, setVictimMobile] = useState("");
  const [name, setName] = useState("");
  const [relation, setRelation] = useState<ContactRelation>("Mother");
  const [phone, setPhone] = useState("");
  const [busy, setBusy] = useState(false);
  const [errors, setErrors] = useState<Record<string, string | null>>({});

  async function link() {
    const map = {
      victimMobile: mobileError(victimMobile), name: nameError(name), phone: mobileError(phone),
    };
    setErrors(map);
    if (!Object.values(map).every((e) => !e)) return;

    setBusy(true);
    try {
      const res = await ParentApi.register({
        victimMobile: normalizeMobile(victimMobile), name, relation, phone: normalizeMobile(phone),
      });
      Alert.alert(res.approved ? "Linked" : "Submitted", res.message);
      onLinked(res, normalizeMobile(phone));
    } catch (e) {
      Alert.alert("Could not link", String(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <ScrollView contentContainerStyle={styles.wrap} keyboardShouldPersistTaps="handled">
      <Text style={styles.h1}>Link to your daughter</Text>
      <Text style={styles.muted}>
        Register after she has registered. Use a number she nominated (her guardian or
        contact number) so only trusted people can follow her.
      </Text>

      <Field label="Her mobile" value={victimMobile} onChangeText={setVictimMobile}
        keyboardType="phone-pad" maxLength={10} placeholder="e.g. 98765 43210" error={errors.victimMobile} />

      <Field label="Your name" value={name} onChangeText={setName} placeholder="e.g. Ravi Sharma" error={errors.name} maxLength={60} />

      <Text style={styles.label}>Relation</Text>
      <View style={styles.chips}>
        {RELATIONS.map((r) => (
          <Pressable key={r} onPress={() => setRelation(r)}
            style={[styles.chip, relation === r && styles.chipOn]}>
            <Text style={[styles.chipText, relation === r && styles.chipTextOn]}>{r}</Text>
          </Pressable>
        ))}
      </View>

      <Field label="Your phone (a number she nominated)" value={phone} onChangeText={setPhone}
        keyboardType="phone-pad" maxLength={10} placeholder="e.g. 99887 76655" error={errors.phone} />

      <Pressable style={[styles.btn, busy && { opacity: 0.6 }]} onPress={link} disabled={busy}>
        <Text style={styles.btnText}>{busy ? "Linking…" : "Link & sign in"}</Text>
      </Pressable>
    </ScrollView>
  );
}

function Field({ label, error, ...rest }: any) {
  return (
    <View style={{ width: "100%", marginBottom: 4 }}>
      <Text style={styles.label}>{label}</Text>
      <TextInput {...rest} style={[styles.input, error && styles.inputErr]} placeholderTextColor="#aab" />
      {!!error && <Text style={styles.errText}>{error}</Text>}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { padding: 24, backgroundColor: "#fff", flexGrow: 1 },
  h1: { fontSize: 26, fontWeight: "800", color: NAVY },
  muted: { color: "#556", marginVertical: 12, lineHeight: 20 },
  label: { fontSize: 12, color: "#667", marginTop: 8, marginBottom: 4 },
  input: { borderWidth: 1, borderColor: "#ccd5de", borderRadius: 8, padding: 11, fontSize: 14 },
  inputErr: { borderColor: "#d6453d", backgroundColor: "#fff6f5" },
  errText: { color: "#d6453d", fontSize: 12, marginTop: 3 },
  chips: { flexDirection: "row", flexWrap: "wrap", gap: 8 },
  chip: { borderWidth: 1, borderColor: "#ccd5de", borderRadius: 16, paddingVertical: 6, paddingHorizontal: 12 },
  chipOn: { backgroundColor: NAVY, borderColor: NAVY },
  chipText: { color: "#445", fontSize: 13 },
  chipTextOn: { color: "#fff", fontWeight: "700" },
  btn: { backgroundColor: NAVY, borderRadius: 9, padding: 14, alignItems: "center", marginTop: 20 },
  btnText: { color: "#fff", fontWeight: "700", fontSize: 15 },
});

