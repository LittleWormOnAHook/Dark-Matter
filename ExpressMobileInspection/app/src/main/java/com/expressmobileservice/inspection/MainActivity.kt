package com.expressmobileservice.inspection

import android.content.ClipData
import android.content.Intent
import android.os.Bundle
import android.widget.Toast
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.ui.Modifier
import androidx.core.content.FileProvider
import com.expressmobileservice.inspection.ui.InspectionScreen
import com.expressmobileservice.inspection.ui.ReportShareType
import com.expressmobileservice.inspection.ui.theme.ExpressMobileInspectionTheme
import java.io.File

class MainActivity : ComponentActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            ExpressMobileInspectionTheme {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colorScheme.background
                ) {
                    InspectionScreen(
                        onShareReport = { state, type, onComplete ->
                            shareReport(state, type, onComplete)
                        },
                        onShareError = { message ->
                            Toast.makeText(this, message, Toast.LENGTH_LONG).show()
                        }
                    )
                }
            }
        }
    }

    private fun shareReport(
        state: InspectionFormState,
        type: ReportShareType,
        onComplete: (Boolean) -> Unit
    ) {
        Thread {
            try {
                val file = when (type) {
                    ReportShareType.PDF -> ReportExporter.exportPdf(this, state)
                    ReportShareType.IMAGE -> ReportExporter.exportImage(this, state)
                }
                if (!file.exists() || file.length() == 0L) {
                    throw IllegalStateException("Report file was not created.")
                }

                val uri = FileProvider.getUriForFile(
                    this,
                    "$packageName.fileprovider",
                    file
                )
                val mimeType = when (type) {
                    ReportShareType.PDF -> "application/pdf"
                    ReportShareType.IMAGE -> "image/jpeg"
                }
                val subject = "$COMPANY_NAME — Vehicle Inspection Report"
                val sendIntent = buildFileShareIntent(uri, mimeType, subject, file)

                runOnUiThread {
                    val chooser = Intent.createChooser(sendIntent, "Send inspection report").apply {
                        addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
                    }
                    startActivity(chooser)
                    onComplete(true)
                }
            } catch (e: Exception) {
                runOnUiThread {
                    Toast.makeText(
                        this,
                        "Could not create report. Please try again.",
                        Toast.LENGTH_LONG
                    ).show()
                    onComplete(false)
                }
            }
        }.start()
    }

  /**
   * Share only the generated PDF/JPEG file. Do not set EXTRA_TEXT on file shares —
   * many SMS and email clients treat the intent as plain text and drop the attachment.
   */
    private fun buildFileShareIntent(
        uri: android.net.Uri,
        mimeType: String,
        subject: String,
        file: File
    ): Intent {
        return Intent(Intent.ACTION_SEND).apply {
            type = mimeType
            putExtra(Intent.EXTRA_STREAM, uri)
            putExtra(Intent.EXTRA_SUBJECT, subject)
            clipData = ClipData.newUri(contentResolver, file.name, uri)
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
        }
    }
}
