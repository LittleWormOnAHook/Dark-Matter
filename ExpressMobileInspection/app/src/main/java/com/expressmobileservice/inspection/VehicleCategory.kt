package com.expressmobileservice.inspection

enum class VehicleCategory(val label: String, val nhtsaTypes: List<String>) {
    CAR_TRUCK("Car / Truck", listOf("car", "truck", "multipurpose passenger vehicle (mpv)")),
    MOTORCYCLE("Motorcycle", listOf("motorcycle")),
    JET_SKI("Jet Ski / PWC", emptyList());

    companion object {
        val yearRange: IntRange = 1970..2026
    }
}

fun Appointment.vehicleSummary(): String = buildString {
    if (vehicleYear != null) append("$vehicleYear ")
    if (vehicleMake.isNotBlank()) append("$vehicleMake ")
    if (vehicleModel.isNotBlank()) append(vehicleModel)
    if (engineSize.isNotBlank()) {
        if (isNotEmpty()) append(" · ")
        append(engineSize)
    }
}.trim()
