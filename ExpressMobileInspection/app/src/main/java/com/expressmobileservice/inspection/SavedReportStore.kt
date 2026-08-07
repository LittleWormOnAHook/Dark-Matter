package com.expressmobileservice.inspection

import android.content.Context
import com.expressmobileservice.inspection.ui.ReportShareType
import java.io.File
import org.json.JSONArray
import org.json.JSONObject

class SavedReportStore(private val context: Context) {

    private val savedDir: File =
        File(context.filesDir, "saved_reports").apply { mkdirs() }
    private val indexFile = File(savedDir, "index.json")

    fun listReports(): List<SavedReport> {
        val array = readIndexArray()
        val reports = mutableListOf<SavedReport>()
        for (i in 0 until array.length()) {
            parseReport(array.optJSONObject(i))?.let { reports.add(it) }
        }
        return reports.sortedByDescending { it.savedAtMillis }
    }

    fun saveDraft(state: InspectionFormState) {
        if (!state.hasSavableContent()) return
        upsertFromState(state, isDraft = true, exportFile = null, exportType = null)
    }

    fun saveExportedReport(state: InspectionFormState, exportFile: File, type: ReportShareType) {
        upsertFromState(state, isDraft = false, exportFile = exportFile, exportType = type)
    }

    fun deleteReport(id: String) {
        val array = readIndexArray()
        val kept = JSONArray()
        for (i in 0 until array.length()) {
            val entry = array.optJSONObject(i) ?: continue
            if (entry.optString("id") == id) {
                deleteFilesForEntry(entry)
            } else {
                kept.put(entry)
            }
        }
        writeIndexArray(kept)
    }

    fun pdfFile(report: SavedReport): File? =
        report.pdfFileName?.let { name -> File(savedDir, name).takeIf { it.exists() } }

    fun imageFile(report: SavedReport): File? =
        report.imageFileName?.let { name -> File(savedDir, name).takeIf { it.exists() } }

    private fun upsertFromState(
        state: InspectionFormState,
        isDraft: Boolean,
        exportFile: File?,
        exportType: ReportShareType?
    ) {
        val key = reportMatchKey(state)
        val array = readIndexArray()
        var existingIndex = -1
        for (i in 0 until array.length()) {
            val entry = array.optJSONObject(i)
            if (entry != null && entry.optString("matchKey") == key) {
                existingIndex = i
                break
            }
        }

        val now = System.currentTimeMillis()
        val id = if (existingIndex >= 0) {
            array.optJSONObject(existingIndex)?.optString("id") ?: newId(now)
        } else {
            newId(now)
        }

        val existing = if (existingIndex >= 0) array.optJSONObject(existingIndex) else null
        var pdfName = existing?.optString("pdfFileName").takeUnless { it.isNullOrBlank() }
        var imageName = existing?.optString("imageFileName").takeUnless { it.isNullOrBlank() }

        if (exportFile != null && exportType != null) {
            when (exportType) {
                ReportShareType.PDF -> {
                    pdfName = "$id.pdf"
                    exportFile.copyTo(File(savedDir, pdfName!!), overwrite = true)
                }
                ReportShareType.IMAGE -> {
                    imageName = "$id.jpg"
                    exportFile.copyTo(File(savedDir, imageName!!), overwrite = true)
                }
            }
        }

        val entry = JSONObject().apply {
            put("id", id)
            put("matchKey", key)
            put("customerName", state.customerInfo.customerName)
            put("customerPhone", state.customerInfo.customerPhone)
            put("vehicle", state.customerInfo.vehicle)
            put("mileage", state.customerInfo.mileage)
            put("generalNotes", state.generalNotes)
            put("sections", state.toJson().optJSONArray("sections") ?: JSONArray())
            put("savedAtMillis", now)
            val hasExport = pdfName != null || imageName != null
            put(
                "isDraft",
                when {
                    exportType != null -> false
                    hasExport -> false
                    else -> true
                }
            )
            if (pdfName != null) put("pdfFileName", pdfName)
            if (imageName != null) put("imageFileName", imageName)
        }

        if (existingIndex >= 0) {
            array.put(existingIndex, entry)
        } else {
            array.put(entry)
        }
        writeIndexArray(array)
        File(savedDir, "$id.json").writeText(state.toJson().toString())
    }

    private fun reportMatchKey(state: InspectionFormState): String {
        return listOf(
            state.customerInfo.customerName,
            state.customerInfo.customerPhone,
            state.customerInfo.vehicle
        ).joinToString("|") { normalizeKeyPart(it) }
    }

    private fun normalizeKeyPart(value: String): String =
        value.trim().lowercase().replace(Regex("\\s+"), " ")

    private fun newId(timestamp: Long): String = "report_$timestamp"

    private fun readIndexArray(): JSONArray {
        if (!indexFile.exists()) return JSONArray()
        return runCatching { JSONArray(indexFile.readText()) }.getOrDefault(JSONArray())
    }

    private fun writeIndexArray(array: JSONArray) {
        indexFile.writeText(array.toString())
    }

    private fun parseReport(json: JSONObject?): SavedReport? {
        if (json == null) return null
        val id = json.optString("id", "")
        if (id.isBlank()) return null

        val stateFile = File(savedDir, "$id.json")
        val formState = if (stateFile.exists()) {
            runCatching { JSONObject(stateFile.readText()).toInspectionFormState() }.getOrNull()
        } else {
            null
        } ?: JSONObject().apply {
            put(
                "customerInfo",
                JSONObject().apply {
                    put("customerName", json.optString("customerName", ""))
                    put("customerPhone", json.optString("customerPhone", ""))
                    put("vehicle", json.optString("vehicle", ""))
                    put("mileage", json.optString("mileage", ""))
                }
            )
            put("sections", json.optJSONArray("sections") ?: JSONArray())
            put("generalNotes", json.optString("generalNotes", ""))
        }.toInspectionFormState()

        return SavedReport(
            id = id,
            customerName = formState.customerInfo.customerName,
            customerPhone = formState.customerInfo.customerPhone,
            vehicle = formState.customerInfo.vehicle,
            mileage = formState.customerInfo.mileage,
            generalNotes = formState.generalNotes,
            sections = formState.sections,
            savedAtMillis = json.optLong("savedAtMillis", 0L),
            isDraft = json.optBoolean("isDraft", true),
            pdfFileName = json.optString("pdfFileName").takeIf { it.isNotBlank() },
            imageFileName = json.optString("imageFileName").takeIf { it.isNotBlank() }
        )
    }

    private fun deleteFilesForEntry(entry: JSONObject) {
        val id = entry.optString("id", "")
        entry.optString("pdfFileName").takeIf { it.isNotBlank() }?.let { File(savedDir, it).delete() }
        entry.optString("imageFileName").takeIf { it.isNotBlank() }?.let { File(savedDir, it).delete() }
        File(savedDir, "$id.json").delete()
    }
}
