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

    private val engineSizesByMakeModel: Map<String, Map<String, List<String>>> = mapOf(
        "Sea-Doo" to mapOf(
            "Spark" to listOf("900cc"),
            "Spark Trixx" to listOf("900cc"),
            "GTI" to listOf("900cc", "1300cc", "1700cc"),
            "GTX" to listOf("1300cc", "1700cc"),
            "RXP" to listOf("1300cc", "1630cc"),
            "RXT" to listOf("1300cc", "1630cc"),
            "Wake" to listOf("900cc", "1300cc", "1700cc"),
            "Fish Pro" to listOf("1300cc", "1700cc")
        ),
        "Yamaha" to mapOf(
            "VX" to listOf("1049cc"),
            "FX" to listOf("1800cc"),
            "EX" to listOf("1049cc"),
            "SuperJet" to listOf("701cc", "1100cc"),
            "GP" to listOf("1200cc")
        ),
        "Kawasaki" to mapOf(
            "STX" to listOf("1498cc", "1603cc"),
            "Ultra" to listOf("1498cc", "1603cc"),
            "SX-R" to listOf("1498cc"),
            "750" to listOf("750cc"),
            "900" to listOf("900cc"),
            "1100" to listOf("1100cc")
        ),
        "Honda" to mapOf(
            "AquaTrax" to listOf("782cc", "1052cc", "1232cc")
        )
    )

    fun engineSizesFor(make: String, model: String): List<String> {
        val makeKey = engineSizesByMakeModel.keys.firstOrNull { it.equals(make, ignoreCase = true) }
            ?: return defaultPwcEngines(model)
        val models = engineSizesByMakeModel[makeKey].orEmpty()
        val exact = models.entries.firstOrNull { it.key.equals(model, ignoreCase = true) }?.value
        if (exact != null) return exact
        val partial = models.entries.firstOrNull { (key, _) ->
            model.contains(key, ignoreCase = true) || key.contains(model, ignoreCase = true)
        }?.value
        if (partial != null) return partial
        return defaultPwcEngines(model)
    }

    private fun defaultPwcEngines(model: String): List<String> {
        val parsed = Regex("""\b(\d{3,4})\b""").findAll(model)
            .mapNotNull { it.groupValues[1].toIntOrNull() }
            .filter { it in 400..2000 }
            .map { "${it}cc" }
            .distinct()
            .toList()
        return if (parsed.isNotEmpty()) parsed else listOf(
            "550cc", "650cc", "717cc", "785cc", "900cc", "1000cc", "1100cc", "1200cc",
            "1300cc", "1500cc", "1600cc", "1800cc"
        )
    }
}
