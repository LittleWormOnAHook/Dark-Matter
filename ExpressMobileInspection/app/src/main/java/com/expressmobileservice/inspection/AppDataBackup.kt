package com.expressmobileservice.inspection

import android.content.ContentValues
import android.content.Context
import android.os.Build
import android.os.Environment
import android.provider.MediaStore
import kotlinx.serialization.Serializable
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import java.io.File

/**
 * Keeps appointments and inspections across app updates and reinstalls (when Google backup
 * or the on-device backup file in Downloads is available).
 */
object AppDataBackup {

    private const val INTERNAL_DIR = "autosave"
    private const val INTERNAL_FILE = "express_data.json"
    private const val DOWNLOADS_DIR = "ExpressMobileService"
    private const val DOWNLOADS_FILE = "ExpressMobileService_backup.json"

    private val json = Json { ignoreUnknownKeys = true }

    @Serializable
    private data class BackupPayload(
        val appointments: List<Appointment> = emptyList(),
        val inspections: List<SavedInspection> = emptyList(),
        val savedAtMillis: Long = System.currentTimeMillis()
    )

    fun restoreIfNeeded(
        context: Context,
        appointmentStore: AppointmentStore,
        inspectionStore: InspectionStore
    ): Boolean {
        if (appointmentStore.hasUserData() || inspectionStore.hasUserData()) return false
        val payload = readInternalBackup(context)
            ?: readDownloadsBackup(context)
            ?: return false
        appointmentStore.replaceAll(payload.appointments)
        inspectionStore.replaceAll(payload.inspections)
        writeSnapshot(context, appointmentStore, inspectionStore)
        return payload.appointments.isNotEmpty() || payload.inspections.isNotEmpty()
    }

    fun writeSnapshot(
        context: Context,
        appointmentStore: AppointmentStore,
        inspectionStore: InspectionStore
    ) {
        val payload = BackupPayload(
            appointments = appointmentStore.getAll(),
            inspections = inspectionStore.decodeAll(),
            savedAtMillis = System.currentTimeMillis()
        )
        val raw = json.encodeToString(payload)
        writeInternalBackup(context, raw)
        writeDownloadsBackup(context, raw)
    }

    private fun writeInternalBackup(context: Context, raw: String) {
        runCatching {
            val dir = File(context.filesDir, INTERNAL_DIR)
            dir.mkdirs()
            File(dir, INTERNAL_FILE).writeText(raw)
        }
    }

    private fun readInternalBackup(context: Context): BackupPayload? {
        return runCatching {
            val file = File(File(context.filesDir, INTERNAL_DIR), INTERNAL_FILE)
            if (!file.exists()) return null
            json.decodeFromString<BackupPayload>(file.readText())
        }.getOrNull()
    }

    private fun writeDownloadsBackup(context: Context, raw: String) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.Q) return
        runCatching {
            val resolver = context.contentResolver
            val collection = MediaStore.Downloads.getContentUri(MediaStore.VOLUME_EXTERNAL_PRIMARY)
            val existing = resolver.query(
                collection,
                arrayOf(MediaStore.Downloads._ID),
                "${MediaStore.Downloads.DISPLAY_NAME}=?",
                arrayOf(DOWNLOADS_FILE),
                null
            )
            existing?.use { cursor ->
                while (cursor.moveToNext()) {
                    val id = cursor.getLong(0)
                    resolver.delete(
                        MediaStore.Downloads.EXTERNAL_CONTENT_URI.buildUpon()
                            .appendPath(id.toString())
                            .build(),
                        null,
                        null
                    )
                }
            }
            val values = ContentValues().apply {
                put(MediaStore.MediaColumns.DISPLAY_NAME, DOWNLOADS_FILE)
                put(MediaStore.MediaColumns.MIME_TYPE, "application/json")
                put(MediaStore.MediaColumns.RELATIVE_PATH, "${Environment.DIRECTORY_DOWNLOADS}/$DOWNLOADS_DIR")
                put(MediaStore.MediaColumns.IS_PENDING, 1)
            }
            val uri = resolver.insert(collection, values) ?: return
            resolver.openOutputStream(uri)?.use { stream ->
                stream.write(raw.toByteArray(Charsets.UTF_8))
            }
            values.clear()
            values.put(MediaStore.MediaColumns.IS_PENDING, 0)
            resolver.update(uri, values, null, null)
        }
    }

    private fun readDownloadsBackup(context: Context): BackupPayload? {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.Q) return null
        return runCatching {
            val resolver = context.contentResolver
            val collection = MediaStore.Downloads.getContentUri(MediaStore.VOLUME_EXTERNAL_PRIMARY)
            resolver.query(
                collection,
                arrayOf(MediaStore.Downloads._ID),
                "${MediaStore.MediaColumns.DISPLAY_NAME}=? AND ${MediaStore.MediaColumns.RELATIVE_PATH} LIKE ?",
                arrayOf(DOWNLOADS_FILE, "%$DOWNLOADS_DIR%"),
                "${MediaStore.MediaColumns.DATE_MODIFIED} DESC"
            )?.use { cursor ->
                if (!cursor.moveToFirst()) return null
                val id = cursor.getLong(0)
                val uri = MediaStore.Downloads.EXTERNAL_CONTENT_URI.buildUpon()
                    .appendPath(id.toString())
                    .build()
                val text = resolver.openInputStream(uri)?.bufferedReader()?.use { it.readText() }
                    ?: return null
                json.decodeFromString<BackupPayload>(text)
            }
        }.getOrNull()
    }
}
