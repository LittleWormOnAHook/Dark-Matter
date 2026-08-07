# Express Mobile Service — Vehicle Inspection App

Android checklist app for **Express Mobile Service** (904-514-2885). Run a quick vehicle inspection, mark each item **Good**, **Bad**, or **Replace**, add notes, and send a **professional PDF or image report** to the customer.

## Features

- Company header with name and phone number
- Simple customer fields: Name, Phone, Vehicle, Mileage
- **19 essential inspection items** in 4 sections
- Per line: tap **Good / Bad / Replace** and optional notes
- Progress bar shows how many items are checked
- **Send as PDF** — formatted document for email
- **Send as Image (JPEG)** — snapshot for text messaging
- **View Saved Reports** — open past drafts and sent PDF/image reports
- Reports auto-save when you send PDF/image, or when you leave the app with customer info entered

## Professional report includes

- Company header with logo, name, phone, and date
- Customer & vehicle info box
- Color-coded status badges (Good / Bad / Replace)
- **Additional Notes** block at the bottom of the form (included on the report)
- Section groupings with alternating row shading
- Summary counts, website, call number, and Google review link in report footer

## Install on your phone

### Quick download (APK) — v1.4 (bold clickable links in PDF reports)

**[Download ExpressMobileInspection.apk](https://github.com/LittleWormOnAHook/Dark-Matter/raw/cursor/express-inspection-pdf-fix-c854/ExpressMobileInspection/releases/ExpressMobileInspection.apk)**

Direct link: `https://github.com/LittleWormOnAHook/Dark-Matter/raw/cursor/express-inspection-pdf-fix-c854/ExpressMobileInspection/releases/ExpressMobileInspection.apk`

If messages only contain plain text and no PDF/JPEG attachment, reinstall this build (v1.1+). Older builds sent text-only reports.

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
