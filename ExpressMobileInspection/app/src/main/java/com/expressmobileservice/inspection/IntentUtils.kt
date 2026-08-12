package com.expressmobileservice.inspection

import android.content.Context
import android.content.Intent
import android.net.Uri
import android.widget.Toast

const val GOOGLE_MESSAGES_PACKAGE = "com.google.android.apps.messaging"

private fun phoneDigits(phone: String): String = phone.filter { it.isDigit() || it == '+' }

fun dialPhone(context: Context, phone: String) {
    val digits = phoneDigits(phone)
    if (digits.isBlank()) {
        Toast.makeText(context, "No phone number on this appointment.", Toast.LENGTH_SHORT).show()
        return
    }
    val intent = Intent(Intent.ACTION_DIAL, Uri.parse("tel:$digits"))
    context.startActivity(intent)
}

fun messagePhone(context: Context, phone: String) {
    val digits = phoneDigits(phone)
    if (digits.isBlank()) {
        Toast.makeText(context, "No phone number on this appointment.", Toast.LENGTH_SHORT).show()
        return
    }
    val googleMessages = Intent(Intent.ACTION_SENDTO, Uri.parse("smsto:$digits")).apply {
        setPackage(GOOGLE_MESSAGES_PACKAGE)
    }
    if (googleMessages.resolveActivity(context.packageManager) != null) {
        context.startActivity(googleMessages)
        return
    }
    val intent = Intent(Intent.ACTION_SENDTO, Uri.parse("smsto:$digits"))
    if (intent.resolveActivity(context.packageManager) != null) {
        context.startActivity(intent)
    } else {
        Toast.makeText(context, "No messaging app found.", Toast.LENGTH_SHORT).show()
    }
}

/**
 * Opens Google Messages to send [attachmentUri] to the customer at [phone].
 * Tries MMS with attachment first, then a compose thread for [phone], then the system share sheet.
 */
fun shareReportToGoogleMessages(
    context: Context,
    phone: String,
    attachmentUri: Uri,
    mimeType: String,
    subject: String,
    body: String
): Boolean {
    val digits = phoneDigits(phone)
    if (digits.isBlank()) {
        Toast.makeText(
            context,
            "Enter the customer phone before sending the report.",
            Toast.LENGTH_SHORT
        ).show()
        return false
    }

    val messagesWithAttachment = Intent(Intent.ACTION_SEND).apply {
        type = mimeType
        putExtra(Intent.EXTRA_STREAM, attachmentUri)
        putExtra(Intent.EXTRA_SUBJECT, subject)
        putExtra(Intent.EXTRA_TEXT, body)
        putExtra("sms_body", body)
        putExtra("address", digits)
        setPackage(GOOGLE_MESSAGES_PACKAGE)
        addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
    }
    if (messagesWithAttachment.resolveActivity(context.packageManager) != null) {
        context.startActivity(messagesWithAttachment)
        return true
    }

    val mmstoIntent = Intent(Intent.ACTION_SENDTO, Uri.parse("mmsto:$digits")).apply {
        putExtra("sms_body", body)
        putExtra(Intent.EXTRA_TEXT, body)
        putExtra(Intent.EXTRA_STREAM, attachmentUri)
        setPackage(GOOGLE_MESSAGES_PACKAGE)
        addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
    }
    if (mmstoIntent.resolveActivity(context.packageManager) != null) {
        context.startActivity(mmstoIntent)
        return true
    }

    val composeIntent = Intent(Intent.ACTION_SENDTO, Uri.parse("smsto:$digits")).apply {
        putExtra("sms_body", body)
        setPackage(GOOGLE_MESSAGES_PACKAGE)
    }
    if (composeIntent.resolveActivity(context.packageManager) != null) {
        context.startActivity(composeIntent)
        Toast.makeText(
            context,
            "Attach the inspection report using the paperclip in Messages.",
            Toast.LENGTH_LONG
        ).show()
        return true
    }

    val fallback = Intent(Intent.ACTION_SEND).apply {
        type = mimeType
        putExtra(Intent.EXTRA_STREAM, attachmentUri)
        putExtra(Intent.EXTRA_SUBJECT, subject)
        putExtra(Intent.EXTRA_TEXT, "To: $digits\n\n$body")
        addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
    }
    if (fallback.resolveActivity(context.packageManager) != null) {
        context.startActivity(Intent.createChooser(fallback, "Send inspection report"))
        return true
    }

    Toast.makeText(context, "No messaging app found.", Toast.LENGTH_SHORT).show()
    return false
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
