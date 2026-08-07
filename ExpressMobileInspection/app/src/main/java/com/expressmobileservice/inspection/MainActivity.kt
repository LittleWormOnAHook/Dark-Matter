package com.expressmobileservice.inspection

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
                val intent = Intent(Intent.ACTION_SEND).apply {
                    this.type = mimeType
                    putExtra(Intent.EXTRA_STREAM, uri)
                    putExtra(Intent.EXTRA_SUBJECT, subject)
                    putExtra(
                        Intent.EXTRA_TEXT,
                        "Attached is your vehicle inspection report from $COMPANY_NAME. Call $COMPANY_PHONE with any questions."
                    )
                    addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
                }
                runOnUiThread {
                    startActivity(Intent.createChooser(intent, "Send inspection report"))
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
}
