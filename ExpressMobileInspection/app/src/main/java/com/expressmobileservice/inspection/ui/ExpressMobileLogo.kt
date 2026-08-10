package com.expressmobileservice.inspection.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.expressmobileservice.inspection.ui.theme.SamsungCalendarColors

@Composable
fun ExpressMobileLogo(
    modifier: Modifier = Modifier
) {
    Box(
        modifier = modifier
            .clip(RoundedCornerShape(6.dp))
            .border(1.5.dp, SamsungCalendarColors.metallicGold, RoundedCornerShape(6.dp))
            .background(SamsungCalendarColors.deepPurple)
            .padding(horizontal = 12.dp, vertical = 6.dp),
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = "Express Mobile",
            color = SamsungCalendarColors.eggWhite,
            fontWeight = FontWeight.Bold,
            fontSize = 15.sp,
            letterSpacing = 0.3.sp,
            maxLines = 1
        )
    }
}
