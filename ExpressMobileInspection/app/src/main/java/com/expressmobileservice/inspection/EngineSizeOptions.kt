package com.expressmobileservice.inspection

object EngineSizeOptions {
    fun forCategory(category: VehicleCategory): List<String> = when (category) {
        VehicleCategory.CAR_TRUCK -> listOf(
            "1.0L", "1.5L", "1.6L", "1.8L", "2.0L", "2.4L", "2.5L", "3.0L", "3.5L",
            "3.6L", "4.0L", "4.6L", "5.0L", "5.3L", "5.7L", "6.0L", "6.2L", "6.7L",
            "7.3L", "8.0L", "Electric", "Hybrid", "Diesel", "Other"
        )
        VehicleCategory.MOTORCYCLE -> listOf(
            "50cc", "125cc", "150cc", "250cc", "300cc", "400cc", "500cc", "600cc",
            "650cc", "750cc", "800cc", "900cc", "1000cc", "1100cc", "1200cc", "1300cc",
            "1400cc", "1800cc", "Other"
        )
        VehicleCategory.JET_SKI -> listOf(
            "550cc", "650cc", "717cc", "785cc", "900cc", "1000cc", "1100cc", "1200cc",
            "1300cc", "1500cc", "1600cc", "1800cc", "Other"
        )
    }
}
