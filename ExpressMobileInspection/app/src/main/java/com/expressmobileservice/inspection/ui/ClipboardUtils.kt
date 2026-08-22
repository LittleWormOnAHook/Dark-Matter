package com.expressmobileservice.inspection.ui

import android.widget.Toast
import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.combinedClickable
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalClipboardManager
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.AnnotatedString

@Composable
fun rememberCopyHandler(): (String, String) -> Unit {
    val clipboard = LocalClipboardManager.current
    val context = LocalContext.current
    return remember(clipboard, context) {
        { text, message ->
            if (text.isNotBlank()) {
                clipboard.setText(AnnotatedString(text))
                Toast.makeText(context, message, Toast.LENGTH_SHORT).show()
            }
        }
    }
}

@OptIn(ExperimentalFoundationApi::class)
fun Modifier.copyOnLongPress(
    text: String,
    message: String = "Copied",
    onCopy: (String, String) -> Unit
): Modifier = combinedClickable(
    onClick = {},
    onLongClick = {
        if (text.isNotBlank()) onCopy(text, message)
    }
)
