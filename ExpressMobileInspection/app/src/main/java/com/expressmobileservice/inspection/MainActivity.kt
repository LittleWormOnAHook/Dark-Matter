package com.expressmobileservice.inspection

import android.content.Intent
import android.net.Uri
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.ui.Modifier
import com.expressmobileservice.inspection.ui.InspectionScreen
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
                        onShareReport = { reportText ->
                            shareReport(reportText)
                        }
                    )
                }
            }
        }
    }

    private fun shareReport(reportText: String) {
        val subject = "$COMPANY_NAME — Vehicle Inspection Report"
        val smsBody = reportText
        val emailBody = reportText

        val smsIntent = Intent(Intent.ACTION_SENDTO).apply {
            data = Uri.parse("smsto:")
            putExtra("sms_body", smsBody)
        }

        val emailIntent = Intent(Intent.ACTION_SENDTO).apply {
            data = Uri.parse("mailto:")
            putExtra(Intent.EXTRA_SUBJECT, subject)
            putExtra(Intent.EXTRA_TEXT, emailBody)
        }

        val chooser = Intent.createChooser(
            Intent(Intent.ACTION_SEND).apply {
                type = "text/plain"
                putExtra(Intent.EXTRA_SUBJECT, subject)
                putExtra(Intent.EXTRA_TEXT, reportText)
            },
            "Send inspection report via"
        ).apply {
            putExtra(
                Intent.EXTRA_INITIAL_INTENTS,
                arrayOf(smsIntent, emailIntent)
            )
        }

        startActivity(chooser)
    }
}
