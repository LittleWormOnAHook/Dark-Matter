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
    val customerContact: String = "",
    val vehicleYearMakeModel: String = "",
    val vin: String = "",
    val mileage: String = "",
    val licensePlate: String = "",
    val technicianName: String = ""
)

data class InspectionFormState(
    val customerInfo: CustomerInfo = CustomerInfo(),
    val sections: List<InspectionSection> = defaultInspectionSections()
)

fun defaultInspectionSections(): List<InspectionSection> = listOf(
    InspectionSection(
        title = "Exterior & Visibility",
        items = listOf(
            item("dashboard_lights", "Dashboard Warning Lights"),
            item("windshield", "Windshield (cracks/chips)"),
            item("wiper_blades", "Wiper Blades"),
            item("body", "Body (dents/rust/structural)"),
            item("headlights", "Headlights"),
            item("taillights", "Taillights & Brake Lights"),
            item("turn_signals", "Turn Signals & Marker Lights"),
            item("license_lamp", "License Plate Lamp")
        )
    ),
    InspectionSection(
        title = "Fluids & Filters",
        items = listOf(
            item("engine_oil", "Engine Oil Level & Condition"),
            item("transmission_fluid", "Transmission Fluid"),
            item("brake_fluid", "Brake Fluid Level"),
            item("coolant", "Coolant Level & Condition"),
            item("power_steering", "Power Steering Fluid"),
            item("washer_fluid", "Washer Fluid"),
            item("engine_air_filter", "Engine Air Filter"),
            item("cabin_air_filter", "Cabin Air Filter")
        )
    ),
    InspectionSection(
        title = "Under Hood",
        items = listOf(
            item("battery", "Battery (charge/cables/corrosion)"),
            item("coolant_hoses", "Coolant Hoses"),
            item("drive_belts", "Drive Belts"),
            item("belt_tensioner", "Belt Tensioner"),
            item("exhaust", "Exhaust System"),
            item("master_cylinder", "Master Cylinder")
        )
    ),
    InspectionSection(
        title = "Tires & Wheels",
        items = listOf(
            item("tire_tread", "Tire Tread Depth"),
            item("tire_pressure", "Tire Pressure"),
            item("tire_wear", "Tire Wear Pattern"),
            item("wheel_alignment", "Wheel Alignment")
        )
    ),
    InspectionSection(
        title = "Brake System",
        items = listOf(
            item("brake_pads", "Brake Pads"),
            item("brake_rotors", "Brake Rotors"),
            item("brake_lines", "Brake Hoses/Lines"),
            item("brake_hardware", "Brake Hardware/Adjuster"),
            item("parking_brake", "Parking Brake & Cables")
        )
    ),
    InspectionSection(
        title = "Steering & Suspension",
        items = listOf(
            item("shocks_struts", "Shocks & Struts"),
            item("ball_joints", "Ball Joints / Control Arms"),
            item("cv_joints", "CV Joints / Boots"),
            item("tie_rods", "Tie Rod Ends"),
            item("rack_pinion", "Rack & Pinion / Steering"),
            item("bushings", "Bushings / Link Pins"),
            item("seals_bearings", "Seals & Bearings")
        )
    )
)

private fun item(id: String, label: String) = InspectionItem(id = id, label = label)

const val COMPANY_NAME = "Express Mobile Service"
const val COMPANY_PHONE = "904-514-2885"
