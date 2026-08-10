package com.expressmobileservice.inspection.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val RegalGold = SamsungCalendarColors.metallicGold
private val RegalDeepPurple = SamsungCalendarColors.deepPurple
private val RegalDeepPlum = SamsungCalendarColors.deepPlum
private val RegalOrchid = SamsungCalendarColors.orchidPurple
private val RegalEggWhite = SamsungCalendarColors.eggWhite
private val RegalTaupe = SamsungCalendarColors.warmTaupe

private val LightColors = lightColorScheme(
    primary = RegalDeepPlum,
    onPrimary = RegalEggWhite,
    primaryContainer = Color(0xFFE8DFF0),
    secondary = RegalGold,
    onSecondary = RegalDeepPurple,
    tertiary = RegalOrchid,
    onTertiary = RegalEggWhite,
    background = Color(0xFFF3EADD),
    surface = Color.White,
    surfaceVariant = Color(0xFFE8E0D8),
    onSurface = Color(0xFF36194D),
    outline = RegalTaupe
)

private val DarkColors = darkColorScheme(
    primary = RegalGold,
    onPrimary = RegalDeepPurple,
    primaryContainer = RegalDeepPlum,
    secondary = RegalOrchid,
    onSecondary = RegalEggWhite,
    tertiary = RegalGold,
    onTertiary = RegalDeepPurple,
    background = SamsungCalendarColors.background,
    surface = SamsungCalendarColors.surface,
    surfaceVariant = SamsungCalendarColors.agendaSurface,
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
        colorScheme = if (darkTheme) DarkColors else LightColors,
        content = content
    )
}

object InspectionColors {
    val good = Color(0xFF4CAF50)
    val bad = Color(0xFFC62828)
    val replace = Color(0xFFE65100)
    val goodContainer = Color(0xFF1B3D1F)
    val badContainer = Color(0xFF3D1515)
    val replaceContainer = Color(0xFF3D2410)
}
