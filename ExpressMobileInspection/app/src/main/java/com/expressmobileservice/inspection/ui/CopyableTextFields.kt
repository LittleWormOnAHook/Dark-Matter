package com.expressmobileservice.inspection.ui

import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.TextField
import androidx.compose.material3.TextFieldColors
import androidx.compose.material3.TextFieldDefaults
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.input.VisualTransformation

@Composable
fun CopyableTextField(
    value: String,
    onValueChange: (String) -> Unit,
    modifier: Modifier = Modifier,
    enabled: Boolean = true,
    readOnly: Boolean = false,
    placeholder: @Composable (() -> Unit)? = null,
    singleLine: Boolean = true,
    minLines: Int = 1,
    maxLines: Int = if (singleLine) 1 else Int.MAX_VALUE,
    textStyle: TextStyle = TextStyle.Default,
    colors: TextFieldColors = TextFieldDefaults.colors(),
    keyboardOptions: KeyboardOptions = KeyboardOptions.Default,
    keyboardActions: KeyboardActions = KeyboardActions.Default,
    visualTransformation: VisualTransformation = VisualTransformation.None,
    copyMessage: String = "Copied"
) {
    val copyHandler = rememberCopyHandler()
    val copyModifier = Modifier.pointerInput(value) {
        detectTapGestures(
            onLongPress = {
                if (value.isNotBlank()) copyHandler(value, copyMessage)
            }
        )
    }

    TextField(
        value = value,
        onValueChange = { updated ->
            ExpressUiSounds.onTypingValueChange(value, updated, onValueChange)
        },
        modifier = modifier.then(copyModifier),
        enabled = enabled,
        readOnly = readOnly,
        placeholder = placeholder,
        singleLine = singleLine,
        minLines = minLines,
        maxLines = maxLines,
        textStyle = textStyle,
        colors = colors,
        keyboardOptions = keyboardOptions,
        keyboardActions = keyboardActions,
        visualTransformation = visualTransformation
    )
}

@Composable
fun CopyableOutlinedTextField(
    value: String,
    onValueChange: (String) -> Unit,
    modifier: Modifier = Modifier,
    enabled: Boolean = true,
    readOnly: Boolean = false,
    label: @Composable (() -> Unit)? = null,
    placeholder: @Composable (() -> Unit)? = null,
    leadingIcon: @Composable (() -> Unit)? = null,
    trailingIcon: @Composable (() -> Unit)? = null,
    singleLine: Boolean = true,
    minLines: Int = 1,
    maxLines: Int = if (singleLine) 1 else Int.MAX_VALUE,
    colors: TextFieldColors = OutlinedTextFieldDefaults.colors(),
    keyboardOptions: KeyboardOptions = KeyboardOptions.Default,
    keyboardActions: KeyboardActions = KeyboardActions.Default,
    visualTransformation: VisualTransformation = VisualTransformation.None,
    copyMessage: String = "Copied"
) {
    val copyHandler = rememberCopyHandler()
    val copyModifier = Modifier.pointerInput(value) {
        detectTapGestures(
            onLongPress = {
                if (value.isNotBlank()) copyHandler(value, copyMessage)
            }
        )
    }

    OutlinedTextField(
        value = value,
        onValueChange = { updated ->
            ExpressUiSounds.onTypingValueChange(value, updated, onValueChange)
        },
        modifier = modifier.then(copyModifier),
        enabled = enabled,
        readOnly = readOnly,
        label = label,
        placeholder = placeholder,
        leadingIcon = leadingIcon,
        trailingIcon = trailingIcon,
        singleLine = singleLine,
        minLines = minLines,
        maxLines = maxLines,
        colors = colors,
        keyboardOptions = keyboardOptions,
        keyboardActions = keyboardActions,
        visualTransformation = visualTransformation
    )
}
