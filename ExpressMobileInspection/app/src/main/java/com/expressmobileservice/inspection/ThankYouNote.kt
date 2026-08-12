package com.expressmobileservice.inspection

private val PHONE_IN_TEXT = Regex(
    """(?:\+?1[\s.-]?)?(?:\(\s*\d{3}\s*\)|\d{3})[\s.-]?\d{3}[\s.-]?\d{4}"""
)

const val THANK_YOU_HEADING = "Thank you"
const val THANK_YOU_BODY =
    "Thank you for choosing $COMPANY_NAME. We appreciate you trusting us with your vehicle " +
        "today, and we hope everything is running smoothly for you."
const val THANK_YOU_PROMPT = "When you have a moment, we'd love to hear how we did:"
const val THANK_YOU_GOOGLE_REVIEW_LABEL = "Google Review"
const val THANK_YOU_WEBSITE_LABEL = "Website"
const val THANK_YOU_PDF_LABEL = "Inspection PDF attached"

fun Appointment.resolveMessagingPhone(): String {
    if (customerPhone.isNotBlank()) return customerPhone.trim()
    return extractPhoneFromText(jobNotes)
        ?: extractPhoneFromText(displayTitle)
        ?: extractPhoneFromText(toClipboardText())
        ?: ""
}

/** Plain-text fallback if a text body is ever needed (no HTML). */
fun buildThankYouNotePlainMessage(): String = buildString {
    appendLine(THANK_YOU_HEADING)
    appendLine()
    appendLine(THANK_YOU_BODY)
    appendLine()
    appendLine(THANK_YOU_PROMPT)
}.trimEnd()

/**
 * MMS text body: thank-you message first, then compact named links (URL on same line — tappable).
 * SMS/RCS cannot hide URLs behind custom link text without HTML (unsupported in Google Messages).
 */
fun buildThankYouNoteSmsLinks(): String = buildString {
    appendLine(THANK_YOU_HEADING)
    appendLine()
    appendLine(THANK_YOU_BODY)
    appendLine()
    appendLine(THANK_YOU_PROMPT)
    appendLine()
    appendLine(THANK_YOU_GOOGLE_REVIEW_LABEL)
    appendLine(COMPANY_GOOGLE_REVIEW_URL)
    appendLine()
    appendLine(THANK_YOU_WEBSITE_LABEL)
    appendLine(COMPANY_WEBSITE.trimEnd('/'))
}.trimEnd()

fun Appointment.buildThankYouNoteMessage(): String = buildThankYouNotePlainMessage()

fun Appointment.resolveInspection(inspectionStore: InspectionStore): SavedInspection? {
    if (inspectionId.isNotBlank()) {
        inspectionStore.getById(inspectionId)?.let { return it }
    }
    if (id.isNotBlank()) {
        inspectionStore.getByAppointmentId(id)?.let { return it }
    }
    return null
}

fun inspectionFormForThankYou(
    appointment: Appointment,
    inspectionStore: InspectionStore
): InspectionFormState {
    appointment.resolveInspection(inspectionStore)?.let { return it.toFormState() }
    return inspectionFromAppointment(appointment).toFormState()
}

fun appointmentSendReadinessError(
    appointment: Appointment,
    inspectionStore: InspectionStore,
    formOverride: InspectionFormState? = null
): String? {
    if (appointment.resolveMessagingPhone().isBlank()) {
        return "Add a customer phone (or phone in the job description) to send a thank you note."
    }
    val form = formOverride ?: inspectionFormForThankYou(appointment, inspectionStore)
    return form.inspectionSendReadinessError()
}

private fun extractPhoneFromText(text: String): String? {
    if (text.isBlank()) return null
    return PHONE_IN_TEXT.find(text)?.value?.trim()
}
