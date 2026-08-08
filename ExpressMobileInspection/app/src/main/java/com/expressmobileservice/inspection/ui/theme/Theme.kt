package com.expressmobileservice.inspection.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val BluePrimary = Color(0xFF1565C0)
private val BlueDark = Color(0xFF0D47A1)
private val GreenAccent = Color(0xFF2E7D32)
private val GreenGood = Color(0xFF2E7D32)
private val RedBad = Color(0xFFC62828)
private val OrangeReplace = Color(0xFFE65100)

private val LightColors = lightColorScheme(
    primary = BluePrimary,
    onPrimary = Color.White,
    primaryContainer = Color(0xFFBBDEFB),
    secondary = BlueDark,
    tertiary = GreenAccent,
    onTertiary = Color.White,
    background = Color(0xFFF5F7FA),
    surface = Color.White,
    surfaceVariant = Color(0xFFE8EEF2),
    onSurface = Color(0xFF1A1A1A),
    outline = Color(0xFFB0BEC5)
)

private val DarkColors = darkColorScheme(
    primary = Color(0xFF4A8CFF),
    onPrimary = Color(0xFF010101),
    primaryContainer = Color(0xFF1C2A38),
    secondary = Color(0xFF90CAF9),
    tertiary = SamsungCalendarColors.green,
    onTertiary = Color(0xFF010101),
    background = SamsungCalendarColors.background,
    surface = SamsungCalendarColors.surface,
    surfaceVariant = SamsungCalendarColors.agendaSurface,
    onSurface = SamsungCalendarColors.onBackground,
    onSurfaceVariant = SamsungCalendarColors.muted,
    outline = Color(0xFF3A3A3A)
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
    val good = GreenGood
    val bad = RedBad
    val replace = OrangeReplace
    val goodContainer = Color(0xFFE8F5E9)
    val badContainer = Color(0xFFFFEBEE)
    val replaceContainer = Color(0xFFFFF3E0)
}
