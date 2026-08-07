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
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.core.content.FileProvider
import androidx.core.view.WindowCompat
import com.expressmobileservice.inspection.ui.InspectionScreen
import com.expressmobileservice.inspection.ui.ReportShareType
import com.expressmobileservice.inspection.ui.theme.ExpressMobileInspectionTheme
import java.io.File

class MainActivity : ComponentActivity() {

    private var currentStateProvider: (() -> InspectionFormState)? = null
    private val savedReportStore by lazy { SavedReportStore(this) }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        val themePreferences = ThemePreferences(this)
        setContent {
            var darkTheme by remember { mutableStateOf(themePreferences.isDarkMode()) }

            WindowCompat.getInsetsController(window, window.decorView).apply {
                isAppearanceLightStatusBars = !darkTheme
            }

            ExpressMobileInspectionTheme(darkTheme = darkTheme) {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colorScheme.background
                ) {
                    InspectionScreen(
                        darkTheme = darkTheme,
                        onDarkThemeChange = { enabled ->
                            darkTheme = enabled
                            themePreferences.setDarkMode(enabled)
                            WindowCompat.getInsetsController(window, window.decorView).apply {
                                isAppearanceLightStatusBars = !enabled
                            }
                        },
                        onShareReport = { state, type, onComplete ->
                            shareReport(state, type, onComplete)
                        },
                        onShareError = { message ->
                            Toast.makeText(this, message, Toast.LENGTH_LONG).show()
                        },
                        onRegisterStateProvider = { provider ->
                            currentStateProvider = provider
                        }
                    )
                }
            }
        }
    }

    override fun onStop() {
        super.onStop()
        currentStateProvider?.invoke()?.let { state ->
            if (state.hasSavableContent()) {
                savedReportStore.saveDraft(state)
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

                savedReportStore.saveExportedReport(state, file, type)

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
