import React, { useEffect, useState, useCallback, useRef } from "react";
import { View, Text, Pressable, StyleSheet, Alert, Linking, ActivityIndicator } from "react-native";
import { ParentApi, LiveLocation, ParentLink } from "../lib/api";

const NAVY = "#004B8E";
const ORANGE = "#F68026";

const STATUS_LINE: Record<string, string> = {
  UnderVerification: "System is calling the contacts to verify.",
  Confirmed: "Confirmed missing — FIR being drafted.",
  FirRegistered: "FIR filed. Case opened.",
  Escalated: "Escalated. Help is being dispatched.",
  FalseAlarm: "Closed as a false alarm.",
  Cancelled: "Alert was cancelled.",
};

/**
 * Live view for a linked parent: her location and whether she's on her expected
 * route, a button to raise a request (which escalates to police once enough
 * concerned people raise one), and the live status of any active incident.
 */
export default function MonitorScreen({ link, phone }: { link: ParentLink; phone: string }) {
  const [loc, setLoc] = useState<LiveLocation | null>(null);
  const [loading, setLoading] = useState(true);
  const timer = useRef<ReturnType<typeof setInterval> | null>(null);

  const load = useCallback(async () => {
    try {
      setLoc(await ParentApi.location(link.victimProfileId));
    } catch (e) {
      // keep last value; surface only on first load
      if (!loc) Alert.alert("Could not load", String(e));
    } finally {
      setLoading(false);
    }
  }, [link.victimProfileId, loc]);

  useEffect(() => {
    load();
    timer.current = setInterval(load, 5000); // poll every 5s (use push in production)
    return () => { if (timer.current) clearInterval(timer.current); };
  }, [load]);

  async function raise() {
    try {
      const res = await ParentApi.concern(link.victimProfileId, {
        phone, name: link.victimName, reason: loc?.onRoute ? "Unreachable" : "Off expected route",
      });
      Alert.alert(
        res.escalatedToPolice ? "Escalated to police" : "Request recorded",
        res.escalatedToPolice
          ? `All ${res.threshold} concerned people raised a request. Incident #${res.incidentId} sent to police.`
          : `${res.concernCount} of ${res.threshold} concerned people have raised a request.`
      );
      load();
    } catch (e) {
      Alert.alert("Could not raise", String(e));
    }
  }

  const onRoute = loc?.onRoute ?? true;
  const hasLoc = loc?.lat != null && loc?.lng != null;

  return (
    <View style={styles.wrap}>
      <Text style={styles.who}>Linked to {link.victimName}{link.approved ? "" : " (pending approval)"}</Text>

      <Text style={styles.h2}>Live location</Text>
      <View style={[styles.card, { backgroundColor: onRoute ? "#f7fbff" : "#fff5f5", borderColor: onRoute ? "#cfe0f3" : "#f3c9c9" }]}>
        {loading && !loc ? <ActivityIndicator color={NAVY} /> : hasLoc ? (
          <>
            <Pressable onPress={() => Linking.openURL(`https://maps.google.com/?q=${loc!.lat},${loc!.lng}`)}>
              <Text style={styles.loc}>📍 {loc!.lat!.toFixed(4)}, {loc!.lng!.toFixed(4)}  (open map)</Text>
            </Pressable>
            <View style={[styles.pill, onRoute ? styles.pillOk : styles.pillWarn]}>
              <Text style={[styles.pillText, onRoute ? styles.pillTextOk : styles.pillTextWarn]}>
                {onRoute ? "On route" : "Off route — deviation detected"}
              </Text>
            </View>
            {!onRoute && <Text style={styles.muted}>She has left her expected route. If you can't reach her, raise a request.</Text>}
          </>
        ) : <Text style={styles.muted}>No location shared yet (she must opt in to location sharing).</Text>}
      </View>

      <Text style={styles.h2}>Raise a request</Text>
      <Text style={styles.muted}>
        {loc ? `${loc.concernCount} of ${loc.concernThreshold} concerned people have raised a request. ` : ""}
        When all do, it goes to the police automatically.
      </Text>
      <Pressable style={styles.btnOrange} onPress={raise}>
        <Text style={styles.btnText}>Raise a request</Text>
      </Pressable>

      <Text style={styles.h2}>What's happening</Text>
      {loc?.incidentStatus ? (
        <View style={styles.card}>
          <Text style={styles.status}>{loc.incidentStatus}</Text>
          <Text style={styles.muted}>{STATUS_LINE[loc.incidentStatus] || "In progress."}</Text>
        </View>
      ) : (
        <Text style={styles.muted}>No active alert. You'll see updates here.</Text>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { flex: 1, padding: 20, backgroundColor: "#fff" },
  who: { color: "#556", marginBottom: 10 },
  h2: { fontSize: 16, fontWeight: "800", color: NAVY, marginTop: 16, marginBottom: 6 },
  card: { borderWidth: 1, borderColor: "#e3e8ee", borderRadius: 12, padding: 14 },
  loc: { color: NAVY, fontSize: 14 },
  muted: { color: "#667", fontSize: 13, marginTop: 8, lineHeight: 19 },
  pill: { alignSelf: "flex-start", borderRadius: 12, paddingVertical: 3, paddingHorizontal: 10, marginTop: 8 },
  pillOk: { backgroundColor: "#e6f6ec" }, pillWarn: { backgroundColor: "#fff3e0" },
  pillText: { fontSize: 12, fontWeight: "700" },
  pillTextOk: { color: "#1b7a3d" }, pillTextWarn: { color: "#b25c00" },
  btnOrange: { backgroundColor: ORANGE, borderRadius: 9, padding: 13, alignItems: "center" },
  btnText: { color: "#fff", fontWeight: "800", fontSize: 14 },
  status: { fontSize: 15, fontWeight: "700", color: NAVY },
});
