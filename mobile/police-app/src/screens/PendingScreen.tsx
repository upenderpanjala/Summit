import React, { useEffect, useState, useCallback } from "react";
import { View, Text, FlatList, Pressable, StyleSheet, RefreshControl, Alert, Linking } from "react-native";
import { PoliceApi, PendingIncident } from "../lib/api";

const NAVY = "#004B8E";
const ORANGE = "#F68026";

/**
 * Station-officer queue: incidents still under the initial check. The officer
 * calls each contact and records confirm/mistake. Higher authorities do NOT see
 * these until the workflow marks them Confirmed.
 */
export default function PendingScreen() {
  const [items, setItems] = useState<PendingIncident[]>([]);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    setRefreshing(true);
    try { setItems(await PoliceApi.pending()); }
    catch (e) { Alert.alert("Load failed", String(e)); }
    finally { setRefreshing(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  async function decide(incidentId: number, contactId: number, decision: string) {
    try {
      const res: any = await PoliceApi.verify(incidentId, contactId, decision);
      Alert.alert("Recorded", `Incident status: ${res.status}`);
      load();
    } catch (e) { Alert.alert("Failed", String(e)); }
  }

  return (
    <FlatList
      style={{ backgroundColor: "#f4f6f9" }}
      contentContainerStyle={{ padding: 12 }}
      data={items}
      keyExtractor={(i) => String(i.id)}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={load} />}
      ListHeaderComponent={<Text style={styles.h1}>Initial-check queue</Text>}
      ListEmptyComponent={<Text style={styles.empty}>No pending checks.</Text>}
      renderItem={({ item }) => (
        <View style={styles.card}>
          <Text style={styles.name}>{item.victimName}</Text>
          <Text style={styles.meta}>{item.victimMobile} · {item.status}</Text>
          {item.latitude != null && (
            <Pressable onPress={() => Linking.openURL(`https://maps.google.com/?q=${item.latitude},${item.longitude}`)}>
              <Text style={styles.loc}>📍 Last location: {item.latitude?.toFixed(4)}, {item.longitude?.toFixed(4)}</Text>
            </Pressable>
          )}
          <Text style={styles.section}>Contacts to verify</Text>
          {item.contacts.map((c) => (
            <View key={c.contactId} style={styles.contactRow}>
              <View style={{ flex: 1 }}>
                <Text style={styles.cName}>{c.name} ({c.relation})</Text>
                <Text style={styles.cPhone} onPress={() => Linking.openURL(`tel:${c.phone}`)}>📞 {c.phone}</Text>
                <Text style={styles.decision}>→ {c.decision}</Text>
              </View>
              <View style={styles.actions}>
                <Pressable style={[styles.btn, { backgroundColor: NAVY }]}
                  onPress={() => decide(item.id, c.contactId, "ConfirmedMissing")}>
                  <Text style={styles.btnText}>Missing</Text>
                </Pressable>
                <Pressable style={[styles.btn, { backgroundColor: "#888" }]}
                  onPress={() => decide(item.id, c.contactId, "DeniedFalseAlarm")}>
                  <Text style={styles.btnText}>Mistake</Text>
                </Pressable>
              </View>
            </View>
          ))}
        </View>
      )}
    />
  );
}

const styles = StyleSheet.create({
  h1: { fontSize: 22, fontWeight: "800", color: NAVY, marginBottom: 8 },
  empty: { color: "#666", textAlign: "center", marginTop: 40 },
  card: { backgroundColor: "#fff", borderRadius: 10, padding: 14, marginBottom: 12, borderTopWidth: 3, borderTopColor: ORANGE },
  name: { fontSize: 18, fontWeight: "700", color: "#111" },
  meta: { color: "#666", marginBottom: 4 },
  loc: { color: NAVY, marginVertical: 4 },
  section: { marginTop: 8, fontWeight: "700", color: "#444" },
  contactRow: { flexDirection: "row", alignItems: "center", borderTopWidth: 1, borderTopColor: "#eee", paddingVertical: 8 },
  cName: { fontWeight: "600" },
  cPhone: { color: NAVY },
  decision: { color: "#888", fontSize: 12 },
  actions: { gap: 6 },
  btn: { borderRadius: 6, paddingVertical: 6, paddingHorizontal: 10, marginBottom: 4 },
  btnText: { color: "#fff", fontWeight: "700", fontSize: 12 },
});
