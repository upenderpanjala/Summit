import React, { useState } from "react";
import { SafeAreaView, StatusBar, View, Text, TextInput, Pressable, StyleSheet, Alert } from "react-native";
import PendingScreen from "./src/screens/PendingScreen";
import { PoliceApi, setToken } from "./src/lib/api";

const NAVY = "#004B8E";

function Login({ onIn }: { onIn: () => void }) {
  const [email, setEmail] = useState("investigator@summit.gov");
  const [password, setPassword] = useState("Invest#12345");
  const [err, setErr] = useState("");
  async function signIn() {
    if (!email.trim() || !password) { setErr("Email and password are required."); return; }
    setErr("");
    try {
      const res = await PoliceApi.login(email.trim(), password);
      setToken(res.accessToken);
      onIn();
    } catch (e) { Alert.alert("Login failed", String(e)); }
  }
  return (
    <View style={styles.pad}>
      <Text style={styles.h1}>Officer sign-in</Text>
      <Text style={styles.label}>Email</Text>
      <TextInput style={styles.input} value={email} onChangeText={setEmail} autoCapitalize="none" />
      <Text style={styles.label}>Password</Text>
      <TextInput style={styles.input} value={password} onChangeText={setPassword} secureTextEntry />
      {!!err && <Text style={styles.err}>{err}</Text>}
      <Pressable style={styles.btn} onPress={signIn}><Text style={styles.btnText}>Sign in</Text></Pressable>
    </View>
  );
}

export default function App() {
  const [authed, setAuthed] = useState(false);
  return (
    <SafeAreaView style={styles.root}>
      <StatusBar barStyle="light-content" backgroundColor={NAVY} />
      <View style={styles.bar}><Text style={styles.brand}>Summit<Text style={styles.accent}> Safety</Text> · Police</Text></View>
      {authed ? <PendingScreen /> : <Login onIn={() => setAuthed(true)} />}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: "#fff" },
  bar: { backgroundColor: NAVY, paddingVertical: 14, paddingHorizontal: 16 },
  brand: { color: "#fff", fontSize: 16, fontWeight: "700" },
  accent: { color: "#F68026" },
  pad: { padding: 24 },
  h1: { fontSize: 22, fontWeight: "800", color: NAVY, marginBottom: 12 },
  label: { fontSize: 12, color: "#667", marginTop: 12, marginBottom: 4 },
  input: { borderWidth: 1, borderColor: "#ccd5de", borderRadius: 8, padding: 11 },
  btn: { backgroundColor: NAVY, borderRadius: 9, padding: 14, alignItems: "center", marginTop: 18 },
  btnText: { color: "#fff", fontWeight: "700" },
  err: { color: "#d6453d", fontSize: 13, marginTop: 10 },
});
