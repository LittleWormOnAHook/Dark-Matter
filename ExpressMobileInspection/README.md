# Express Mobile Service — Vehicle Inspection App

Android checklist app for **Express Mobile Service** (904-514-2885). Technicians can run a standard multi-point vehicle inspection, mark each item **Good**, **Bad**, or **Replace**, add keyboard notes per line, and **text or email** the finished report to the customer.

## Features

- Company header with name and phone number
- Customer & vehicle info fields (name, contact, YMM, VIN, mileage, plate, technician)
- **38 inspection items** across 6 sections based on common industry MPI checklists (Monro/AMRA, Meineke, ASE-style multi-point inspections):
  - Exterior & Visibility
  - Fluids & Filters
  - Under Hood
  - Tires & Wheels
  - Brake System
  - Steering & Suspension
- Per-item **Good / Bad / Replace** touch toggles (tap again to clear)
- Per-item **notes** field (keyboard)
- **Send Report** opens Android share sheet with SMS and email shortcuts
- **Clear Form** resets everything

## Requirements

- Android 8.0+ (API 26+)
- Android Studio Ladybug (2024.2+) or newer recommended
- JDK 17

## Install on your phone

### Quick download (APK)

Download the pre-built APK directly on your Android phone:

**[Download ExpressMobileInspection.apk](https://github.com/LittleWormOnAHook/Dark-Matter/raw/cursor/express-mobile-inspection-bdd8/ExpressMobileInspection/releases/ExpressMobileInspection.apk)**

1. Open the link above on your phone (Chrome works best).
2. When prompted, allow the download.
3. Open the downloaded file and tap **Install**.
4. If Android blocks it, go to **Settings → Security** (or the install prompt) and allow installs from your browser for this one-time install.

### Android Studio
2. Let Gradle sync.
3. Connect your Android phone (USB debugging on) or use an emulator.
4. Run **Run ▶ app** or build an APK:

```bash
cd ExpressMobileInspection
./gradlew assembleDebug
```

The debug APK is at:

`app/build/outputs/apk/debug/app-debug.apk`

Copy that file to your phone and install it, or use `adb install app/build/outputs/apk/debug/app-debug.apk`.

## Usage

1. Fill in customer and vehicle details at the top.
2. Scroll through each section and tap **Good**, **Bad**, or **Replace** for each line.
3. Tap the **Notes** field under any item to type a notation.
4. Tap **Send Report (Text or Email)** when finished.
5. Choose your messaging app, SMS, or email client and pick the customer contact.

## Project structure

```
ExpressMobileInspection/
  app/src/main/java/com/expressmobileservice/inspection/
    MainActivity.kt          — share intent wiring
    InspectionData.kt        — checklist items & models
    ReportFormatter.kt       — plain-text report for SMS/email
    ui/InspectionScreen.kt   — Compose UI
    ui/theme/Theme.kt        — colors
```

## License

Private use for Express Mobile Service.
