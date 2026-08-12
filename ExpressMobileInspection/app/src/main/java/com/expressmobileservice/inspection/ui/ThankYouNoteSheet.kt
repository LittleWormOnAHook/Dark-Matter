package com.expressmobileservice.inspection.ui

import android.widget.Toast
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Language
import androidx.compose.material.icons.filled.PictureAsPdf
import androidx.compose.material.icons.filled.Star
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.Text
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.core.content.FileProvider
import com.expressmobileservice.inspection.Appointment
import com.expressmobileservice.inspection.InspectionFormState
import com.expressmobileservice.inspection.InspectionStore
import com.expressmobileservice.inspection.ReportExporter
import com.expressmobileservice.inspection.appointmentSendReadinessError
import com.expressmobileservice.inspection.buildThankYouNoteMessage
import com.expressmobileservice.inspection.inspectionFormForThankYou
import com.expressmobileservice.inspection.resolveMessagingPhone
import com.expressmobileservice.inspection.shareThankYouWithInspectionPdf
import com.expressmobileservice.inspection.ui.theme.SamsungCalendarColors

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ThankYouNoteSheet(
    appointment: Appointment,
    inspectionStore: InspectionStore,
    formOverride: InspectionFormState? = null,
    onDismiss: () -> Unit
) {
    val context = LocalContext.current
    val phone = appointment.resolveMessagingPhone()
        .ifBlank { formOverride?.customerInfo?.customerPhone.orEmpty() }
    val form = formOverride ?: inspectionFormForThankYou(appointment, inspectionStore)
    val readinessError = appointmentSendReadinessError(appointment, inspectionStore, formOverride)
    var isSending by remember { mutableStateOf(false) }
    val canSend = readinessError == null && !isSending

    ModalBottomSheet(
        onDismissRequest = { if (!isSending) onDismiss() },
        sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true),
        containerColor = SamsungCalendarColors.surface
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 24.dp)
                .padding(bottom = 32.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text(
                text = "Thank you",
                color = SamsungCalendarColors.green,
                fontSize = 28.sp,
                fontWeight = FontWeight.Bold
            )
            Spacer(modifier = Modifier.height(12.dp))
            Text(
                text = "Thank you for choosing Express Mobile Service. We appreciate you trusting us " +
                    "with your vehicle today, and we hope everything is running smoothly for you.",
                color = SamsungCalendarColors.onBackground,
                fontSize = 15.sp,
                lineHeight = 22.sp,
                textAlign = TextAlign.Center
            )
            Spacer(modifier = Modifier.height(8.dp))
            Text(
                text = "When you have a moment, we'd love to hear how we did:",
                color = SamsungCalendarColors.muted,
                fontSize = 13.sp,
                textAlign = TextAlign.Center
            )
            Spacer(modifier = Modifier.height(16.dp))
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .background(SamsungCalendarColors.accentPurple, RoundedCornerShape(12.dp))
                    .border(1.dp, SamsungCalendarColors.green.copy(alpha = 0.65f), RoundedCornerShape(12.dp))
                    .padding(horizontal = 16.dp, vertical = 12.dp),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.Center
            ) {
                Icon(
                    Icons.Default.PictureAsPdf,
                    contentDescription = null,
                    tint = SamsungCalendarColors.green,
                    modifier = Modifier.size(20.dp)
                )
                Spacer(modifier = Modifier.width(8.dp))
                Text(
                    text = "Inspection PDF attached",
                    color = SamsungCalendarColors.green,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 14.sp
                )
            }
            Spacer(modifier = Modifier.height(16.dp))
            ThankYouLinkButton(
                label = "Google review",
                icon = {
                    Icon(
                        Icons.Default.Star,
                        contentDescription = null,
                        tint = SamsungCalendarColors.green
                    )
                }
            )
            Spacer(modifier = Modifier.height(10.dp))
            ThankYouLinkButton(
                label = "expressmobileservice.net",
                icon = {
                    Icon(
                        Icons.Default.Language,
                        contentDescription = null,
                        tint = SamsungCalendarColors.green
                    )
                }
            )
            readinessError?.let { error ->
                Spacer(modifier = Modifier.height(16.dp))
                Text(
                    text = error,
                    color = SamsungCalendarColors.eventRed,
                    fontSize = 13.sp,
                    textAlign = TextAlign.Center,
                    lineHeight = 18.sp
                )
            }
            Spacer(modifier = Modifier.height(24.dp))
            Button(
                onClick = ExpressUiSounds.withImpact {
                    if (!canSend) {
                        readinessError?.let {
                            Toast.makeText(context, it, Toast.LENGTH_LONG).show()
                        }
                        return@withImpact
                    }
                    isSending = true
                    val message = buildThankYouNoteMessage()
                    Thread {
                        try {
                            val file = ReportExporter.exportPdf(context, form)
                            val uri = FileProvider.getUriForFile(
                                context,
                                "${context.packageName}.fileprovider",
                                file
                            )
                            android.os.Handler(android.os.Looper.getMainLooper()).post {
                                shareThankYouWithInspectionPdf(context, uri, phone, message)
                                isSending = false
                                onDismiss()
                            }
                        } catch (_: Exception) {
                            android.os.Handler(android.os.Looper.getMainLooper()).post {
                                isSending = false
                                Toast.makeText(
                                    context,
                                    "Could not create inspection PDF. Please try again.",
                                    Toast.LENGTH_LONG
                                ).show()
                            }
                        }
                    }.start()
                },
                enabled = canSend,
                modifier = Modifier.fillMaxWidth(),
                colors = ButtonDefaults.buttonColors(
                    containerColor = SamsungCalendarColors.green,
                    contentColor = Color.Black,
                    disabledContainerColor = SamsungCalendarColors.quickAddField,
                    disabledContentColor = SamsungCalendarColors.muted
                ),
                shape = RoundedCornerShape(12.dp)
            ) {
                if (isSending) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(22.dp),
                        color = Color.Black,
                        strokeWidth = 2.dp
                    )
                    Spacer(modifier = Modifier.width(10.dp))
                }
                Text(
                    text = if (isSending) "Preparing PDF…" else "Send message + PDF",
                    fontWeight = FontWeight.Bold,
                    fontSize = 16.sp
                )
            }
            if (phone.isNotBlank()) {
                Spacer(modifier = Modifier.height(8.dp))
                Text(
                    text = "To: $phone",
                    color = SamsungCalendarColors.muted,
                    fontSize = 12.sp
                )
            }
        }
    }
}

@Composable
private fun ThankYouLinkButton(
    label: String,
    icon: @Composable () -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .background(SamsungCalendarColors.accentPurple, RoundedCornerShape(12.dp))
            .border(1.dp, SamsungCalendarColors.green.copy(alpha = 0.65f), RoundedCornerShape(12.dp))
            .padding(horizontal = 16.dp, vertical = 14.dp),
        horizontalArrangement = Arrangement.Center,
        verticalAlignment = Alignment.CenterVertically
    ) {
        icon()
        Spacer(modifier = Modifier.width(8.dp))
        Text(
            text = label,
            color = SamsungCalendarColors.green,
            fontWeight = FontWeight.SemiBold,
            fontSize = 15.sp
        )
    }
}
