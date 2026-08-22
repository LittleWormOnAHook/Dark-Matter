package com.expressmobileservice.inspection

import android.content.Context
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json

class InspectionStore(context: Context) {

    private val appContext = context.applicationContext
    private val prefs = appContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
    private val json = Json { ignoreUnknownKeys = true }

    var onDataChanged: (() -> Unit)? = null

    fun getAll(appointmentStore: AppointmentStore? = null): List<SavedInspection> =
        decodeAll().sortedEarliestFirst(appointmentStore)

    fun getById(id: String): SavedInspection? = decodeAll().firstOrNull { it.id == id }

    fun getByAppointmentId(appointmentId: String): SavedInspection? =
        decodeAll().firstOrNull { it.appointmentId == appointmentId }

    fun save(inspection: SavedInspection) {
        val current = decodeAll().toMutableList()
        val index = current.indexOfFirst { it.id == inspection.id }
        val existing = current.getOrNull(index)
        val resolvedDate = when {
            inspection.inspectionDateMillis > 0L -> inspection.inspectionDateMillis
            existing != null && existing.inspectionDateMillis > 0L -> existing.inspectionDateMillis
            else -> System.currentTimeMillis()
        }
        val updated = inspection.copy(
            inspectionDateMillis = resolvedDate,
            updatedAtMillis = System.currentTimeMillis()
        )
        if (index >= 0) {
            current[index] = updated
        } else {
            current.add(updated)
        }
        persist(current)
    }

    fun saveFromAppointment(appointment: Appointment): SavedInspection {
        val existing = getByAppointmentId(appointment.id)
        val inspection = if (existing != null) {
            existing.copy(
                customerName = appointment.customerName,
                customerPhone = appointment.customerPhone,
                vehicle = appointment.vehicleSummary().ifBlank { existing.vehicle },
                mileage = appointment.mileage.ifBlank { existing.mileage },
                generalNotes = appointment.jobNotes.ifBlank { existing.generalNotes },
                inspectionDateMillis = appointment.startEpochMillis,
                updatedAtMillis = System.currentTimeMillis()
            )
        } else {
            inspectionFromAppointment(appointment)
        }
        save(inspection)
        return inspection
    }

    fun delete(id: String) {
        persist(decodeAll().filterNot { it.id == id })
    }

    fun mostRecent(): SavedInspection? =
        decodeAll().maxByOrNull { it.updatedAtMillis }

    fun hasUserData(): Boolean = prefs.contains(KEY_INSPECTIONS)

    fun replaceAll(inspections: List<SavedInspection>) {
        persist(inspections, notify = false)
    }

    fun decodeAll(): List<SavedInspection> {
        val raw = prefs.getString(KEY_INSPECTIONS, null) ?: return emptyList()
        return runCatching {
            json.decodeFromString<List<SavedInspection>>(raw)
        }.getOrDefault(emptyList())
    }

    private fun persist(inspections: List<SavedInspection>, notify: Boolean = true) {
        prefs.edit()
            .putString(KEY_INSPECTIONS, json.encodeToString(inspections))
            .apply()
        if (notify) onDataChanged?.invoke()
    }

    companion object {
        private const val PREFS_NAME = "express_inspections"
        private const val KEY_INSPECTIONS = "inspections_json"
    }
}
