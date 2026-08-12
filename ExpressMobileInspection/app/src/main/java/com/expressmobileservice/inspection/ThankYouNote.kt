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

fun buildThankYouNoteMessage(): String = buildString {
    appendLine("Thank you")
    appendLine()
    appendLine(
        "Thank you for choosing $COMPANY_NAME. We appreciate you trusting us with your vehicle " +
            "today, and we hope everything is running smoothly for you."
    )
    appendLine()
    appendLine("When you have a moment, we'd love to hear how we did:")
    appendLine()
    appendLine("Google review")
    appendLine(COMPANY_GOOGLE_REVIEW_URL)
    appendLine()
    appendLine("expressmobileservice.net")
    appendLine(COMPANY_WEBSITE)
}.trimEnd()

fun Appointment.buildThankYouNoteMessage(): String = buildThankYouNoteMessage()

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
