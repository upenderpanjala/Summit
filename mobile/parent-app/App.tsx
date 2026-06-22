import React, { useState } from "react";
import { SafeAreaView, StatusBar, View, Text, StyleSheet } from "react-native";
import LinkScreen from "./src/screens/LinkScreen";
import MonitorScreen from "./src/screens/MonitorScreen";
import { ParentLink } from "./src/lib/api";

const NAVY = "#004B8E";

export default function App() {
  const [link, setLink] = useState<ParentLink | null>(null);
  const [phone, setPhone] = useState("");

  return (
    <SafeAreaView style={styles.root}>
      <StatusBar barStyle="light-content" backgroundColor={NAVY} />
      <View style={styles.bar}>
        <Text style={styles.brand}>Summit<Text style={styles.accent}> Safety</Text> · Parent</Text>
      </View>
      {!link ? (
        <LinkScreen onLinked={(l, p) => { setLink(l); setPhone(p); }} />
      ) : (
        <MonitorScreen link={link} phone={phone} />
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
