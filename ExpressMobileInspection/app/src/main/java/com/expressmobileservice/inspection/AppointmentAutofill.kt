package com.expressmobileservice.inspection

import java.time.LocalDate

fun Appointment.matchesAutofillQuery(query: String): Boolean {
    val q = query.trim().lowercase()
    if (q.isBlank()) return true
    return customerName.lowercase().contains(q) ||
        customerPhone.contains(q) ||
        jobNotes.lowercase().contains(q) ||
        address.lowercase().contains(q) ||
        vehicleMake.lowercase().contains(q) ||
        vehicleModel.lowercase().contains(q) ||
        vehicleSummary().lowercase().contains(q)
}

fun Appointment.autofillLabel(): String = buildString {
    if (!allDay) append(formatTime(startEpochMillis))
    else append("All day")
    if (customerPhone.isNotBlank()) {
        append(" · ")
        append(customerPhone)
    }
    val detail = when {
        customerName.isNotBlank() -> customerName
        jobNotes.isNotBlank() -> jobNotes
        else -> ""
    }
    if (detail.isNotBlank()) {
        append(" · ")
        append(detail)
    }
    val vehicle = vehicleSummary()
    if (vehicle.isNotBlank()) {
        append(" · ")
        append(vehicle)
    }
}

fun Appointment.toClipboardText(): String = buildString {
    if (customerPhone.isNotBlank()) appendLine(customerPhone)
    if (customerName.isNotBlank()) appendLine(customerName)
    if (address.isNotBlank()) appendLine(address)
    if (jobNotes.isNotBlank()) appendLine(jobNotes)
    val vehicle = vehicleSummary()
    if (vehicle.isNotBlank()) appendLine(vehicle)
    if (mileage.isNotBlank()) appendLine("Mileage: $mileage")
    if (engineSize.isNotBlank()) appendLine("Engine: $engineSize")
}.trim()

fun List<Appointment>.autofillSuggestions(
    query: String,
    day: LocalDate? = null
): List<Appointment> {
    val filtered = filter { apt ->
        apt.matchesAutofillQuery(query) &&
            (day == null || appointmentOverlapsDay(apt, day))
    }
    return filtered
        .sortedWith(compareBy<Appointment> { it.startEpochMillis }.thenByDescending { it.id })
        .distinctBy { apt ->
            when {
                apt.customerPhone.isNotBlank() -> "phone:${apt.customerPhone}"
                apt.customerName.isNotBlank() -> "name:${apt.customerName.lowercase()}|${apt.address.lowercase()}"
                else -> apt.id
            }
        }
        .sortedBy { it.startEpochMillis }
        .take(25)
}
