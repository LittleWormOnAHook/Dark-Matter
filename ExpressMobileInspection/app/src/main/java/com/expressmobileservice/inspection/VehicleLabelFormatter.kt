package com.expressmobileservice.inspection

fun formatVehicleLabel(raw: String): String {
    val trimmed = raw.trim()
    if (trimmed.isBlank()) return trimmed
    return trimmed
        .split(Regex("\\s+"))
        .joinToString(" ") { word ->
            word.split("-").joinToString("-") { segment ->
                segment.lowercase().replaceFirstChar { char ->
                    if (char.isLowerCase()) char.titlecase() else char.toString()
                }
            }
        }
}
