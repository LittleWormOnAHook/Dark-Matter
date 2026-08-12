package com.expressmobileservice.inspection.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

private val InspGold = Color(0xFFFFD700)
private val InspPurple = Color(0xFF441F4D)

@Composable
fun InspBadgeButton(
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    Box(
        modifier = modifier
            .size(40.dp)
            .clip(CircleShape)
            .background(InspGold)
            .clickable(onClick = ExpressUiSounds.withAnchor(onClick)),
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = "INSP",
            color = InspPurple,
            fontSize = 9.sp,
            fontWeight = FontWeight.Bold,
            letterSpacing = 0.sp
        )
    }
}
