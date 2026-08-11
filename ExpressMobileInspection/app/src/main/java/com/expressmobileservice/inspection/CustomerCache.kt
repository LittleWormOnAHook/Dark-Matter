package com.expressmobileservice.inspection

data class CustomerRecord(
    val customerName: String,
    val customerPhone: String,
    val address: String,
    val vehicleCategory: String,
    val vehicleYear: Int?,
    val vehicleMake: String,
    val vehicleModel: String,
    val engineSize: String,
    val mileage: String,
    val lastUsedMillis: Long
) {
    fun displayLabel(): String = buildString {
        if (customerName.isNotBlank()) append(customerName)
        if (customerPhone.isNotBlank()) {
            if (isNotEmpty()) append(" · ")
            append(customerPhone)
        }
        if (address.isNotBlank()) {
            if (isNotEmpty()) append(" · ")
            append(address)
        }
    }.ifBlank { "Customer" }

    fun toAppointmentFields(): Appointment = Appointment(
        customerName = customerName,
        customerPhone = customerPhone,
        address = address,
        vehicleCategory = vehicleCategory,
        vehicleYear = vehicleYear,
        vehicleMake = vehicleMake,
        vehicleModel = vehicleModel,
        engineSize = engineSize,
        mileage = mileage
    )
}

private fun normalizePhone(phone: String): String = phone.filter { it.isDigit() }

private fun customerKey(appointment: Appointment): String? = when {
    appointment.customerPhone.isNotBlank() -> "phone:${normalizePhone(appointment.customerPhone)}"
    appointment.customerName.isNotBlank() -> "name:${appointment.customerName.lowercase()}"
    else -> null
}

fun List<Appointment>.distinctCustomerRecords(): List<CustomerRecord> =
    sortedByDescending { it.startEpochMillis }
        .mapNotNull { apt ->
            val key = customerKey(apt)
            if (key == null) return@mapNotNull null
            key to CustomerRecord(
                customerName = apt.customerName,
                customerPhone = apt.customerPhone,
                address = apt.address,
                vehicleCategory = apt.vehicleCategory,
                vehicleYear = apt.vehicleYear,
                vehicleMake = apt.vehicleMake,
                vehicleModel = apt.vehicleModel,
                engineSize = apt.engineSize,
                mileage = apt.mileage,
                lastUsedMillis = apt.startEpochMillis
            )
        }
        .distinctBy { it.first }
        .map { it.second }
        .sortedByDescending { it.lastUsedMillis }

fun List<CustomerRecord>.searchCustomers(query: String, limit: Int = 8): List<CustomerRecord> {
    val q = query.trim().lowercase()
    if (q.isBlank()) return take(limit)
    val qDigits = normalizePhone(query)
    return filter { record ->
        record.customerName.lowercase().contains(q) ||
            record.customerPhone.contains(q) ||
            (qDigits.length >= 3 && normalizePhone(record.customerPhone).contains(qDigits)) ||
            record.address.lowercase().contains(q)
    }.take(limit)
}
