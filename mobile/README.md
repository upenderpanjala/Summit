# Summit Safety — Mobile apps (Victim · Police · Parent)

Three React Native + Expo (TypeScript) apps that talk to the Summit Safety
backend in `../src/Summit.VMS`. These are complete, readable starters — each is a
real app shell (`App.tsx` + `app.json` + `src/`) you finish scaffolding with Expo.

```
mobile/
├─ victim-app/        # the person at risk
│  ├─ App.tsx                     Register -> SOS
│  ├─ app.json                    mic + location permission strings
│  └─ src/
│     ├─ lib/api.ts               register, OTP verify, contacts, voice, SOS, location
│     └─ screens/
│        ├─ RegisterScreen.tsx    details -> OTP to parent -> contacts(+phones) -> voice
│        └─ SosScreen.tsx         arm hands-free, panic button, GPS
├─ police-app/        # the officer
│  ├─ App.tsx                     login -> queue
│  ├─ app.json
│  └─ src/
│     ├─ lib/api.ts               JWT login, pending (auto-verifying), active cases
│     └─ screens/PendingScreen.tsx
└─ parent-app/        # the concerned parent / contacts
   ├─ App.tsx                     Link -> Monitor
   ├─ app.json
   └─ src/
      ├─ lib/api.ts               register/link, live location, raise request
      └─ screens/
         ├─ LinkScreen.tsx        link with a number the victim nominated
         └─ MonitorScreen.tsx     live location, on/off route, raise request, status
```

## Run any one app (Windows / macOS / Linux)
Each app uses simple `useState` screen switching — no navigation library — so the
only native modules are `expo-audio` + `expo-location` (victim app only).

```bash
# 1) scaffold a current-SDK Expo project (gets a correct package.json)
npx create-expo-app@latest victim-app -t expo-template-blank-typescript
cd victim-app

# 2) add the native modules this app uses (victim app only)
npx expo install expo-audio expo-location

# 3) copy this repo's mobile/victim-app/App.tsx, app.json and src/ over the new project
#    (police-app and parent-app need no extra modules — skip step 2 for them)

# 4) point the app at your backend, then start
#    edit src/lib/api.ts -> API_BASE
npx expo start            # scan the QR with Expo Go, or press a/i for emulator
```

`API_BASE` per target: Android emulator `http://10.0.2.2:5000`, iOS simulator
`http://localhost:5000`, real device your PC's LAN IP e.g. `http://192.168.1.20:5000`.
For local testing run the backend over HTTP so a real phone trusts it.

## Demo path (with the backend running)
1. **Victim**: register -> OTP sent to parent (shown in dev) -> add 3 contacts with
   phones -> record the phrase -> arm hands-free or press SOS.
2. The **system auto-calls** the contacts (mock IVR) and confirms — no officer dials.
3. **Police**: sign in (`investigator@summit.gov` / `Invest#12345`) -> the case
   appears under **Active cases** already verified, with last location -> acknowledge.
4. **Parent**: link with the victim's mobile + a nominated number -> watch her live
   location and route status -> raise a request (all concerned -> escalates to police).

## Build an installable Android APK (no Android Studio needed)
Mobile apps are not Windows `.exe` files — Android ships as `.apk`/`.aab`, iOS as
`.ipa` (iOS can only be built on macOS). The cloud route:

```bash
npm install -g eas-cli
eas login
eas build -p android --profile preview     # returns a downloadable .apk
```

## Honest scope
- Aadhaar was removed; identity is a **Summit token** (mock issuer) verified by an
  **OTP to the parent's phone** (mock SMS). Swap each mock for a real provider
  behind the same interface.
- No voice-identity recognition — the phrase only triggers; contacts verify.
- Onboarding/parent endpoints are anonymous in the prototype; add OTP + JWT before
  real use, since live location is the most sensitive data here.
- Hands-free listening with the app closed is feasible on Android (foreground
  service + on-device wake word); on iPhone use a Siri shortcut / Emergency SOS.
