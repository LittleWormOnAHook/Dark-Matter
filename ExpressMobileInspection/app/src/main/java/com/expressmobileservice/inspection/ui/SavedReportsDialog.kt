package com.expressmobileservice.inspection.ui

import android.content.Intent
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Image
import androidx.compose.material.icons.filled.PictureAsPdf
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.core.content.FileProvider
import com.expressmobileservice.inspection.InspectionFormState
import com.expressmobileservice.inspection.SavedReport
import com.expressmobileservice.inspection.SavedReportStore
import java.io.File
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

@Composable
fun SavedReportsDialog(
    onDismiss: () -> Unit,
    onLoadReport: (InspectionFormState) -> Unit
) {
    val context = LocalContext.current
    val store = remember { SavedReportStore(context) }
    var reports by remember { mutableStateOf(store.listReports()) }

    fun refresh() {
        reports = store.listReports()
    }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Saved Reports") },
        text = {
            if (reports.isEmpty()) {
                Text(
                    text = "No saved reports yet. Reports are saved when you send a PDF or image, or when you leave the app with customer info entered.",
                    style = MaterialTheme.typography.bodyMedium
                )
            } else {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .heightIn(max = 420.dp)
                        .verticalScroll(rememberScrollState()),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    reports.forEach { report ->
                        SavedReportCard(
                            report = report,
                            onOpenPdf = {
                                store.pdfFile(report)?.let { openReportFile(context, it, "application/pdf") }
                            },
                            onOpenImage = {
                                store.imageFile(report)?.let { openReportFile(context, it, "image/jpeg") }
                            },
                            onLoadForm = {
                                onLoadReport(report.toFormState())
                                onDismiss()
                            },
                            onDelete = {
                                store.deleteReport(report.id)
                                refresh()
                            }
                        )
                    }
                }
            }
        },
        confirmButton = {
            TextButton(onClick = onDismiss) {
                Text("Close")
            }
        }
    )
}

@Composable
private fun SavedReportCard(
    report: SavedReport,
    onOpenPdf: () -> Unit,
    onOpenImage: () -> Unit,
    onLoadForm: () -> Unit,
    onDelete: () -> Unit
) {
    val dateText = SimpleDateFormat("MMM d, yyyy h:mm a", Locale.US)
        .format(Date(report.savedAtMillis))
    val statusLabel = if (report.isDraft) "Draft" else "Sent"

    Card(
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.surfaceVariant
        )
    ) {
        Column(modifier = Modifier.padding(12.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.Top
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Text(report.displayTitle, fontWeight = FontWeight.SemiBold)
                    if (report.displaySubtitle.isNotBlank()) {
                        Text(
                            report.displaySubtitle,
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                    Text(
                        "$dateText • $statusLabel",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
                IconButton(onClick = onDelete) {
                    Icon(Icons.Default.Delete, contentDescription = "Delete report")
                }
            }
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = 8.dp),
                horizontalArrangement = Arrangement.spacedBy(6.dp)
            ) {
                if (report.pdfFileName != null) {
                    OutlinedButton(onClick = onOpenPdf, modifier = Modifier.weight(1f)) {
                        Icon(Icons.Default.PictureAsPdf, contentDescription = null)
                        Text("PDF", modifier = Modifier.padding(start = 4.dp))
                    }
                }
                if (report.imageFileName != null) {
                    OutlinedButton(onClick = onOpenImage, modifier = Modifier.weight(1f)) {
                        Icon(Icons.Default.Image, contentDescription = null)
                        Text("Image", modifier = Modifier.padding(start = 4.dp))
                    }
                }
                OutlinedButton(onClick = onLoadForm, modifier = Modifier.weight(1f)) {
                    Icon(Icons.Default.Refresh, contentDescription = null)
                    Text("Load", modifier = Modifier.padding(start = 4.dp))
                }
            }
        }
    }
}

private fun openReportFile(context: android.content.Context, file: File, mimeType: String) {
    val uri = FileProvider.getUriForFile(
        context,
        "${context.packageName}.fileprovider",
        file
    )
    val intent = Intent(Intent.ACTION_VIEW).apply {
        setDataAndType(uri, mimeType)
        addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
    }
    context.startActivity(Intent.createChooser(intent, "Open report"))
}
