package com.expressmobileservice.inspection

/**
 * Engine / displacement options keyed by make and model.
 * Falls back to category defaults when no specific match exists.
 */
object VehicleEngineCatalog {

    fun getOptions(
        category: VehicleCategory,
        make: String,
        model: String
    ): List<String> {
        if (make.isBlank()) return emptyList()
        val normalizedMake = make.trim()
        val normalizedModel = model.trim()

        return when (category) {
            VehicleCategory.JET_SKI -> {
                if (normalizedModel.isBlank()) {
                    EngineSizeOptions.forCategory(category)
                } else {
                    PwcVehicleCatalog.engineSizesFor(normalizedMake, normalizedModel)
                }
            }
            VehicleCategory.MOTORCYCLE -> motorcycleOptions(normalizedMake, normalizedModel)
            VehicleCategory.CAR_TRUCK -> carTruckOptions(normalizedMake, normalizedModel)
        }.distinct().sortedWith(engineSizeComparator).let { list ->
            if (list.isEmpty()) EngineSizeOptions.forCategory(category)
            else if ("Other" !in list) list + "Other" else list
        }
    }

    private fun motorcycleOptions(make: String, model: String): List<String> {
        val fromTable = lookup(make, model, motorcycleByMakeModel)
        if (fromTable.isNotEmpty()) return fromTable

        val fromMake = motorcycleByMake[make].orEmpty()
        if (fromMake.isNotEmpty() && model.isBlank()) return fromMake

        val parsed = parseDisplacementFromName(model, isMotorcycle = true)
        if (parsed.isNotEmpty()) return parsed

        if (model.isNotBlank()) return EngineSizeOptions.forCategory(VehicleCategory.MOTORCYCLE)
        return motorcycleByMake[make] ?: EngineSizeOptions.forCategory(VehicleCategory.MOTORCYCLE)
    }

    private fun carTruckOptions(make: String, model: String): List<String> {
        val fromTable = lookup(make, model, carTruckByMakeModel)
        if (fromTable.isNotEmpty()) return fromTable

        val fromMake = carTruckByMake[make].orEmpty()
        if (fromMake.isNotEmpty() && model.isBlank()) return fromMake

        val parsed = parseDisplacementFromName(model, isMotorcycle = false)
        if (parsed.isNotEmpty()) return parsed

        if (model.isNotBlank()) return EngineSizeOptions.forCategory(VehicleCategory.CAR_TRUCK)
        return carTruckByMake[make] ?: EngineSizeOptions.forCategory(VehicleCategory.CAR_TRUCK)
    }

    private fun lookup(
        make: String,
        model: String,
        table: Map<String, Map<String, List<String>>>
    ): List<String> {
        if (model.isBlank()) return emptyList()
        val makeKey = table.keys.firstOrNull { it.equals(make, ignoreCase = true) } ?: return emptyList()
        val models = table[makeKey] ?: return emptyList()
        val exact = models.entries.firstOrNull { it.key.equals(model, ignoreCase = true) }?.value
        if (exact != null) return exact
        return models.entries
            .firstOrNull { (key, _) -> model.contains(key, ignoreCase = true) || key.contains(model, ignoreCase = true) }
            ?.value
            .orEmpty()
    }

    private fun parseDisplacementFromName(name: String, isMotorcycle: Boolean): List<String> {
        if (name.isBlank()) return emptyList()
        val found = linkedSetOf<String>()
        Regex("""(\d+(?:\.\d+)?)\s*[Ll]""").findAll(name).forEach { match ->
            found.add("${match.groupValues[1]}L")
        }
        Regex("""\b(\d{2,4})\b""").findAll(name).forEach { match ->
            val value = match.groupValues[1].toIntOrNull() ?: return@forEach
            when {
                isMotorcycle && value in 50..2000 -> found.add("${value}cc")
                !isMotorcycle && value in 10..99 && name.contains("V$value", ignoreCase = true) ->
                    found.add("V$value")
            }
        }
        if (!isMotorcycle) {
            when {
                name.contains("EcoBoost", ignoreCase = true) -> found.addAll(listOf("2.0L", "2.3L", "2.7L", "3.5L"))
                name.contains("Hybrid", ignoreCase = true) -> found.add("Hybrid")
                name.contains("Electric", ignoreCase = true) || name.contains("EV", ignoreCase = true) ->
                    found.add("Electric")
                name.contains("Diesel", ignoreCase = true) -> found.add("Diesel")
            }
        }
        return found.toList()
    }

    private val engineSizeComparator = Comparator<String> { a, b ->
        val aNum = a.filter { it.isDigit() || it == '.' }.toDoubleOrNull()
        val bNum = b.filter { it.isDigit() || it == '.' }.toDoubleOrNull()
        when {
            aNum != null && bNum != null -> aNum.compareTo(bNum)
            a == "Other" -> 1
            b == "Other" -> -1
            else -> a.compareTo(b, ignoreCase = true)
        }
    }

    private val carTruckByMake: Map<String, List<String>> = mapOf(
        "Ford" to listOf("2.0L", "2.3L", "2.7L", "3.0L", "3.3L", "3.5L", "5.0L", "6.7L", "7.3L", "Electric", "Hybrid"),
        "Chevrolet" to listOf("1.5L", "2.0L", "2.5L", "3.0L", "3.6L", "4.3L", "5.3L", "6.2L", "6.6L", "Electric"),
        "Toyota" to listOf("1.8L", "2.0L", "2.4L", "2.5L", "3.5L", "4.0L", "Hybrid", "Electric"),
        "Honda" to listOf("1.5L", "1.8L", "2.0L", "2.4L", "3.0L", "3.5L", "Hybrid", "Electric"),
        "Ram" to listOf("3.0L", "3.6L", "5.7L", "6.4L", "6.7L", "Diesel"),
        "GMC" to listOf("2.7L", "3.0L", "3.6L", "5.3L", "6.2L", "6.6L", "Diesel", "Electric"),
        "Jeep" to listOf("2.0L", "2.4L", "3.0L", "3.6L", "5.7L", "6.4L", "4xe Hybrid"),
        "Nissan" to listOf("2.0L", "2.5L", "3.5L", "3.8L", "5.6L", "Electric"),
        "BMW" to listOf("2.0L", "3.0L", "4.4L", "Electric", "Hybrid"),
        "Mercedes-Benz" to listOf("2.0L", "3.0L", "4.0L", "Electric", "Hybrid")
    )

    private val motorcycleByMake: Map<String, List<String>> = mapOf(
        "Harley-Davidson" to listOf("883cc", "1200cc", "1250cc", "1313cc", "1745cc"),
        "Honda" to listOf("125cc", "250cc", "300cc", "500cc", "600cc", "750cc", "1000cc", "1100cc", "1300cc", "1800cc"),
        "Yamaha" to listOf("125cc", "250cc", "400cc", "600cc", "700cc", "900cc", "1000cc", "1300cc", "1700cc"),
        "Kawasaki" to listOf("250cc", "400cc", "500cc", "650cc", "900cc", "1000cc", "1400cc"),
        "Suzuki" to listOf("250cc", "400cc", "600cc", "750cc", "1000cc", "1300cc"),
        "Ducati" to listOf("400cc", "600cc", "800cc", "900cc", "1000cc", "1100cc", "1200cc", "1300cc"),
        "Indian" to listOf("1113cc", "1167cc", "1811cc"),
        "Triumph" to listOf("400cc", "660cc", "765cc", "900cc", "1200cc"),
        "KTM" to listOf("125cc", "250cc", "390cc", "450cc", "500cc", "690cc", "790cc", "890cc", "1290cc")
    )

    private val carTruckByMakeModel: Map<String, Map<String, List<String>>> = mapOf(
        "Ford" to mapOf(
            "F-150" to listOf("2.7L", "3.3L", "3.5L", "5.0L", "3.5L Hybrid"),
            "Mustang" to listOf("2.3L", "5.0L", "5.2L", "Electric"),
            "Explorer" to listOf("2.3L", "3.0L", "3.3L"),
            "Escape" to listOf("1.5L", "2.0L", "2.5L", "Hybrid", "PHEV"),
            "Ranger" to listOf("2.3L", "2.7L"),
            "Bronco" to listOf("2.3L", "2.7L", "3.0L"),
            "Edge" to listOf("2.0L", "2.7L"),
            "Expedition" to listOf("3.5L")
        ),
        "Chevrolet" to mapOf(
            "Silverado" to listOf("2.7L", "3.0L Diesel", "5.3L", "6.2L"),
            "Colorado" to listOf("2.7L", "3.6L"),
            "Camaro" to listOf("2.0L", "3.6L", "6.2L"),
            "Corvette" to listOf("6.2L", "5.5L"),
            "Equinox" to listOf("1.5L", "2.0L"),
            "Tahoe" to listOf("5.3L", "6.2L", "3.0L Diesel")
        ),
        "Toyota" to mapOf(
            "Camry" to listOf("2.5L", "3.5L", "Hybrid"),
            "Corolla" to listOf("1.8L", "2.0L", "Hybrid"),
            "RAV4" to listOf("2.5L", "Hybrid", "PHEV"),
            "Tacoma" to listOf("2.4L", "2.7L", "3.5L"),
            "Tundra" to listOf("3.4L Twin-Turbo", "3.5L Hybrid"),
            "4Runner" to listOf("2.4L Turbo", "4.0L")
        ),
        "Honda" to mapOf(
            "Civic" to listOf("1.5L", "2.0L", "Hybrid"),
            "Accord" to listOf("1.5L", "2.0L", "Hybrid"),
            "CR-V" to listOf("1.5L", "2.0L", "Hybrid"),
            "Pilot" to listOf("3.5L"),
            "Ridgeline" to listOf("3.5L")
        ),
        "Ram" to mapOf(
            "1500" to listOf("3.0L Diesel", "3.6L", "5.7L"),
            "2500" to listOf("6.4L", "6.7L Diesel"),
            "3500" to listOf("6.4L", "6.7L Diesel")
        )
    )

    private val motorcycleByMakeModel: Map<String, Map<String, List<String>>> = mapOf(
        "Honda" to mapOf(
            "CBR600" to listOf("600cc"),
            "CBR1000" to listOf("1000cc"),
            "Rebel 500" to listOf("500cc"),
            "Gold Wing" to listOf("1833cc"),
            "Africa Twin" to listOf("1084cc")
        ),
        "Harley-Davidson" to mapOf(
            "Sportster" to listOf("883cc", "1200cc"),
            "Softail" to listOf("107ci", "114ci", "117ci"),
            "Touring" to listOf("107ci", "114ci", "117ci")
        ),
        "Yamaha" to mapOf(
            "YZF-R6" to listOf("600cc"),
            "YZF-R1" to listOf("1000cc"),
            "MT-07" to listOf("689cc"),
            "MT-09" to listOf("890cc")
        )
    )
}
