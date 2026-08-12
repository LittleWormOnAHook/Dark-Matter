package com.expressmobileservice.inspection

import android.content.ClipData
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

fun openWebLink(context: Context, url: String) {
    if (url.isBlank()) return
    val intent = Intent(Intent.ACTION_VIEW, Uri.parse(url))
    if (intent.resolveActivity(context.packageManager) != null) {
        context.startActivity(intent)
    } else {
        Toast.makeText(context, "No browser found.", Toast.LENGTH_SHORT).show()
    }
}

/**
 * Opens Google Messages with the thank-you card image, plain tappable links, and inspection PDF.
 */
fun shareThankYouWithInspectionPdf(
    context: Context,
    pdfUri: Uri,
    phone: String,
    cardImageUri: Uri?,
    linkMessage: String = buildThankYouNoteSmsLinks()
) {
    val digits = normalizePhoneDigits(phone)
    if (digits.isBlank()) {
        Toast.makeText(context, "No phone number on this appointment.", Toast.LENGTH_SHORT).show()
        return
    }

    val smsPackage = Telephony.Sms.getDefaultSmsPackage(context)
    val attachments = buildList {
        cardImageUri?.let { add(it) }
        add(pdfUri)
    }

    fun grantReadPermissions(intent: Intent) {
        intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
        smsPackage?.let { pkg ->
            attachments.forEach { uri ->
                context.grantUriPermission(pkg, uri, Intent.FLAG_GRANT_READ_URI_PERMISSION)
            }
        }
    }

    fun buildClipData(): ClipData {
        val clip = ClipData.newUri(context.contentResolver, "Thank you card", attachments.first())
        attachments.drop(1).forEach { uri ->
            clip.addItem(ClipData.Item(uri))
        }
        return clip
    }

    if (attachments.size > 1) {
        val multiIntent = Intent(Intent.ACTION_SEND_MULTIPLE).apply {
            type = "*/*"
            putParcelableArrayListExtra(Intent.EXTRA_STREAM, ArrayList(attachments))
            clipData = buildClipData()
            putExtra("address", digits)
            putExtra("sms_body", linkMessage)
            putExtra(Intent.EXTRA_TEXT, linkMessage)
        }
        grantReadPermissions(multiIntent)
        smsPackage?.let { multiIntent.setPackage(it) }
        if (multiIntent.resolveActivity(context.packageManager) != null) {
            context.startActivity(multiIntent)
            return
        }
        multiIntent.setPackage(null)
        if (multiIntent.resolveActivity(context.packageManager) != null) {
            context.startActivity(multiIntent)
            return
        }
    }

    val pdfIntent = Intent(Intent.ACTION_SEND).apply {
        type = "application/pdf"
        putExtra(Intent.EXTRA_STREAM, pdfUri)
        clipData = ClipData.newUri(context.contentResolver, "Inspection PDF", pdfUri)
        putExtra("address", digits)
        putExtra("sms_body", linkMessage)
        putExtra(Intent.EXTRA_TEXT, linkMessage)
        putExtra(Intent.EXTRA_SUBJECT, "$COMPANY_NAME — Thank you")
    }
    grantReadPermissions(pdfIntent)
    smsPackage?.let { pdfIntent.setPackage(it) }
    if (pdfIntent.resolveActivity(context.packageManager) != null) {
        context.startActivity(pdfIntent)
        return
    }
    pdfIntent.setPackage(null)
    if (pdfIntent.resolveActivity(context.packageManager) != null) {
        context.startActivity(pdfIntent)
    } else {
        Toast.makeText(context, "No messaging app found.", Toast.LENGTH_SHORT).show()
    }
}

/** @deprecated Use [shareThankYouWithInspectionPdf] — HTML/text bodies are not used for thank-you MMS. */
fun shareInspectionPdfToCustomer(
    context: Context,
    pdfUri: Uri,
    customerPhone: String,
    subject: String,
    message: String,
    cardImageUri: Uri? = null
) {
    shareThankYouWithInspectionPdf(context, pdfUri, customerPhone, cardImageUri)
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
            val pdfFile = ReportExporter.exportPdfWithThankYouCover(context, form)
            val pdfUri = androidx.core.content.FileProvider.getUriForFile(
                context,
                "${context.packageName}.fileprovider",
                pdfFile
            )
            android.os.Handler(android.os.Looper.getMainLooper()).post {
                shareThankYouWithInspectionPdf(context, pdfUri, phone, cardImageUri = null)
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
