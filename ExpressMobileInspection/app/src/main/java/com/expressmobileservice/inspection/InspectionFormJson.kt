package com.expressmobileservice.inspection

import org.json.JSONArray
import org.json.JSONObject

fun InspectionFormState.toJson(): JSONObject {
    val customer = JSONObject().apply {
        put("customerName", customerInfo.customerName)
        put("customerPhone", customerInfo.customerPhone)
        put("vehicle", customerInfo.vehicle)
        put("mileage", customerInfo.mileage)
    }
    val sectionsArray = JSONArray()
    sections.forEach { section ->
        val itemsArray = JSONArray()
        section.items.forEach { item ->
            itemsArray.put(
                JSONObject().apply {
                    put("id", item.id)
                    put("label", item.label)
                    put("status", item.status.name)
                    put("notes", item.notes)
                }
            )
        }
        sectionsArray.put(
            JSONObject().apply {
                put("title", section.title)
                put("items", itemsArray)
            }
        )
    }
    return JSONObject().apply {
        put("customerInfo", customer)
        put("sections", sectionsArray)
        put("generalNotes", generalNotes)
    }
}

fun JSONObject.toInspectionFormState(): InspectionFormState {
    val customerJson = optJSONObject("customerInfo") ?: JSONObject()
    val sectionsArray = optJSONArray("sections") ?: JSONArray()
    val sections = mutableListOf<InspectionSection>()
    for (i in 0 until sectionsArray.length()) {
        val sectionJson = sectionsArray.optJSONObject(i) ?: continue
        val itemsArray = sectionJson.optJSONArray("items") ?: JSONArray()
        val items = mutableListOf<InspectionItem>()
        for (j in 0 until itemsArray.length()) {
            val itemJson = itemsArray.optJSONObject(j) ?: continue
            val statusName = itemJson.optString("status", InspectionStatus.NONE.name)
            val status = runCatching { InspectionStatus.valueOf(statusName) }
                .getOrDefault(InspectionStatus.NONE)
            items.add(
                InspectionItem(
                    id = itemJson.optString("id", ""),
                    label = itemJson.optString("label", ""),
                    status = status,
                    notes = itemJson.optString("notes", "")
                )
            )
        }
        sections.add(
            InspectionSection(
                title = sectionJson.optString("title", ""),
                items = items
            )
        )
    }
    if (sections.isEmpty()) {
        return InspectionFormState(generalNotes = optString("generalNotes", ""))
    }
    return InspectionFormState(
        customerInfo = CustomerInfo(
            customerName = customerJson.optString("customerName", ""),
            customerPhone = customerJson.optString("customerPhone", ""),
            vehicle = customerJson.optString("vehicle", ""),
            mileage = customerJson.optString("mileage", "")
        ),
        sections = sections,
        generalNotes = optString("generalNotes", "")
    )
}

fun InspectionFormState.hasSavableContent(): Boolean {
    if (customerInfo.customerName.isNotBlank() ||
        customerInfo.customerPhone.isNotBlank() ||
        customerInfo.vehicle.isNotBlank() ||
        generalNotes.isNotBlank()
    ) {
        return true
    }
    return sections.any { section ->
        section.items.any { item ->
            item.status != InspectionStatus.NONE || item.notes.isNotBlank()
        }
    }
}
