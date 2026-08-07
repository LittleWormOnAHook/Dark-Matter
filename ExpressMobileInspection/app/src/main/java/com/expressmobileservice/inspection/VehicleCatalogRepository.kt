package com.expressmobileservice.inspection

import android.content.Context
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable
import kotlinx.serialization.builtins.ListSerializer
import kotlinx.serialization.builtins.serializer
import kotlinx.serialization.encodeToString
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

    suspend fun getMakes(category: VehicleCategory): List<String> = withContext(Dispatchers.IO) {
        if (category == VehicleCategory.JET_SKI) {
            return@withContext PwcVehicleCatalog.makes
        }
        val cacheKey = "makes_${category.name}"
        readCache(cacheKey)?.let { return@withContext it }
        val merged = linkedSetOf<String>()
        category.nhtsaTypes.forEach { type ->
            val encoded = URLEncoder.encode(type, "UTF-8")
            val url = "$BASE/GetMakesForVehicleType/$encoded?format=json"
            fetchNhtsa(url)?.results?.mapNotNull { it.makeName }?.let { merged.addAll(it) }
        }
        val sorted = merged.filter { it.isNotBlank() }.sorted()
        if (sorted.isNotEmpty()) writeCache(cacheKey, sorted)
        sorted.ifEmpty { fallbackMakes(category) }
    }

    suspend fun getModels(
        category: VehicleCategory,
        make: String,
        year: Int
    ): List<String> = withContext(Dispatchers.IO) {
        if (make.isBlank()) return@withContext emptyList()
        if (category == VehicleCategory.JET_SKI) {
            return@withContext PwcVehicleCatalog.modelsForMake(make)
        }
        val cacheKey = "models_${category.name}_${make}_$year"
        readCache(cacheKey)?.let { return@withContext it }
        val encodedMake = URLEncoder.encode(make, "UTF-8")
        val url = "$BASE/GetModelsForMakeYear/make/$encodedMake/modelyear/$year?format=json"
        val models = fetchNhtsa(url)?.results
            ?.mapNotNull { it.modelName }
            ?.filter { it.isNotBlank() }
            ?.distinct()
            ?.sorted()
            .orEmpty()
        if (models.isNotEmpty()) writeCache(cacheKey, models)
        models.ifEmpty { listOf("Other") }
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

    private fun readCache(key: String): List<String>? {
        val raw = prefs.getString(key, null) ?: return null
        return runCatching {
            json.decodeFromString(ListSerializer(String.serializer()), raw)
        }.getOrNull()
    }

    private fun writeCache(key: String, values: List<String>) {
        prefs.edit().putString(key, json.encodeToString(ListSerializer(String.serializer()), values)).apply()
    }

    private fun fetchNhtsa(urlString: String): NhtsaResponse? = try {
        val connection = (URL(urlString).openConnection() as HttpURLConnection).apply {
            connectTimeout = 12_000
            readTimeout = 12_000
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
        val results: List<NhtsaResult>? = null
    )

    @Serializable
    private data class NhtsaResult(
        @SerialName("Make_Name") val makeName: String? = null,
        @SerialName("Model_Name") val modelName: String? = null
    )

    companion object {
        private const val PREFS = "vehicle_catalog_cache"
        private const val BASE = "https://vpic.nhtsa.dot.gov/api/vehicles"
    }
}
