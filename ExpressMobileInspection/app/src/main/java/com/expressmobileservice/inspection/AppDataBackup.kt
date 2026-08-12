package com.expressmobileservice.inspection

import android.content.ContentValues
import android.content.Context
import android.net.Uri
import android.os.Build
import android.os.Environment
import android.provider.MediaStore
import kotlinx.serialization.Serializable
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import java.io.File

/**
 * Keeps appointments and inspections across app updates and reinstalls.
 * Writes to app storage and Downloads/ExpressMobileService/ExpressMobileService_backup.json.
 */
object AppDataBackup {

    private const val INTERNAL_DIR = "autosave"
    private const val INTERNAL_FILE = "express_data.json"
    private const val DOWNLOADS_DIR = "ExpressMobileService"
    private const val DOWNLOADS_FILE = "ExpressMobileService_backup.json"

    private val json = Json { ignoreUnknownKeys = true }

    @Serializable
    data class BackupPayload(
        val appointments: List<Appointment> = emptyList(),
        val inspections: List<SavedInspection> = emptyList(),
        val savedAtMillis: Long = System.currentTimeMillis()
    )

    data class BackupCandidate(
        val payload: BackupPayload,
        val sourceLabel: String
    )

    fun isStoreEmpty(appointmentStore: AppointmentStore, inspectionStore: InspectionStore): Boolean =
        appointmentStore.getAll().isEmpty() && inspectionStore.decodeAll().isEmpty()

    fun restoreIfNeeded(
        context: Context,
        appointmentStore: AppointmentStore,
        inspectionStore: InspectionStore
    ): RestoreResult {
        if (!isStoreEmpty(appointmentStore, inspectionStore)) {
            return RestoreResult(false, null, 0, 0)
        }
        val candidate = findBestBackup(context) ?: return RestoreResult(false, null, 0, 0)
        applyBackup(appointmentStore, inspectionStore, candidate.payload)
        writeSnapshot(context, appointmentStore, inspectionStore)
        return RestoreResult(
            restored = candidate.payload.appointments.isNotEmpty() || candidate.payload.inspections.isNotEmpty(),
            sourceLabel = candidate.sourceLabel,
            appointmentCount = candidate.payload.appointments.size,
            inspectionCount = candidate.payload.inspections.size
        )
    }

    fun restoreFromUri(
        context: Context,
        uri: Uri,
        appointmentStore: AppointmentStore,
        inspectionStore: InspectionStore
    ): RestoreResult {
        val text = context.contentResolver.openInputStream(uri)?.bufferedReader()?.use { it.readText() }
            ?: return RestoreResult(false, null, 0, 0)
        val payload = runCatching { json.decodeFromString<BackupPayload>(text) }.getOrNull()
            ?: return RestoreResult(false, null, 0, 0)
        applyBackup(appointmentStore, inspectionStore, payload)
        writeSnapshot(context, appointmentStore, inspectionStore)
        return RestoreResult(
            restored = payload.appointments.isNotEmpty() || payload.inspections.isNotEmpty(),
            sourceLabel = "selected file",
            appointmentCount = payload.appointments.size,
            inspectionCount = payload.inspections.size
        )
    }

    fun restoreFromDownloads(
        context: Context,
        appointmentStore: AppointmentStore,
        inspectionStore: InspectionStore
    ): RestoreResult {
        val candidate = findBestBackup(context) ?: return RestoreResult(false, null, 0, 0)
        applyBackup(appointmentStore, inspectionStore, candidate.payload)
        writeSnapshot(context, appointmentStore, inspectionStore)
        return RestoreResult(
            restored = candidate.payload.appointments.isNotEmpty() || candidate.payload.inspections.isNotEmpty(),
            sourceLabel = candidate.sourceLabel,
            appointmentCount = candidate.payload.appointments.size,
            inspectionCount = candidate.payload.inspections.size
        )
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

    fun findBestBackup(context: Context): BackupCandidate? {
        val candidates = buildList {
            readInternalBackup(context)?.let { add(BackupCandidate(it, "app autosave")) }
            readDownloadsBackup(context)?.let { add(BackupCandidate(it, "Downloads backup")) }
            readLegacyDownloadsFile()?.let { add(BackupCandidate(it, "Downloads folder")) }
        }
        return candidates.maxByOrNull { it.payload.savedAtMillis }
    }

    private fun applyBackup(
        appointmentStore: AppointmentStore,
        inspectionStore: InspectionStore,
        payload: BackupPayload
    ) {
        appointmentStore.replaceAll(payload.appointments)
        inspectionStore.replaceAll(payload.inspections)
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
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.Q) {
            writeLegacyDownloadsFile(raw)
            return
        }
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
        writeLegacyDownloadsFile(raw)
    }

    private fun readDownloadsBackup(context: Context): BackupPayload? {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.Q) return readLegacyDownloadsFile()
        return runCatching {
            val resolver = context.contentResolver
            val collection = MediaStore.Downloads.getContentUri(MediaStore.VOLUME_EXTERNAL_PRIMARY)
            resolver.query(
                collection,
                arrayOf(MediaStore.Downloads._ID, MediaStore.MediaColumns.DATE_MODIFIED),
                "${MediaStore.MediaColumns.DISPLAY_NAME}=?",
                arrayOf(DOWNLOADS_FILE),
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

    private fun legacyDownloadsFile(): File {
        val base = Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_DOWNLOADS)
        return File(File(base, DOWNLOADS_DIR), DOWNLOADS_FILE)
    }

    private fun writeLegacyDownloadsFile(raw: String) {
        runCatching {
            val file = legacyDownloadsFile()
            file.parentFile?.mkdirs()
            file.writeText(raw)
        }
    }

    private fun readLegacyDownloadsFile(): BackupPayload? {
        return runCatching {
            val file = legacyDownloadsFile()
            if (!file.exists()) return null
            json.decodeFromString<BackupPayload>(file.readText())
        }.getOrNull()
    }

    data class RestoreResult(
        val restored: Boolean,
        val sourceLabel: String?,
        val appointmentCount: Int,
        val inspectionCount: Int
    )
}
