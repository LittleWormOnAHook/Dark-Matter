package com.expressmobileservice.inspection.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.luminance

private val BluePrimary = Color(0xFF1565C0)
private val BlueDark = Color(0xFF0D47A1)
private val GreenGood = Color(0xFF2E7D32)
private val RedBad = Color(0xFFC62828)
private val OrangeReplace = Color(0xFFE65100)

private val LightColors = lightColorScheme(
    primary = BluePrimary,
    onPrimary = Color.White,
    primaryContainer = Color(0xFFBBDEFB),
    secondary = BlueDark,
    background = Color(0xFFF5F7FA),
    surface = Color.White,
    onSurface = Color(0xFF1A1A1A),
    onSurfaceVariant = Color(0xFF616161),
    outline = Color(0xFFB0BEC5)
)

private val DarkColors = darkColorScheme(
    primary = Color(0xFF63A4FF),
    onPrimary = Color(0xFF0D2137),
    primaryContainer = Color(0xFF1E3A5F),
    secondary = Color(0xFF90CAF9),
    background = Color(0xFF121212),
    surface = Color(0xFF1E1E1E),
    onSurface = Color(0xFFE8E8E8),
    onSurfaceVariant = Color(0xFFB0B0B0),
    outline = Color(0xFF4A4A4A)
)

@Composable
fun ExpressMobileInspectionTheme(
    darkTheme: Boolean,
    content: @Composable () -> Unit
) {
    MaterialTheme(
        colorScheme = if (darkTheme) DarkColors else LightColors,
        content = content
    )
}

data class InspectionStatusColors(
    val good: Color,
    val bad: Color,
    val replace: Color,
    val goodContainer: Color,
    val badContainer: Color,
    val replaceContainer: Color
)

@Composable
fun inspectionStatusColors(): InspectionStatusColors {
    val isDark = MaterialTheme.colorScheme.background.luminance() < 0.5f
    return if (isDark) {
        InspectionStatusColors(
            good = Color(0xFF81C784),
            bad = Color(0xFFEF5350),
            replace = Color(0xFFFFB74D),
            goodContainer = Color(0xFF1B3D1F),
            badContainer = Color(0xFF3D1B1B),
            replaceContainer = Color(0xFF3D2A14)
        )
    } else {
        InspectionStatusColors(
            good = GreenGood,
            bad = RedBad,
            replace = OrangeReplace,
            goodContainer = Color(0xFFE8F5E9),
            badContainer = Color(0xFFFFEBEE),
            replaceContainer = Color(0xFFFFF3E0)
        )
    }
}

/** Legacy static colors — prefer [inspectionStatusColors] in composables. */
object InspectionColors {
    val good = GreenGood
    val bad = RedBad
    val replace = OrangeReplace
    val goodContainer = Color(0xFFE8F5E9)
    val badContainer = Color(0xFFFFEBEE)
    val replaceContainer = Color(0xFFFFF3E0)
}
