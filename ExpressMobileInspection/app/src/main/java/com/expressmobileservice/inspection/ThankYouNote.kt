package com.expressmobileservice.inspection

private val PHONE_IN_TEXT = Regex(
    """(?:\+?1[\s.-]?)?(?:\(\s*\d{3}\s*\)|\d{3})[\s.-]?\d{3}[\s.-]?\d{4}"""
)

fun Appointment.resolveMessagingPhone(): String {
    if (customerPhone.isNotBlank()) return customerPhone.trim()
    return extractPhoneFromText(jobNotes)
        ?: extractPhoneFromText(displayTitle)
        ?: extractPhoneFromText(toClipboardText())
        ?: ""
}

fun Appointment.customerFirstName(): String {
    val raw = customerName.trim().ifBlank {
        jobNotes.trim().split(Regex("[\\s—,|]+")).firstOrNull().orEmpty()
    }
    return raw.split(Regex("\\s+")).firstOrNull()?.trim().orEmpty()
}

fun buildThankYouNoteMessage(customerName: String, jobNotes: String = ""): String {
    val firstName = customerName.trim().split(Regex("\\s+")).firstOrNull()?.trim().orEmpty()
        .ifBlank {
            jobNotes.trim().split(Regex("[\\s—,|]+")).firstOrNull()?.trim().orEmpty()
        }
    val greeting = if (firstName.isNotBlank()) {
        "Thank you, $firstName!"
    } else {
        "Thank you!"
    }
    return buildString {
        appendLine(greeting)
        appendLine()
        appendLine(
            "Thank you for choosing $COMPANY_NAME. We appreciate you trusting us with your vehicle " +
                "today, and we hope everything is running smoothly for you."
        )
        appendLine()
        appendLine("When you have a moment, we'd love to hear how we did:")
        appendLine("Google review: $COMPANY_GOOGLE_REVIEW_URL")
        appendLine()
        appendLine("Learn more about our services:")
        appendLine(COMPANY_WEBSITE_DISPLAY)
        appendLine(COMPANY_WEBSITE)
        appendLine()
        appendLine("Questions? Call us anytime:")
        appendLine(COMPANY_PHONE_DISPLAY)
    }.trimEnd()
}

fun Appointment.buildThankYouNoteMessage(): String =
    buildThankYouNoteMessage(customerName, jobNotes)

private fun extractPhoneFromText(text: String): String? {
    if (text.isBlank()) return null
    return PHONE_IN_TEXT.find(text)?.value?.trim()
}
