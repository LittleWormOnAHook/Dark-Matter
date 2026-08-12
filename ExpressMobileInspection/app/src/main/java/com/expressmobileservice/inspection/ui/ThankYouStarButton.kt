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
import androidx.compose.ui.unit.dp
import com.expressmobileservice.inspection.ui.theme.SamsungCalendarColors

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
            .background(SamsungCalendarColors.accentPurple)
            .border(1.5.dp, SamsungCalendarColors.green, CircleShape)
            .clickable(onClick = onClick),
        contentAlignment = Alignment.Center
    ) {
        Icon(
            imageVector = Icons.Default.Star,
            contentDescription = contentDescription,
            tint = SamsungCalendarColors.green,
            modifier = Modifier.size(22.dp)
        )
    }
}
