package com.expressmobileservice.inspection.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowForward
import androidx.compose.material.icons.filled.AccessTime
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material.icons.filled.Notes
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Phone
import androidx.compose.material3.DatePicker
import androidx.compose.material3.DatePickerDialog
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Switch
import androidx.compose.material3.SwitchDefaults
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TextField
import androidx.compose.material3.TextFieldDefaults
import androidx.compose.material3.TimePicker
import androidx.compose.material3.rememberDatePickerState
import androidx.compose.material3.rememberTimePickerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.expressmobileservice.inspection.Appointment
import com.expressmobileservice.inspection.combineDateAndTime
import com.expressmobileservice.inspection.defaultAppointmentEnd
import com.expressmobileservice.inspection.defaultAppointmentStart
import com.expressmobileservice.inspection.formatDayHeader
import com.expressmobileservice.inspection.formatTime
import com.expressmobileservice.inspection.toEpochMillisAtStartOfDay
import com.expressmobileservice.inspection.toLocalDate
import com.expressmobileservice.inspection.toLocalDateTime
import com.expressmobileservice.inspection.ui.theme.SamsungCalendarColors
import java.time.Instant
import java.time.LocalDate
import java.time.LocalTime
import java.time.ZoneId

private enum class PickerTarget {
    START_DATE,
    END_DATE,
    START_TIME,
    END_TIME
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AppointmentEditorScreen(
    initial: Appointment?,
    defaultDate: LocalDate,
    prefilledJobNotes: String? = null,
    onDismiss: () -> Unit,
    onSave: (Appointment) -> Unit
) {
    val defaultStart = initial?.startEpochMillis ?: defaultAppointmentStart(defaultDate)
    val defaultEnd = initial?.endEpochMillis ?: defaultAppointmentEnd(defaultStart)

    var title by remember {
        mutableStateOf(
            initial?.let { if (it.jobNotes.isNotBlank()) it.jobNotes else it.customerName }
                ?: prefilledJobNotes.orEmpty()
        )
    }
    var customerName by remember { mutableStateOf(initial?.customerName.orEmpty()) }
    var customerPhone by remember { mutableStateOf(initial?.customerPhone.orEmpty()) }
    var jobNotes by remember {
        mutableStateOf(initial?.jobNotes ?: prefilledJobNotes.orEmpty())
    }
    var address by remember { mutableStateOf(initial?.address.orEmpty()) }
    var allDay by remember { mutableStateOf(initial?.allDay ?: false) }
    var startMillis by remember { mutableStateOf(defaultStart) }
    var endMillis by remember { mutableStateOf(defaultEnd) }
    var showPicker by remember { mutableStateOf<PickerTarget?>(null) }
    var validationError by remember { mutableStateOf<String?>(null) }

    fun save() {
        val resolvedJob = when {
            jobNotes.isNotBlank() -> jobNotes.trim()
            title.isNotBlank() -> title.trim()
            else -> ""
        }
        val resolvedName = customerName.trim().ifBlank {
            title.trim().takeIf { resolvedJob.isBlank() }.orEmpty()
        }
        if (resolvedName.isBlank() && resolvedJob.isBlank()) {
            validationError = "Enter a title, customer, or job."
            return
        }
        if (!allDay && endMillis <= startMillis) {
            validationError = "End time must be after start time."
            return
        }
        onSave(
            Appointment(
                id = initial?.id ?: java.util.UUID.randomUUID().toString(),
                customerName = resolvedName,
                customerPhone = customerPhone.trim(),
                jobNotes = resolvedJob,
                address = address.trim(),
                startEpochMillis = if (allDay) {
                    startMillis.toLocalDate().toEpochMillisAtStartOfDay()
                } else startMillis,
                endEpochMillis = if (allDay) {
                    endMillis.toLocalDate().plusDays(1).toEpochMillisAtStartOfDay() - 1
                } else endMillis,
                allDay = allDay
            )
        )
    }

    showPicker?.let { target ->
        when (target) {
            PickerTarget.START_DATE, PickerTarget.END_DATE -> {
                val initialMillis = when (target) {
                    PickerTarget.START_DATE -> startMillis
                    PickerTarget.END_DATE -> endMillis
                    else -> startMillis
                }
                val state = rememberDatePickerState(initialSelectedDateMillis = initialMillis)
                DatePickerDialog(
                    onDismissRequest = { showPicker = null },
                    confirmButton = {
                        TextButton(
                            onClick = {
                                state.selectedDateMillis?.let { millis ->
                                    val pickedDate = Instant.ofEpochMilli(millis)
                                        .atZone(ZoneId.systemDefault()).toLocalDate()
                                    when (target) {
                                        PickerTarget.START_DATE -> {
                                            val time = startMillis.toLocalDateTime().toLocalTime()
                                            startMillis = combineDateAndTime(pickedDate, time)
                                            if (endMillis <= startMillis) {
                                                endMillis = defaultAppointmentEnd(startMillis)
                                            }
                                        }
                                        PickerTarget.END_DATE -> {
                                            val time = endMillis.toLocalDateTime().toLocalTime()
                                            endMillis = combineDateAndTime(pickedDate, time)
                                        }
                                        else -> Unit
                                    }
                                }
                                showPicker = null
                            }
                        ) {
                            Text("OK", color = SamsungCalendarColors.green)
                        }
                    },
                    dismissButton = {
                        TextButton(onClick = { showPicker = null }) {
                            Text("Cancel")
                        }
                    }
                ) {
                    DatePicker(state = state)
                }
            }
            PickerTarget.START_TIME, PickerTarget.END_TIME -> {
                val initialTime = when (target) {
                    PickerTarget.START_TIME -> startMillis.toLocalDateTime().toLocalTime()
                    PickerTarget.END_TIME -> endMillis.toLocalDateTime().toLocalTime()
                    else -> LocalTime.now()
                }
                val state = rememberTimePickerState(
                    initialHour = initialTime.hour,
                    initialMinute = initialTime.minute,
                    is24Hour = false
                )
                DatePickerDialog(
                    onDismissRequest = { showPicker = null },
                    confirmButton = {
                        TextButton(
                            onClick = {
                                val date = when (target) {
                                    PickerTarget.START_TIME -> startMillis.toLocalDate()
                                    PickerTarget.END_TIME -> endMillis.toLocalDate()
                                    else -> LocalDate.now()
                                }
                                val time = LocalTime.of(state.hour, state.minute)
                                when (target) {
                                    PickerTarget.START_TIME -> {
                                        startMillis = combineDateAndTime(date, time)
                                        if (endMillis <= startMillis) {
                                            endMillis = defaultAppointmentEnd(startMillis)
                                        }
                                    }
                                    PickerTarget.END_TIME -> {
                                        endMillis = combineDateAndTime(date, time)
                                    }
                                    else -> Unit
                                }
                                showPicker = null
                            }
                        ) {
                            Text("OK", color = SamsungCalendarColors.green)
                        }
                    },
                    dismissButton = {
                        TextButton(onClick = { showPicker = null }) {
                            Text("Cancel")
                        }
                    }
                ) {
                    TimePicker(state = state)
                }
            }
        }
    }

    Scaffold(
        containerColor = SamsungCalendarColors.background,
        bottomBar = {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .background(SamsungCalendarColors.background)
                    .padding(horizontal = 24.dp, vertical = 16.dp),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                TextButton(onClick = onDismiss) {
                    Text(
                        "Cancel",
                        color = SamsungCalendarColors.green,
                        fontWeight = FontWeight.Bold,
                        fontSize = 16.sp
                    )
                }
                TextButton(onClick = { save() }) {
                    Text(
                        "Save",
                        color = SamsungCalendarColors.green,
                        fontWeight = FontWeight.Bold,
                        fontSize = 16.sp
                    )
                }
            }
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .verticalScroll(rememberScrollState())
                .background(SamsungCalendarColors.background)
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 20.dp, vertical = 12.dp),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.End
            ) {
                Box(
                    modifier = Modifier
                        .size(14.dp)
                        .clip(CircleShape)
                        .background(SamsungCalendarColors.eventBlue)
                )
            }

            TextField(
                value = title,
                onValueChange = {
                    title = it
                    if (jobNotes.isBlank() || jobNotes == title) jobNotes = it
                },
                placeholder = { Text("Title", color = SamsungCalendarColors.muted) },
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp),
                singleLine = true,
                colors = TextFieldDefaults.colors(
                    focusedContainerColor = Color.Transparent,
                    unfocusedContainerColor = Color.Transparent,
                    focusedIndicatorColor = Color.Transparent,
                    unfocusedIndicatorColor = Color.Transparent,
                    cursorColor = SamsungCalendarColors.green
                ),
                textStyle = MaterialTheme.typography.headlineSmall.copy(fontWeight = FontWeight.Normal)
            )

            validationError?.let { error ->
                Text(
                    text = error,
                    color = MaterialTheme.colorScheme.error,
                    modifier = Modifier.padding(horizontal = 20.dp, vertical = 4.dp)
                )
            }

            SamsungEditorRow(
                icon = { Icon(Icons.Default.AccessTime, null, tint = SamsungCalendarColors.green) },
                label = "All day"
            ) {
                Switch(
                    checked = allDay,
                    onCheckedChange = { allDay = it },
                    colors = SwitchDefaults.colors(
                        checkedThumbColor = Color.White,
                        checkedTrackColor = SamsungCalendarColors.green
                    )
                )
            }

            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 20.dp, vertical = 8.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                SamsungDateTimeColumn(
                    dateText = formatDayHeader(startMillis.toLocalDate()),
                    timeText = if (allDay) "" else formatTime(startMillis),
                    onDateClick = { showPicker = PickerTarget.START_DATE },
                    onTimeClick = { if (!allDay) showPicker = PickerTarget.START_TIME },
                    modifier = Modifier.weight(1f)
                )
                Icon(
                    Icons.AutoMirrored.Filled.ArrowForward,
                    contentDescription = null,
                    tint = SamsungCalendarColors.muted,
                    modifier = Modifier.padding(horizontal = 8.dp)
                )
                SamsungDateTimeColumn(
                    dateText = formatDayHeader(endMillis.toLocalDate()),
                    timeText = if (allDay) "" else formatTime(endMillis),
                    onDateClick = { showPicker = PickerTarget.END_DATE },
                    onTimeClick = { if (!allDay) showPicker = PickerTarget.END_TIME },
                    modifier = Modifier.weight(1f)
                )
            }

            HorizontalDivider(
                color = SamsungCalendarColors.divider,
                modifier = Modifier.padding(vertical = 8.dp)
            )

            SamsungDetailRow(
                icon = { Icon(Icons.Default.Person, null, tint = SamsungCalendarColors.green) },
                label = "Customer"
            ) {
                OutlinedTextField(
                    value = customerName,
                    onValueChange = { customerName = it },
                    placeholder = { Text("Customer name") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true,
                    colors = samsungFieldColors(),
                    keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.Words)
                )
            }

            SamsungDetailRow(
                icon = { Icon(Icons.Default.Phone, null, tint = SamsungCalendarColors.green) },
                label = "Phone"
            ) {
                OutlinedTextField(
                    value = customerPhone,
                    onValueChange = { customerPhone = it },
                    placeholder = { Text("Any phone number") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true,
                    colors = samsungFieldColors(),
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Phone)
                )
            }

            SamsungDetailRow(
                icon = { Icon(Icons.Default.LocationOn, null, tint = SamsungCalendarColors.green) },
                label = "Location"
            ) {
                OutlinedTextField(
                    value = address,
                    onValueChange = { address = it },
                    placeholder = { Text("Opens in Waze on Android") },
                    modifier = Modifier.fillMaxWidth(),
                    minLines = 1,
                    maxLines = 2,
                    colors = samsungFieldColors(),
                    keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.Words)
                )
            }

            SamsungDetailRow(
                icon = { Icon(Icons.Default.Notes, null, tint = SamsungCalendarColors.green) },
                label = "Notes"
            ) {
                OutlinedTextField(
                    value = jobNotes,
                    onValueChange = {
                        jobNotes = it
                        if (title.isBlank() || title == jobNotes) title = it
                    },
                    placeholder = { Text("Job details, parts, follow-up") },
                    modifier = Modifier.fillMaxWidth(),
                    minLines = 2,
                    maxLines = 4,
                    colors = samsungFieldColors()
                )
            }

            Spacer(modifier = Modifier.height(48.dp))
        }
    }
}

@Composable
private fun SamsungEditorRow(
    icon: @Composable () -> Unit,
    label: String,
    trailing: @Composable () -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 20.dp, vertical = 10.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            icon()
            Spacer(modifier = Modifier.width(16.dp))
            Text(label, color = MaterialTheme.colorScheme.onSurface, fontSize = 16.sp)
        }
        trailing()
    }
}

@Composable
private fun SamsungDetailRow(
    icon: @Composable () -> Unit,
    label: String,
    field: @Composable () -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 20.dp, vertical = 6.dp),
        verticalAlignment = Alignment.Top
    ) {
        icon()
        Spacer(modifier = Modifier.width(16.dp))
        Column(modifier = Modifier.weight(1f)) {
            Text(label, color = SamsungCalendarColors.muted, fontSize = 13.sp)
            Spacer(modifier = Modifier.height(4.dp))
            field()
        }
    }
}

@Composable
private fun SamsungDateTimeColumn(
    dateText: String,
    timeText: String,
    onDateClick: () -> Unit,
    onTimeClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    Column(modifier = modifier) {
        Text(
            text = dateText,
            fontWeight = FontWeight.Bold,
            fontSize = 16.sp,
            modifier = Modifier.clickable(onClick = onDateClick)
        )
        if (timeText.isNotBlank()) {
            Text(
                text = timeText,
                fontWeight = FontWeight.Bold,
                fontSize = 28.sp,
                modifier = Modifier
                    .padding(top = 4.dp)
                    .clickable(onClick = onTimeClick)
            )
        }
    }
}

@Composable
private fun samsungFieldColors() = OutlinedTextFieldDefaults.colors(
    focusedBorderColor = SamsungCalendarColors.green,
    unfocusedBorderColor = SamsungCalendarColors.divider,
    cursorColor = SamsungCalendarColors.green,
    focusedContainerColor = SamsungCalendarColors.surface,
    unfocusedContainerColor = SamsungCalendarColors.surface
)
