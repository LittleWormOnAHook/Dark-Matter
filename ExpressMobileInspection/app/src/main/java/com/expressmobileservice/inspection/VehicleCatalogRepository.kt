package com.expressmobileservice.inspection

import android.content.Context
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable
import kotlinx.serialization.builtins.ListSerializer
import kotlinx.serialization.builtins.serializer
import kotlinx.serialization.json.Json
import java.net.HttpURLConnection
import java.net.URL
import java.net.URLEncoder

/**
 * US vehicle makes/models via NHTSA vPIC (cars, trucks, motorcycles) plus local PWC data.
 * Years: 1970–2026.
 */
class VehicleCatalogRepository(context: Context) {

    private val prefs = context.applicationContext.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
    private val json = Json { ignoreUnknownKeys = true }

    init {
        clearLegacyCache()
    }

    suspend fun getMakes(category: VehicleCategory): List<String> = withContext(Dispatchers.IO) {
        if (category == VehicleCategory.JET_SKI) {
            return@withContext PwcVehicleCatalog.makes
        }
        val cacheKey = "makes_v3_${category.name}"
        readCache(cacheKey)?.let { return@withContext it }
        val merged = linkedSetOf<String>()
        category.nhtsaTypes.forEach { type ->
            val encoded = URLEncoder.encode(type, "UTF-8")
            val url = "$BASE/GetMakesForVehicleType/$encoded?format=json"
            fetchNhtsa(url)?.results?.mapNotNull { it.resolvedMakeName }?.let { names ->
                merged.addAll(names.map(::formatVehicleLabel))
            }
        }
        val sorted = merged.filter { it.isNotBlank() }.sorted()
        if (sorted.isNotEmpty()) writeCache(cacheKey, sorted)
        sorted.ifEmpty { fallbackMakes(category) }
    }

    suspend fun getModels(
        category: VehicleCategory,
        make: String,
        year: Int?
    ): List<String> = withContext(Dispatchers.IO) {
        if (make.isBlank()) return@withContext emptyList()
        if (category == VehicleCategory.JET_SKI) {
            return@withContext PwcVehicleCatalog.modelsForMake(make)
        }

        val cacheKey = "models_v3_${category.name}_${make}_${year ?: "all"}"
        readCache(cacheKey)?.let { return@withContext it }

        val merged = linkedSetOf<String>()
        val encodedMake = URLEncoder.encode(make, "UTF-8")

        if (year != null) {
            category.nhtsaTypes.forEach { type ->
                val encodedType = URLEncoder.encode(type, "UTF-8")
                val url =
                    "$BASE/GetModelsForMakeYear/make/$encodedMake/modelyear/$year/vehicletype/$encodedType?format=json"
                fetchModelNames(url)?.let { merged.addAll(it) }
            }
            fetchModelsForMakeYear(make, year)?.let { merged.addAll(it) }
        }

        if (merged.isEmpty()) {
            category.nhtsaTypes.forEach { type ->
                val encodedType = URLEncoder.encode(type, "UTF-8")
                val recentYears = listOf(
                    VehicleCategory.yearRange.last,
                    VehicleCategory.yearRange.last - 1,
                    VehicleCategory.yearRange.last - 2
                )
                recentYears.forEach { recentYear ->
                    val url =
                        "$BASE/GetModelsForMakeYear/make/$encodedMake/modelyear/$recentYear/vehicletype/$encodedType?format=json"
                    fetchModelNames(url)?.let { merged.addAll(it) }
                }
            }
            fetchModelsForMake(make)?.let { merged.addAll(it) }
        }

        val models = merged.filter { it.isNotBlank() }.sorted()
        if (models.isNotEmpty()) writeCache(cacheKey, models)
        models.ifEmpty { listOf("Other") }
    }

    private fun fetchModelsForMakeYear(make: String, year: Int): List<String>? {
        val encodedMake = URLEncoder.encode(make, "UTF-8")
        val url = "$BASE/GetModelsForMakeYear/make/$encodedMake/modelyear/$year?format=json"
        return fetchModelNames(url)
    }

    private fun fetchModelsForMake(make: String): List<String>? {
        val encodedMake = URLEncoder.encode(make, "UTF-8")
        val url = "$BASE/GetModelsForMake/$encodedMake?format=json"
        return fetchModelNames(url)
    }

    private fun fetchModelNames(url: String): List<String>? {
        val models = fetchNhtsa(url)?.results
            ?.mapNotNull { it.modelName }
            ?.map(::formatVehicleLabel)
            ?.filter { it.isNotBlank() }
            ?: return null
        return models.distinct().sorted().takeIf { it.isNotEmpty() }
    }

    private fun fallbackMakes(category: VehicleCategory): List<String> = when (category) {
        VehicleCategory.CAR_TRUCK -> listOf(
            "Ford", "Chevrolet", "Toyota", "Honda", "Nissan", "Dodge", "GMC", "Jeep",
            "Ram", "Hyundai", "Kia", "BMW", "Mercedes-Benz", "Volkswagen", "Subaru", "Other"
        )
        VehicleCategory.MOTORCYCLE -> listOf(
            "Harley-Davidson", "Honda", "Yamaha", "Kawasaki", "Suzuki", "BMW", "Ducati",
            "Indian", "Triumph", "KTM", "Polaris", "Can-Am", "Other"
        )
        VehicleCategory.JET_SKI -> PwcVehicleCatalog.makes
    }

    private fun clearLegacyCache() {
        val legacyPrefixes = listOf("makes_v2_", "models_v2_")
        prefs.edit().apply {
            prefs.all.keys.forEach { key ->
                if (legacyPrefixes.any { key.startsWith(it) }) remove(key)
            }
        }.apply()
    }

    private fun readCache(key: String): List<String>? {
        val raw = prefs.getString(key, null) ?: return null
        val cached = runCatching {
            json.decodeFromString(ListSerializer(String.serializer()), raw)
        }.getOrNull() ?: return null
        if (cached.size == 1 && cached.single() == "Other") return null
        return cached
    }

    private fun writeCache(key: String, values: List<String>) {
        if (values.isEmpty() || (values.size == 1 && values.single() == "Other")) return
        prefs.edit().putString(key, json.encodeToString(ListSerializer(String.serializer()), values)).apply()
    }

    private fun fetchNhtsa(urlString: String): NhtsaResponse? = try {
        val connection = (URL(urlString).openConnection() as HttpURLConnection).apply {
            connectTimeout = 15_000
            readTimeout = 15_000
            requestMethod = "GET"
        }
        connection.inputStream.bufferedReader().use { reader ->
            json.decodeFromString<NhtsaResponse>(reader.readText())
        }
    } catch (_: Exception) {
        null
    }

    @Serializable
    private data class NhtsaResponse(
        @SerialName("Results") val results: List<NhtsaResult>? = null
    )

    @Serializable
    private data class NhtsaResult(
        @SerialName("MakeName") val makeName: String? = null,
        @SerialName("Make_Name") val makeNameAlt: String? = null,
        @SerialName("Model_Name") val modelName: String? = null
    ) {
        val resolvedMakeName: String?
            get() = makeName?.takeIf { it.isNotBlank() } ?: makeNameAlt?.takeIf { it.isNotBlank() }
    }

    companion object {
        private const val PREFS = "vehicle_catalog_cache"
        private const val BASE = "https://vpic.nhtsa.dot.gov/api/vehicles"
    }
}
