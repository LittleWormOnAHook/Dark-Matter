package com.expressmobileservice.inspection

data class SavedReport(
    val id: String,
    val customerName: String,
    val customerPhone: String,
    val vehicle: String,
    val mileage: String,
    val generalNotes: String,
    val sections: List<InspectionSection>,
    val savedAtMillis: Long,
    val isDraft: Boolean,
    val pdfFileName: String?,
    val imageFileName: String?
) {
    fun toFormState(): InspectionFormState = InspectionFormState(
        customerInfo = CustomerInfo(
            customerName = customerName,
            customerPhone = customerPhone,
            vehicle = vehicle,
            mileage = mileage
        ),
        sections = sections,
        generalNotes = generalNotes
    )

    val displayTitle: String = customerName.ifBlank { "Unnamed customer" }

    val displaySubtitle: String = buildList {
        if (customerPhone.isNotBlank()) add(customerPhone)
        if (vehicle.isNotBlank()) add(vehicle)
    }.joinToString(" • ")
}
