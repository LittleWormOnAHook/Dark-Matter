# Express Mobile Service — Vehicle Inspection App

> **Branch:** `express-mobile-inspection` — this Android app lives on its own branch and is **not** part of the Dark Matter: Genesis Unity game on `main`. Do not merge into `main` unless you intentionally want both in the same tree.

Android checklist app for **Express Mobile Service** (904-514-2885). Run a quick vehicle inspection, mark each item **Good**, **Bad**, or **Replace**, add notes, and send a **professional PDF or image report** to the customer.

## Features

- Company header with name and phone number
- Simple customer fields: Name, Phone, Vehicle, Mileage
- **19 essential inspection items** in 4 sections
- Per line: tap **Good / Bad / Replace** and optional notes
- Progress bar shows how many items are checked
- **Send as PDF** — formatted document for email
- **Send as Image (JPEG)** — snapshot for text messaging
- **Clear Form** resets everything

## Professional report includes

- Company header with logo, name, phone, and date
- Customer & vehicle info box
- Color-coded status badges (Good / Bad / Replace)
- **Additional Notes** block at the bottom of the form (included on the report)
- Section groupings with alternating row shading
- Summary counts and thank-you footer

## Install on your phone

### Quick download (APK)

**[Download ExpressMobileInspection.apk](https://github.com/LittleWormOnAHook/Dark-Matter/raw/express-mobile-inspection/ExpressMobileInspection/releases/ExpressMobileInspection.apk)**

Direct link: `https://github.com/LittleWormOnAHook/Dark-Matter/raw/express-mobile-inspection/ExpressMobileInspection/releases/ExpressMobileInspection.apk`

1. Open the link on your Android phone.
2. Allow the download.
3. Open the file and tap **Install**.

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

1. Enter customer name, phone, vehicle, and mileage.
2. Tap **Good**, **Bad**, or **Replace** on each inspection line.
3. Add optional notes on any item.
4. Tap **Send as PDF** or **Send as Image (JPEG)**.
5. Pick your messaging or email app and send to the customer.

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
