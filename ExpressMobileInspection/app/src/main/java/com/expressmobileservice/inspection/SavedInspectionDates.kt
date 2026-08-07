package com.expressmobileservice.inspection

/**
 * Date used for saved-inspection list display and sorting.
 * Linked appointments use the job start time; ad-hoc inspections use save time.
 */
fun SavedInspection.displaySortMillis(appointmentStore: AppointmentStore? = null): Long {
    if (inspectionDateMillis > 0L) return inspectionDateMillis
    appointmentId?.let { id ->
        appointmentStore?.getById(id)?.let { return it.startEpochMillis }
    }
    return updatedAtMillis
}

fun List<SavedInspection>.sortedEarliestFirst(
    appointmentStore: AppointmentStore? = null
): List<SavedInspection> = sortedBy { it.displaySortMillis(appointmentStore) }
