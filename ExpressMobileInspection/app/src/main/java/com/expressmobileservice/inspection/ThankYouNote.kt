package com.expressmobileservice.inspection

private val PHONE_IN_TEXT = Regex(
    """(?:\+?1[\s.-]?)?(?:\(\s*\d{3}\s*\)|\d{3})[\s.-]?\d{3}[\s.-]?\d{4}"""
)

const val THANK_YOU_HEADING = "Thank you"
const val THANK_YOU_BODY =
    "Thank you for choosing $COMPANY_NAME. We appreciate you trusting us with your vehicle " +
        "today, and we hope everything is running smoothly for you."
const val THANK_YOU_PROMPT = "When you have a moment, we'd love to hear how we did:"
const val THANK_YOU_GOOGLE_REVIEW_LABEL = "Google review"
const val THANK_YOU_WEBSITE_LABEL = COMPANY_WEBSITE_DISPLAY
const val THANK_YOU_PDF_LABEL = "Inspection PDF attached"

fun Appointment.resolveMessagingPhone(): String {
    if (customerPhone.isNotBlank()) return customerPhone.trim()
    return extractPhoneFromText(jobNotes)
        ?: extractPhoneFromText(displayTitle)
        ?: extractPhoneFromText(toClipboardText())
        ?: ""
}

/** Plain-text fallback for apps that ignore HTML MMS bodies. URLs stay linkified by the SMS app. */
fun buildThankYouNotePlainMessage(): String = buildString {
    appendLine(THANK_YOU_HEADING)
    appendLine()
    appendLine(THANK_YOU_BODY)
    appendLine()
    appendLine(THANK_YOU_PROMPT)
    appendLine()
    appendLine("$THANK_YOU_GOOGLE_REVIEW_LABEL: $COMPANY_GOOGLE_REVIEW_URL")
    appendLine()
    appendLine("$THANK_YOU_WEBSITE_LABEL: $COMPANY_WEBSITE")
}.trimEnd()

/**
 * HTML body for MMS — button labels are anchor links (no raw URLs shown).
 * Google Messages and many SMS apps render [text](url) style links from simple HTML anchors.
 */
fun buildThankYouNoteHtmlMessage(): String = buildString {
    appendLine("""<html><body style="font-family:sans-serif;color:#FFFFFF;background:#2C1432;">""")
    appendLine(
        """<p style="color:#FFD700;font-size:22px;font-weight:bold;text-align:center;margin:0 0 12px;">$THANK_YOU_HEADING</p>"""
    )
    appendLine(
        """<p style="text-align:center;line-height:1.45;margin:0 0 10px;">$THANK_YOU_BODY</p>"""
    )
    appendLine(
        """<p style="color:#BDB5D5;text-align:center;font-size:13px;margin:0 0 16px;">$THANK_YOU_PROMPT</p>"""
    )
    appendLine(
        """<p style="text-align:center;margin:0 0 10px;">${
            thankYouHtmlLinkButton(THANK_YOU_GOOGLE_REVIEW_LABEL, COMPANY_GOOGLE_REVIEW_URL)
        }</p>"""
    )
    appendLine(
        """<p style="text-align:center;margin:0;">${
            thankYouHtmlLinkButton(THANK_YOU_WEBSITE_LABEL, COMPANY_WEBSITE)
        }</p>"""
    )
    appendLine("</body></html>")
}.trimEnd()

/** @deprecated Use [buildThankYouNotePlainMessage] for plain text or pass both plain + HTML when sending. */
fun buildThankYouNoteMessage(): String = buildThankYouNotePlainMessage()

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

private fun thankYouHtmlLinkButton(label: String, url: String): String =
    """<a href="$url" style="display:inline-block;padding:12px 18px;border:1px solid #FFD700;border-radius:12px;background:#441F4D;color:#FFD700;font-weight:600;text-decoration:none;">$label</a>"""

private fun extractPhoneFromText(text: String): String? {
    if (text.isBlank()) return null
    return PHONE_IN_TEXT.find(text)?.value?.trim()
}
