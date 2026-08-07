package com.expressmobileservice.inspection.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

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
    outline = Color(0xFFB0BEC5)
)

@Composable
fun ExpressMobileInspectionTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = LightColors,
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
