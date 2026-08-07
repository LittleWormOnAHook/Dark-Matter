package com.expressmobileservice.inspection

import android.content.Context
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json

class InspectionStore(context: Context) {

    private val prefs = context.applicationContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
    private val json = Json { ignoreUnknownKeys = true }

    fun getAll(): List<SavedInspection> {
        val raw = prefs.getString(KEY_INSPECTIONS, null) ?: return emptyList()
        return runCatching {
            json.decodeFromString<List<SavedInspection>>(raw)
        }.getOrDefault(emptyList()).sortedByDescending { it.updatedAtMillis }
    }

    fun getById(id: String): SavedInspection? = getAll().firstOrNull { it.id == id }

    fun getByAppointmentId(appointmentId: String): SavedInspection? =
        getAll().firstOrNull { it.appointmentId == appointmentId }

    fun save(inspection: SavedInspection) {
        val current = getAll().toMutableList()
        val index = current.indexOfFirst { it.id == inspection.id }
        val updated = inspection.copy(updatedAtMillis = System.currentTimeMillis())
        if (index >= 0) {
            current[index] = updated
        } else {
            current.add(0, updated)
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
                updatedAtMillis = System.currentTimeMillis()
            )
        } else {
            inspectionFromAppointment(appointment)
        }
        save(inspection)
        return inspection
    }

    fun delete(id: String) {
        persist(getAll().filterNot { it.id == id })
    }

    fun mostRecent(): SavedInspection? = getAll().firstOrNull()

    private fun persist(inspections: List<SavedInspection>) {
        prefs.edit()
            .putString(KEY_INSPECTIONS, json.encodeToString(inspections))
            .apply()
    }

    companion object {
        private const val PREFS_NAME = "express_inspections"
        private const val KEY_INSPECTIONS = "inspections_json"
    }
}
