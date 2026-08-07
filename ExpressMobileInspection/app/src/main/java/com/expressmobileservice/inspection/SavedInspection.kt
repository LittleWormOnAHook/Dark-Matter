package com.expressmobileservice.inspection

import kotlinx.serialization.Serializable
import java.util.UUID

@Serializable
enum class SerializableInspectionStatus {
    NONE, GOOD, BAD, REPLACE;

    fun toStatus(): InspectionStatus = when (this) {
        NONE -> InspectionStatus.NONE
        GOOD -> InspectionStatus.GOOD
        BAD -> InspectionStatus.BAD
        REPLACE -> InspectionStatus.REPLACE
    }

    companion object {
        fun from(status: InspectionStatus) = when (status) {
            InspectionStatus.NONE -> NONE
            InspectionStatus.GOOD -> GOOD
            InspectionStatus.BAD -> BAD
            InspectionStatus.REPLACE -> REPLACE
        }
    }
}

@Serializable
data class SerializableInspectionItem(
    val id: String,
    val label: String,
    val status: SerializableInspectionStatus = SerializableInspectionStatus.NONE,
    val notes: String = ""
)

@Serializable
data class SerializableInspectionSection(
    val title: String,
    val items: List<SerializableInspectionItem>
)

@Serializable
data class SavedInspection(
    val id: String = UUID.randomUUID().toString(),
    val appointmentId: String? = null,
    val customerName: String = "",
    val customerPhone: String = "",
    val vehicle: String = "",
    val mileage: String = "",
    val generalNotes: String = "",
    val sections: List<SerializableInspectionSection> = defaultSerializableSections(),
    val updatedAtMillis: Long = System.currentTimeMillis()
) {
    fun toFormState(): InspectionFormState = InspectionFormState(
        customerInfo = CustomerInfo(
            customerName = customerName,
            customerPhone = customerPhone,
            vehicle = vehicle,
            mileage = mileage
        ),
        sections = sections.map { section ->
            InspectionSection(
                title = section.title,
                items = section.items.map { item ->
                    InspectionItem(
                        id = item.id,
                        label = item.label,
                        status = item.status.toStatus(),
                        notes = item.notes
                    )
                }
            )
        },
        generalNotes = generalNotes
    )
}

fun InspectionFormState.toSavedInspection(
    id: String,
    appointmentId: String? = null
): SavedInspection = SavedInspection(
    id = id,
    appointmentId = appointmentId,
    customerName = customerInfo.customerName,
    customerPhone = customerInfo.customerPhone,
    vehicle = customerInfo.vehicle,
    mileage = customerInfo.mileage,
    generalNotes = generalNotes,
    sections = sections.map { section ->
        SerializableInspectionSection(
            title = section.title,
            items = section.items.map { item ->
                SerializableInspectionItem(
                    id = item.id,
                    label = item.label,
                    status = SerializableInspectionStatus.from(item.status),
                    notes = item.notes
                )
            }
        )
    },
    updatedAtMillis = System.currentTimeMillis()
)

fun defaultSerializableSections(): List<SerializableInspectionSection> =
    defaultInspectionSections().map { section ->
        SerializableInspectionSection(
            title = section.title,
            items = section.items.map { item ->
                SerializableInspectionItem(id = item.id, label = item.label)
            }
        )
    }

fun inspectionFromAppointment(appointment: Appointment): SavedInspection {
    val vehicleText = appointment.vehicleSummary().ifBlank {
        listOfNotNull(
            appointment.vehicleYear?.toString(),
            appointment.vehicleMake.takeIf { it.isNotBlank() },
            appointment.vehicleModel.takeIf { it.isNotBlank() }
        ).joinToString(" ")
    }
    return SavedInspection(
        id = appointment.inspectionId.ifBlank { UUID.randomUUID().toString() },
        appointmentId = appointment.id,
        customerName = appointment.customerName,
        customerPhone = appointment.customerPhone,
        vehicle = vehicleText,
        mileage = appointment.mileage,
        generalNotes = appointment.jobNotes,
        sections = defaultSerializableSections(),
        updatedAtMillis = System.currentTimeMillis()
    )
}
