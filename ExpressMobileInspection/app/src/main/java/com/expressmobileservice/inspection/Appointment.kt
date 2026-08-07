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
    val vehicleCategory: String = VehicleCategory.CAR_TRUCK.name,
    val vehicleYear: Int? = null,
    val vehicleMake: String = "",
    val vehicleModel: String = "",
    val engineSize: String = "",
    val mileage: String = "",
    val inspectionId: String = "",
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

    /** Samsung-style agenda line: phone + job (e.g. "(904) 226-8986 Intake…"). */
    val agendaTitle: String
        get() = buildString {
            if (customerPhone.isNotBlank()) append(customerPhone)
            val detail = when {
                jobNotes.isNotBlank() -> jobNotes
                customerName.isNotBlank() -> customerName
                else -> ""
            }
            if (detail.isNotBlank()) {
                if (isNotEmpty()) append(" ")
                append(detail)
            }
            if (isEmpty()) append("Appointment")
        }
}

enum class CalendarViewMode {
    DAY,
    WEEK,
    MONTH
}
