package com.expressmobileservice.inspection

import kotlinx.serialization.Serializable
import java.util.UUID

@Serializable
data class Appointment(
    val id: String = UUID.randomUUID().toString(),
    val customerName: String = "",
    val customerPhone: String = "",
    val jobNotes: String = "",
    val address: String = "",
    val startEpochMillis: Long = 0L,
    val endEpochMillis: Long = 0L,
    val allDay: Boolean = false,
    val colorArgb: Long = 0xFF1565C0L
) {
    val displayTitle: String
        get() = buildString {
            if (customerName.isNotBlank()) append(customerName)
            if (jobNotes.isNotBlank()) {
                if (isNotEmpty()) append(" — ")
                append(jobNotes)
            }
            if (isEmpty()) append("Appointment")
        }

    val hasPhone: Boolean get() = customerPhone.isNotBlank()
    val hasAddress: Boolean get() = address.isNotBlank()
}

enum class CalendarViewMode {
    DAY,
    WEEK,
    MONTH
}
