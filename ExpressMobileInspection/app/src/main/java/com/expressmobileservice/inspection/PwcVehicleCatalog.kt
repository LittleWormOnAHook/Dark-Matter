package com.expressmobileservice.inspection

/**
 * US personal watercraft (jet ski) makes/models sold in America (1970–2026).
 * NHTSA does not catalog PWCs; this supplements the NHTSA API for cars/trucks/motorcycles.
 */
object PwcVehicleCatalog {
    val makes: List<String> = listOf(
        "Sea-Doo",
        "Yamaha",
        "Kawasaki",
        "Honda",
        "Polaris",
        "Tiger Shark",
        "Wet Jet",
        "Arctic Cat",
        "Other"
    )

    private val modelsByMake: Map<String, List<String>> = mapOf(
        "Sea-Doo" to listOf(
            "SP", "SPI", "SPX", "GTI", "GTI SE", "GTX", "GTX Limited", "RXP", "RXP-X",
            "RXT", "RXT-X", "Wake", "Wake Pro", "Fish Pro", "Spark", "Spark Trixx",
            "GTR", "GTX Pro", "Explorer Pro", "Switch", "Other"
        ),
        "Yamaha" to listOf(
            "WaveRunner", "VX", "VX Cruiser", "VX Deluxe", "VX Limited", "FX",
            "FX Cruiser", "FX HO", "FX SVHO", "GP", "GP800", "SuperJet", "EX",
            "EX Sport", "EX Deluxe", "JetBlaster", "Other"
        ),
        "Kawasaki" to listOf(
            "JS", "JS550", "JS650", "750 SS", "750 SX", "900 ZXi", "1100 ZXi",
            "STX", "STX-15F", "STX-160", "STX-160X", "STX-160LX", "Ultra 150",
            "Ultra 250X", "Ultra 310", "Ultra 310LX", "Ultra 310R", "SX-R", "Other"
        ),
        "Honda" to listOf(
            "AquaTrax", "F-12", "F-12X", "F-15", "F-15X", "R-12X", "Other"
        ),
        "Polaris" to listOf(
            "SL", "SLH", "SLTX", "Pro 785", "Genesis", "Virage", "Virage TX",
            "MSX 140", "MSX 150", "MSX 110", "Other"
        ),
        "Tiger Shark" to listOf(
            "640", "770", "900", "1000", "Monte Carlo", "Daytona", "Other"
        ),
        "Wet Jet" to listOf("440", "500", "Other"),
        "Arctic Cat" to listOf("Tigershark", "Other"),
        "Other" to listOf("Other", "Custom")
    )

    fun modelsForMake(make: String): List<String> =
        modelsByMake[make] ?: listOf("Other", "Custom")
}
