# Express Mobile Service — Appointments & Vehicle Inspection

> **Branch:** `express-mobile-inspection` — this Android app lives on its own branch and is **not** part of the Dark Matter: Genesis Unity game on `main`. Do not merge into `main` unless you intentionally want both in the same tree.

Android app for **Express Mobile Service** (904-514-2885). Schedule customer jobs on a **day / week / month calendar**, then run vehicle inspections and send **PDF or image reports**.

## Features

### Appointments (Samsung-style calendar)

- **Day / Week / Month** — open the **☰ menu** (top left), like Samsung Calendar
- Month and week views: **swipe left/right** to change month or week
- **Customer info** step: dropdowns for **year, make, model, engine size** (1970–2026)
  - **Car / Truck** and **Motorcycle** data from **NHTSA** (US government database)
  - **Jet Ski / PWC** from built-in US watercraft catalog
- **Auto inspection** — saving an appointment creates a linked **inspection file** (customer + vehicle pre-filled)
- Tap **phone** → dial; tap **address** → **Waze**

### Inspection

- Opens with the **latest appointment inspection** (auto-created when you save a calendar job)
- Customer name, phone, vehicle, and mileage pre-filled from the appointment
- **Auto-saves** as you check items and add notes
- **19 essential inspection items** in 4 sections
- Per line: tap **Good / Bad / Replace** and optional notes
- Progress bar shows how many items are checked
- **Send as PDF** — opens **Google Messages** to the customer phone on the inspection sheet (MMS when supported)
- **Send as Image (JPEG)** — same Messages flow with a JPEG snapshot
- **Clear Form** resets everything

### Home screen

- Bottom tabs: **Appointments** | **Inspection**

## Professional report includes

- Company header with logo, name, phone, and date
- Customer & vehicle info box
- Color-coded status badges (Good / Bad / Replace)
- **Additional Notes** block at the bottom of the form (included on the report)
- Section groupings with alternating row shading
- Summary counts and thank-you footer

## Install on your phone

### Shareable install link (v2.2.4) — send this to anyone

**[Download Express Mobile Inspection](https://github.com/LittleWormOnAHook/Dark-Matter/releases/download/express-inspection-v2.2.4/ExpressMobileInspection.apk)**

```
https://github.com/LittleWormOnAHook/Dark-Matter/releases/download/express-inspection-v2.2.4/ExpressMobileInspection.apk
```

All app releases: [GitHub Releases (express-inspection)](https://github.com/LittleWormOnAHook/Dark-Matter/releases?q=express-inspection)

Full install guide + copy/paste text: [`INSTALL.md`](INSTALL.md)

**Keep your data:** Install the new APK **over** the old app (do not uninstall first). Customer jobs and inspections auto-backup to Downloads and restore after reinstall when possible.

1. Open the link on your Android phone.
2. Allow the download.
3. **Uninstall any older Express Mobile Inspection app first** (Settings → Apps) if install fails.
4. Open the downloaded file and tap **Install**.
5. If Android blocks it, allow **Install unknown apps** for your browser or Files app.
6. If Play Protect warns, tap **Install anyway** (this is a sideloaded business app, not from Play Store).

### Install troubleshooting

| Problem | Fix |
|--------|-----|
| **App not installed** | Uninstall the old version, then try again |
| **Can't open file** | Use Chrome or Files app; re-download (v2.0.1 is signed) |
| **Play Protect blocked** | Tap **More details** → **Install anyway** |
| **Download won't start** | Copy the direct link into Chrome on your phone |

### Android Studio

1. Open `ExpressMobileInspection/` in Android Studio.
2. Connect your phone (USB debugging on).
3. Run the app.

### Build APK manually

```bash
cd ExpressMobileInspection
./gradlew assembleDebug
```

APK path: `app/build/outputs/apk/debug/app-debug.apk`

## Usage

### Appointments

1. Open the **Appointments** tab (default).
2. Switch **Day / Week / Month** at the top.
3. Tap **+** to add a customer job with date, time, phone, and address.
4. Tap a **phone number** to dial; tap an **address** to open **Waze**.

### Inspection

1. Open the **Inspection** tab.
2. Enter customer name, phone, vehicle, and mileage.
3. Tap **Good**, **Bad**, or **Replace** on each inspection line.
4. Add optional notes on any item.
5. Tap **Send as PDF** or **Send as Image (JPEG)**.
6. Pick your messaging or email app and send to the customer.

## Requirements

- Android 8.0+ (API 26+)

## Working on this app

```bash
git fetch origin express-mobile-inspection
git checkout express-mobile-inspection
```

Open only `ExpressMobileInspection/` in Android Studio — not the Unity project root.

To return to the game:

```bash
git checkout main
```
