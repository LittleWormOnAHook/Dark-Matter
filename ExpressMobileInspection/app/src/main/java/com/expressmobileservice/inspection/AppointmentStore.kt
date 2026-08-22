package com.expressmobileservice.inspection

import android.content.Context
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json

class AppointmentStore(context: Context) {

    private val appContext = context.applicationContext
    private val prefs = appContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
    private val json = Json { ignoreUnknownKeys = true }

    var onDataChanged: (() -> Unit)? = null

    fun getAll(): List<Appointment> {
        val raw = prefs.getString(KEY_APPOINTMENTS, null) ?: return emptyList()
        return runCatching {
            json.decodeFromString<List<Appointment>>(raw)
        }.getOrDefault(emptyList()).sortedBy { it.startEpochMillis }
    }

    fun getById(id: String): Appointment? = getAll().firstOrNull { it.id == id }

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

    fun hasUserData(): Boolean = prefs.contains(KEY_APPOINTMENTS)

    fun replaceAll(appointments: List<Appointment>) {
        prefs.edit()
            .putString(KEY_APPOINTMENTS, json.encodeToString(appointments))
            .apply()
    }

    private fun persist(appointments: List<Appointment>) {
        prefs.edit()
            .putString(KEY_APPOINTMENTS, json.encodeToString(appointments))
            .apply()
        onDataChanged?.invoke()
    }

    companion object {
        private const val PREFS_NAME = "express_appointments"
        private const val KEY_APPOINTMENTS = "appointments_json"
    }
}
