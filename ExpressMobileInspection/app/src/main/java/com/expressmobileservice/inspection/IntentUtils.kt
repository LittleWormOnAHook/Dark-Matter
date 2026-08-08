package com.expressmobileservice.inspection

import android.content.Context
import android.content.Intent
import android.net.Uri
import android.widget.Toast

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
    val intent = Intent(Intent.ACTION_SENDTO, Uri.parse("smsto:$digits"))
    if (intent.resolveActivity(context.packageManager) != null) {
        context.startActivity(intent)
    } else {
        Toast.makeText(context, "No messaging app found.", Toast.LENGTH_SHORT).show()
    }
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
