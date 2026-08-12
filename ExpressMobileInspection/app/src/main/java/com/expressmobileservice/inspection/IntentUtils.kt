package com.expressmobileservice.inspection

import android.content.Context
import android.content.Intent
import android.net.Uri
import android.provider.Telephony
import android.widget.Toast

fun dialPhone(context: Context, phone: String) {
    val digits = normalizePhoneDigits(phone)
    if (digits.isBlank()) {
        Toast.makeText(context, "No phone number on this appointment.", Toast.LENGTH_SHORT).show()
        return
    }
    context.startActivity(Intent(Intent.ACTION_DIAL, Uri.parse("tel:$digits")))
}

fun normalizePhoneDigits(phone: String): String =
    phone.filter { it.isDigit() || it == '+' }

/**
 * Opens the default SMS/MMS app with the PDF attached and customer number pre-filled.
 * If no customer phone is on the form, opens the phone dialer instead.
 */
fun shareInspectionPdfToCustomer(
    context: Context,
    pdfUri: Uri,
    customerPhone: String,
    subject: String,
    message: String
) {
    val digits = normalizePhoneDigits(customerPhone)
    if (digits.isBlank()) {
        context.startActivity(Intent(Intent.ACTION_DIAL))
        return
    }

    val sendIntent = Intent(Intent.ACTION_SEND).apply {
        type = "application/pdf"
        putExtra(Intent.EXTRA_STREAM, pdfUri)
        putExtra(Intent.EXTRA_TEXT, message)
        putExtra(Intent.EXTRA_SUBJECT, subject)
        putExtra("address", digits)
        putExtra("sms_body", message)
        addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
    }

    val smsPackage = Telephony.Sms.getDefaultSmsPackage(context)
    if (smsPackage != null) {
        sendIntent.setPackage(smsPackage)
        if (sendIntent.resolveActivity(context.packageManager) != null) {
            context.startActivity(sendIntent)
            return
        }
        sendIntent.setPackage(null)
    }

    val sendToIntent = Intent(Intent.ACTION_SENDTO, Uri.parse("smsto:$digits")).apply {
        putExtra("sms_body", message)
        putExtra(Intent.EXTRA_TEXT, message)
        putExtra(Intent.EXTRA_STREAM, pdfUri)
        addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
    }
    if (sendToIntent.resolveActivity(context.packageManager) != null) {
        context.startActivity(sendToIntent)
        return
    }

    if (sendIntent.resolveActivity(context.packageManager) != null) {
        context.startActivity(sendIntent)
    } else {
        dialPhone(context, digits)
    }
}

fun messagePhone(context: Context, phone: String, body: String = "") {
    val digits = normalizePhoneDigits(phone)
    if (digits.isBlank()) {
        Toast.makeText(context, "No phone number on this appointment.", Toast.LENGTH_SHORT).show()
        return
    }
    val intent = Intent(Intent.ACTION_SENDTO, Uri.parse("smsto:$digits")).apply {
        if (body.isNotBlank()) {
            putExtra("sms_body", body)
            putExtra(Intent.EXTRA_TEXT, body)
        }
    }
    val smsPackage = Telephony.Sms.getDefaultSmsPackage(context)
    if (smsPackage != null) {
        intent.setPackage(smsPackage)
    }
    if (intent.resolveActivity(context.packageManager) != null) {
        context.startActivity(intent)
    } else {
        Toast.makeText(context, "No messaging app found.", Toast.LENGTH_SHORT).show()
    }
}

fun sendThankYouNote(
    context: Context,
    appointment: Appointment,
    inspectionStore: InspectionStore
) {
    val phone = appointment.resolveMessagingPhone()
    if (phone.isBlank()) {
        Toast.makeText(
            context,
            "Add a customer phone (or phone in the job description) to send a thank you note.",
            Toast.LENGTH_LONG
        ).show()
        return
    }
    Thread {
        try {
            val form = inspectionFormForThankYou(appointment, inspectionStore)
            val file = ReportExporter.exportPdf(context, form)
            val uri = androidx.core.content.FileProvider.getUriForFile(
                context,
                "${context.packageName}.fileprovider",
                file
            )
            val message = buildThankYouNoteMessage()
            android.os.Handler(android.os.Looper.getMainLooper()).post {
                shareThankYouWithInspectionPdf(context, uri, phone, message)
            }
        } catch (_: Exception) {
            android.os.Handler(android.os.Looper.getMainLooper()).post {
                Toast.makeText(
                    context,
                    "Could not attach inspection PDF. Please try again.",
                    Toast.LENGTH_LONG
                ).show()
            }
        }
    }.start()
}

fun shareThankYouWithInspectionPdf(
    context: Context,
    pdfUri: Uri,
    phone: String,
    message: String
) {
    shareInspectionPdfToCustomer(
        context = context,
        pdfUri = pdfUri,
        customerPhone = phone,
        subject = "$COMPANY_NAME — Thank you",
        message = message
    )
}

fun openWaze(context: Context, address: String) {
    if (address.isBlank()) {
        Toast.makeText(context, "No address on this appointment.", Toast.LENGTH_SHORT).show()
        return
    }
    val encoded = Uri.encode(address)
    val wazeIntent = Intent(
        Intent.ACTION_VIEW,
        Uri.parse("https://waze.com/ul?q=$encoded&navigate=yes")
    ).apply {
        setPackage("com.waze")
    }
    if (wazeIntent.resolveActivity(context.packageManager) != null) {
        context.startActivity(wazeIntent)
        return
    }
    val fallback = Intent(
        Intent.ACTION_VIEW,
        Uri.parse("https://waze.com/ul?q=$encoded&navigate=yes")
    )
    if (fallback.resolveActivity(context.packageManager) != null) {
        context.startActivity(fallback)
    } else {
        val mapsIntent = Intent(
            Intent.ACTION_VIEW,
            Uri.parse("geo:0,0?q=$encoded")
        )
        if (mapsIntent.resolveActivity(context.packageManager) != null) {
            context.startActivity(mapsIntent)
        } else {
            Toast.makeText(context, "Install Waze to open directions.", Toast.LENGTH_LONG).show()
        }
    }
}
