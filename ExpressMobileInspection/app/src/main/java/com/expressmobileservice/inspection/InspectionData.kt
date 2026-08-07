package com.expressmobileservice.inspection

enum class InspectionStatus {
    NONE,
    GOOD,
    BAD,
    REPLACE
}

data class InspectionItem(
    val id: String,
    val label: String,
    val status: InspectionStatus = InspectionStatus.NONE,
    val notes: String = ""
)

data class InspectionSection(
    val title: String,
    val items: List<InspectionItem>
)

data class CustomerInfo(
    val customerName: String = "",
    val customerPhone: String = "",
    val vehicle: String = "",
    val mileage: String = ""
)

data class InspectionFormState(
    val customerInfo: CustomerInfo = CustomerInfo(),
    val sections: List<InspectionSection> = defaultInspectionSections()
)

fun defaultInspectionSections(): List<InspectionSection> = listOf(
    InspectionSection(
        title = "Fluids & Engine",
        items = listOf(
            item("engine_oil", "Engine Oil"),
            item("coolant", "Coolant"),
            item("brake_fluid", "Brake Fluid"),
            item("transmission_fluid", "Transmission Fluid"),
            item("battery", "Battery"),
            item("air_filter", "Air Filter")
        )
    ),
    InspectionSection(
        title = "Brakes & Tires",
        items = listOf(
            item("brake_pads", "Brake Pads"),
            item("brake_rotors", "Brake Rotors"),
            item("tire_tread", "Tire Tread"),
            item("tire_pressure", "Tire Pressure")
        )
    ),
    InspectionSection(
        title = "Lights & Exterior",
        items = listOf(
            item("headlights", "Headlights"),
            item("taillights", "Taillights / Brake Lights"),
            item("turn_signals", "Turn Signals"),
            item("wipers", "Wiper Blades"),
            item("windshield", "Windshield")
        )
    ),
    InspectionSection(
        title = "Steering & Undercarriage",
        items = listOf(
            item("shocks", "Shocks / Struts"),
            item("steering", "Steering / Tie Rods"),
            item("exhaust", "Exhaust System"),
            item("belts_hoses", "Belts / Hoses")
        )
    )
)

private fun item(id: String, label: String) = InspectionItem(id = id, label = label)

const val COMPANY_NAME = "Express Mobile Service"
const val COMPANY_PHONE = "904-514-2885"
