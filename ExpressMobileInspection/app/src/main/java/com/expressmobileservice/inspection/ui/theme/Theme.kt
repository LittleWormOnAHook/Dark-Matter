package com.expressmobileservice.inspection.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val DarkColors = darkColorScheme(
    primary = SamsungCalendarColors.green,
    onPrimary = Color(0xFF000000),
    primaryContainer = SamsungCalendarColors.quickAddField,
    onPrimaryContainer = SamsungCalendarColors.onBackground,
    secondary = SamsungCalendarColors.greenDark,
    onSecondary = Color(0xFF010101),
    tertiary = SamsungCalendarColors.eventBlue,
    onTertiary = Color(0xFF010101),
    background = SamsungCalendarColors.background,
    surface = SamsungCalendarColors.surface,
    surfaceVariant = SamsungCalendarColors.agendaSurface,
    onBackground = SamsungCalendarColors.onBackground,
    onSurface = SamsungCalendarColors.onBackground,
    onSurfaceVariant = SamsungCalendarColors.muted,
    outline = SamsungCalendarColors.divider
)

@Composable
fun ExpressMobileInspectionTheme(
    darkTheme: Boolean = true,
    content: @Composable () -> Unit
) {
    MaterialTheme(
        colorScheme = DarkColors,
        content = content
    )
}

object InspectionColors {
    val good = SamsungCalendarColors.green
    val bad = Color(0xFFFF5252)
    val replace = Color(0xFFFFB74D)
    val goodContainer = Color(0xFF1B3D22)
    val badContainer = Color(0xFF3D1B1B)
    val replaceContainer = Color(0xFF3D2E14)
}
