package com.expressmobileservice.inspection.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.DarkMode
import androidx.compose.material.icons.filled.LightMode
import androidx.compose.material3.Icon
import androidx.compose.material3.Switch
import androidx.compose.material3.SwitchDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

@Composable
fun ThemeModeSlider(
    darkTheme: Boolean,
    onDarkThemeChange: (Boolean) -> Unit,
    modifier: Modifier = Modifier,
    contentColor: Color = Color.White
) {
    Row(
        modifier = modifier.padding(end = 4.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(4.dp)
    ) {
        Icon(
            imageVector = Icons.Default.LightMode,
            contentDescription = "Light mode",
            tint = if (!darkTheme) contentColor else contentColor.copy(alpha = 0.55f),
            modifier = Modifier.padding(start = 4.dp)
        )
        Switch(
            checked = darkTheme,
            onCheckedChange = onDarkThemeChange,
            colors = SwitchDefaults.colors(
                checkedThumbColor = Color(0xFF1E1E1E),
                checkedTrackColor = Color(0xFF90CAF9),
                uncheckedThumbColor = Color.White,
                uncheckedTrackColor = Color.White.copy(alpha = 0.45f),
                uncheckedBorderColor = Color.White.copy(alpha = 0.7f)
            )
        )
        Icon(
            imageVector = Icons.Default.DarkMode,
            contentDescription = "Dark mode",
            tint = if (darkTheme) contentColor else contentColor.copy(alpha = 0.55f)
        )
        Text(
            text = if (darkTheme) "Dark" else "Light",
            color = contentColor,
            fontSize = 12.sp,
            modifier = Modifier.padding(end = 4.dp)
        )
    }
}
