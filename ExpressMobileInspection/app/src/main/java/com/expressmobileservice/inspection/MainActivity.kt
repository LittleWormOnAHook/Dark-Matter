package com.expressmobileservice.inspection

import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.widget.Toast
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.core.content.FileProvider
import androidx.core.view.WindowCompat
import com.expressmobileservice.inspection.ui.ExpressUiSounds
import com.expressmobileservice.inspection.ui.HomeScreen
import com.expressmobileservice.inspection.ui.ReportShareType
import com.expressmobileservice.inspection.ui.theme.ExpressMobileInspectionTheme
import com.expressmobileservice.inspection.ui.theme.SamsungCalendarColors

class MainActivity : ComponentActivity() {

    private lateinit var appointmentStore: AppointmentStore
    private lateinit var inspectionStore: InspectionStore

    private var refreshKey by mutableIntStateOf(0)
    private var showRestoreBanner by mutableStateOf(false)

    private val importBackupLauncher = registerForActivityResult(
        ActivityResultContracts.OpenDocument()
    ) { uri: Uri? ->
        if (uri == null) return@registerForActivityResult
        val result = AppDataBackup.restoreFromUri(this, uri, appointmentStore, inspectionStore)
        handleRestoreResult(result)
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        appointmentStore = AppointmentStore(this)
        inspectionStore = InspectionStore(this)
        val snapshotData = {
            AppDataBackup.writeSnapshot(this, appointmentStore, inspectionStore)
        }
        appointmentStore.onDataChanged = snapshotData
        inspectionStore.onDataChanged = snapshotData
        val restoreResult = AppDataBackup.restoreIfNeeded(this, appointmentStore, inspectionStore)
        showRestoreBanner = !restoreResult.restored && AppDataBackup.isStoreEmpty(appointmentStore, inspectionStore)
        enableEdgeToEdge()
        WindowCompat.getInsetsController(window, window.decorView).apply {
            isAppearanceLightStatusBars = false
            isAppearanceLightNavigationBars = false
        }
        window.statusBarColor = android.graphics.Color.parseColor("#000000")
        window.navigationBarColor = android.graphics.Color.parseColor("#000000")
        ExpressUiSounds.init(this)
        setContent {
            ExpressMobileInspectionTheme {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = SamsungCalendarColors.background
                ) {
                    if (restoreResult.restored) {
                        LaunchedEffect(Unit) {
                            Toast.makeText(
                                this@MainActivity,
                                restoreMessage(restoreResult),
                                Toast.LENGTH_LONG
                            ).show()
                        }
                    }
                    HomeScreen(
                        key = refreshKey,
                        appointmentStore = appointmentStore,
                        inspectionStore = inspectionStore,
                        showRestoreBanner = showRestoreBanner,
                        onRestoreFromDownloads = { restoreFromDownloads() },
                        onImportBackupFile = { importBackupLauncher.launch(arrayOf("application/json", "text/plain", "*/*")) },
                        onShareReport = { state, type, onComplete ->
                            shareReport(state, type, onComplete)
                        },
                        onShareError = { message ->
                            Toast.makeText(this, message, Toast.LENGTH_LONG).show()
                        },
                        onNotify = { message ->
                            Toast.makeText(this, message, Toast.LENGTH_SHORT).show()
                        }
                    )
                }
            }
        }
    }

    override fun onStop() {
        if (::appointmentStore.isInitialized && ::inspectionStore.isInitialized) {
            AppDataBackup.writeSnapshot(this, appointmentStore, inspectionStore)
        }
        super.onStop()
    }

    private fun restoreFromDownloads() {
        val result = AppDataBackup.restoreFromDownloads(this, appointmentStore, inspectionStore)
        handleRestoreResult(result)
    }

    private fun handleRestoreResult(result: AppDataBackup.RestoreResult) {
        if (result.restored) {
            showRestoreBanner = false
            refreshKey++
            Toast.makeText(this, restoreMessage(result), Toast.LENGTH_LONG).show()
        } else {
            Toast.makeText(
                this,
                "No backup found. Check Downloads/ExpressMobileService/ExpressMobileService_backup.json",
                Toast.LENGTH_LONG
            ).show()
        }
    }

    private fun restoreMessage(result: AppDataBackup.RestoreResult): String {
        val source = result.sourceLabel ?: "backup"
        return "Restored ${result.appointmentCount} jobs and ${result.inspectionCount} inspections from $source"
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
                val message =
                    "Attached is your vehicle inspection report from $COMPANY_NAME. Call $COMPANY_PHONE with any questions."
                runOnUiThread {
                    when (type) {
                        ReportShareType.PDF -> {
                            val readinessError = state.inspectionSendReadinessError()
                            if (readinessError != null) {
                                Toast.makeText(this, readinessError, Toast.LENGTH_LONG).show()
                                onComplete(false)
                                return@runOnUiThread
                            }
                            shareThankYouWithInspectionPdf(
                                this,
                                uri,
                                state.customerInfo.customerPhone,
                                buildThankYouNotePlainMessage(),
                                buildThankYouNoteHtmlMessage()
                            )
                        }
                        ReportShareType.IMAGE -> {
                            val intent = Intent(Intent.ACTION_SEND).apply {
                                this.type = mimeType
                                putExtra(Intent.EXTRA_STREAM, uri)
                                putExtra(Intent.EXTRA_SUBJECT, subject)
                                putExtra(Intent.EXTRA_TEXT, message)
                                addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
                            }
                            startActivity(Intent.createChooser(intent, "Send inspection report"))
                        }
                    }
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
