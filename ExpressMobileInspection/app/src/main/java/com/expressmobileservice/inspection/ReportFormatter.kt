package com.expressmobileservice.inspection

import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

object ReportFormatter {

    private val dateFormat = SimpleDateFormat("MMMM d, yyyy 'at' h:mm a", Locale.US)

    fun formatReport(state: InspectionFormState): String {
        val info = state.customerInfo
        val builder = StringBuilder()

        builder.appendLine(COMPANY_NAME)
        builder.appendLine("Phone: $COMPANY_PHONE")
        builder.appendLine("Vehicle Inspection Report")
        builder.appendLine("Generated: ${dateFormat.format(Date())}")
        builder.appendLine()
        builder.appendLine("--- Customer & Vehicle ---")
        builder.appendLine("Customer: ${info.customerName.ifBlank { "—" }}")
        builder.appendLine("Contact: ${info.customerContact.ifBlank { "—" }}")
        builder.appendLine("Vehicle: ${info.vehicleYearMakeModel.ifBlank { "—" }}")
        builder.appendLine("VIN: ${info.vin.ifBlank { "—" }}")
        builder.appendLine("Mileage: ${info.mileage.ifBlank { "—" }}")
        builder.appendLine("License Plate: ${info.licensePlate.ifBlank { "—" }}")
        builder.appendLine("Technician: ${info.technicianName.ifBlank { "—" }}")
        builder.appendLine()

        state.sections.forEach { section ->
            builder.appendLine("=== ${section.title} ===")
            section.items.forEach { item ->
                val statusLabel = when (item.status) {
                    InspectionStatus.GOOD -> "GOOD"
                    InspectionStatus.BAD -> "BAD"
                    InspectionStatus.REPLACE -> "REPLACE"
                    InspectionStatus.NONE -> "NOT CHECKED"
                }
                builder.appendLine("• ${item.label}")
                builder.appendLine("  Status: $statusLabel")
                if (item.notes.isNotBlank()) {
                    builder.appendLine("  Notes: ${item.notes}")
                }
            }
            builder.appendLine()
        }

        val summary = summarize(state)
        builder.appendLine("--- Summary ---")
        builder.appendLine("Good: ${summary.good}")
        builder.appendLine("Bad: ${summary.bad}")
        builder.appendLine("Replace: ${summary.replace}")
        builder.appendLine("Not checked: ${summary.unchecked}")
        builder.appendLine()
        builder.appendLine("Thank you for choosing $COMPANY_NAME.")
        builder.appendLine("Questions? Call $COMPANY_PHONE")

        return builder.toString().trimEnd()
    }

    private fun summarize(state: InspectionFormState): SummaryCounts {
        var good = 0
        var bad = 0
        var replace = 0
        var unchecked = 0

        state.sections.flatMap { it.items }.forEach { item ->
            when (item.status) {
                InspectionStatus.GOOD -> good++
                InspectionStatus.BAD -> bad++
                InspectionStatus.REPLACE -> replace++
                InspectionStatus.NONE -> unchecked++
            }
        }

        return SummaryCounts(good, bad, replace, unchecked)
    }

    private data class SummaryCounts(
        val good: Int,
        val bad: Int,
        val replace: Int,
        val unchecked: Int
    )
}
