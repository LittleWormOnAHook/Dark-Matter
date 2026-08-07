package com.expressmobileservice.inspection

import android.content.Context
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json

class AppointmentStore(context: Context) {

    private val prefs = context.applicationContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
    private val json = Json { ignoreUnknownKeys = true }

    fun getAll(): List<Appointment> {
        val raw = prefs.getString(KEY_APPOINTMENTS, null) ?: return emptyList()
        return runCatching {
            json.decodeFromString<List<Appointment>>(raw)
        }.getOrDefault(emptyList()).sortedBy { it.startEpochMillis }
    }

    fun save(appointment: Appointment) {
        val current = getAll().toMutableList()
        val index = current.indexOfFirst { it.id == appointment.id }
        if (index >= 0) {
            current[index] = appointment
        } else {
            current.add(appointment)
        }
        persist(current)
    }

    fun delete(id: String) {
        persist(getAll().filterNot { it.id == id })
    }

    private fun persist(appointments: List<Appointment>) {
        prefs.edit()
            .putString(KEY_APPOINTMENTS, json.encodeToString(appointments))
            .apply()
    }

    companion object {
        private const val PREFS_NAME = "express_appointments"
        private const val KEY_APPOINTMENTS = "appointments_json"
    }
}
