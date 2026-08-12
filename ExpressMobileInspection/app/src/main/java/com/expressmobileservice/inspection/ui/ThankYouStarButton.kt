package com.expressmobileservice.inspection.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Star
import androidx.compose.material3.Icon
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp

private val ThankYouGold = Color(0xFFD4A017)

@Composable
fun ThankYouStarButton(
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    contentDescription: String = "Send thank you note"
) {
    Box(
        modifier = modifier
            .size(40.dp)
            .clip(CircleShape)
            .background(ThankYouGold.copy(alpha = 0.18f))
            .border(1.dp, ThankYouGold.copy(alpha = 0.85f), CircleShape)
            .clickable(onClick = onClick),
        contentAlignment = Alignment.Center
    ) {
        Icon(
            imageVector = Icons.Default.Star,
            contentDescription = contentDescription,
            tint = ThankYouGold,
            modifier = Modifier.size(22.dp)
        )
    }
}
