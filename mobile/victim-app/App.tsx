import React, { useState } from "react";
import { SafeAreaView, StatusBar, View, Text, StyleSheet } from "react-native";
import RegisterScreen from "./src/screens/RegisterScreen";
import SosScreen from "./src/screens/SosScreen";

const NAVY = "#004B8E";

export default function App() {
  const [victimId, setVictimId] = useState<number | null>(null);

  return (
    <SafeAreaView style={styles.root}>
      <StatusBar barStyle="light-content" backgroundColor={NAVY} />
      <View style={styles.bar}>
        <Text style={styles.brand}>Summit<Text style={styles.accent}> Safety</Text></Text>
      </View>
      {victimId == null ? (
        <RegisterScreen onActive={(id) => setVictimId(id)} />
      ) : (
        <SosScreen victimId={victimId} />
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: "#fff" },
  bar: { backgroundColor: NAVY, paddingVertical: 14, paddingHorizontal: 16 },
  brand: { color: "#fff", fontSize: 16, fontWeight: "700" },
  accent: { color: "#F68026" },
});
