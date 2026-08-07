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
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.ArrowForward
import androidx.compose.material.icons.filled.AccessTime
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material.icons.filled.Notes
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Phone
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.DatePicker
import androidx.compose.material3.DatePickerDialog
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
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
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
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
import com.expressmobileservice.inspection.syncEndAfterStartChange
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

private enum class EditorStep {
    SCHEDULE,
    CUSTOMER
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

    var editorStep by remember { mutableStateOf(EditorStep.SCHEDULE) }
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

    fun applyStartChange(newStart: Long) {
        startMillis = newStart
        endMillis = syncEndAfterStartChange(newStart)
    }

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
            editorStep = EditorStep.CUSTOMER
            return
        }
        if (!allDay && endMillis <= startMillis) {
            validationError = "End time must be after start time."
            editorStep = EditorStep.SCHEDULE
            return
        }
        validationError = null
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
                                            applyStartChange(combineDateAndTime(pickedDate, time))
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
                                        applyStartChange(combineDateAndTime(date, time))
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
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        when (editorStep) {
                            EditorStep.SCHEDULE -> "Date & time"
                            EditorStep.CUSTOMER -> "Customer info"
                        }
                    )
                },
                navigationIcon = {
                    IconButton(
                        onClick = {
                            when (editorStep) {
                                EditorStep.SCHEDULE -> onDismiss()
                                EditorStep.CUSTOMER -> {
                                    validationError = null
                                    editorStep = EditorStep.SCHEDULE
                                }
                            }
                        }
                    ) {
                        Icon(
                            Icons.AutoMirrored.Filled.ArrowBack,
                            contentDescription = when (editorStep) {
                                EditorStep.SCHEDULE -> "Back to calendar"
                                EditorStep.CUSTOMER -> "Back to date and time"
                            },
                            tint = Color.White
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = SamsungCalendarColors.surface,
                    titleContentColor = Color.White,
                    navigationIconContentColor = Color.White
                )
            )
        },
        bottomBar = {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .background(SamsungCalendarColors.background)
                    .padding(horizontal = 16.dp, vertical = 12.dp)
            ) {
                when (editorStep) {
                    EditorStep.SCHEDULE -> {
                        Button(
                            onClick = {
                                validationError = null
                                editorStep = EditorStep.CUSTOMER
                            },
                            modifier = Modifier.fillMaxWidth(),
                            colors = ButtonDefaults.buttonColors(
                                containerColor = SamsungCalendarColors.green,
                                contentColor = Color.Black
                            )
                        ) {
                            Text("Next: Customer info", fontWeight = FontWeight.Bold)
                            Spacer(modifier = Modifier.width(8.dp))
                            Icon(Icons.AutoMirrored.Filled.ArrowForward, contentDescription = null)
                        }
                    }
                    EditorStep.CUSTOMER -> {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.spacedBy(12.dp)
                        ) {
                            OutlinedButton(
                                onClick = {
                                    validationError = null
                                    editorStep = EditorStep.SCHEDULE
                                },
                                modifier = Modifier.weight(1f),
                                colors = ButtonDefaults.outlinedButtonColors(
                                    contentColor = SamsungCalendarColors.green
                                )
                            ) {
                                Icon(
                                    Icons.AutoMirrored.Filled.ArrowBack,
                                    contentDescription = null,
                                    modifier = Modifier.size(18.dp)
                                )
                                Spacer(modifier = Modifier.width(4.dp))
                                Text("Date & time")
                            }
                            Button(
                                onClick = { save() },
                                modifier = Modifier.weight(1.2f),
                                colors = ButtonDefaults.buttonColors(
                                    containerColor = SamsungCalendarColors.green,
                                    contentColor = Color.Black
                                )
                            ) {
                                Text("Save", fontWeight = FontWeight.Bold, fontSize = 16.sp)
                            }
                        }
                    }
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
            validationError?.let { error ->
                Text(
                    text = error,
                    color = MaterialTheme.colorScheme.error,
                    modifier = Modifier.padding(horizontal = 20.dp, vertical = 8.dp)
                )
            }

            when (editorStep) {
                EditorStep.SCHEDULE -> ScheduleStepContent(
                    title = title,
                    onTitleChange = {
                        title = it
                        if (jobNotes.isBlank() || jobNotes == title) jobNotes = it
                    },
                    allDay = allDay,
                    onAllDayChange = { allDay = it },
                    startMillis = startMillis,
                    endMillis = endMillis,
                    onShowPicker = { showPicker = it }
                )
                EditorStep.CUSTOMER -> CustomerStepContent(
                    customerName = customerName,
                    onCustomerNameChange = { customerName = it },
                    customerPhone = customerPhone,
                    onCustomerPhoneChange = { customerPhone = it },
                    address = address,
                    onAddressChange = { address = it },
                    jobNotes = jobNotes,
                    onJobNotesChange = {
                        jobNotes = it
                        if (title.isBlank() || title == jobNotes) title = it
                    }
                )
            }

            Spacer(modifier = Modifier.height(24.dp))
        }
    }
}

@Composable
private fun ScheduleStepContent(
    title: String,
    onTitleChange: (String) -> Unit,
    allDay: Boolean,
    onAllDayChange: (Boolean) -> Unit,
    startMillis: Long,
    endMillis: Long,
    onShowPicker: (PickerTarget) -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 20.dp, vertical = 12.dp),
        horizontalArrangement = Arrangement.End
    ) {
        Box(
            modifier = Modifier
                .size(14.dp)
                .clip(CircleShape)
                .background(SamsungCalendarColors.green)
        )
    }

    TextField(
        value = title,
        onValueChange = onTitleChange,
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

    SamsungEditorRow(
        icon = { Icon(Icons.Default.AccessTime, null, tint = SamsungCalendarColors.green) },
        label = "All day"
    ) {
        Switch(
            checked = allDay,
            onCheckedChange = onAllDayChange,
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
            showTime = !allDay,
            onDateClick = { onShowPicker(PickerTarget.START_DATE) },
            onTimeClick = { onShowPicker(PickerTarget.START_TIME) },
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
            showTime = !allDay,
            onDateClick = { onShowPicker(PickerTarget.END_DATE) },
            onTimeClick = { onShowPicker(PickerTarget.END_TIME) },
            modifier = Modifier.weight(1f)
        )
    }

    if (!allDay) {
        Text(
            text = "End time auto-sets to 1 hour later on the same day when you change start.",
            color = SamsungCalendarColors.muted,
            fontSize = 12.sp,
            modifier = Modifier.padding(horizontal = 20.dp, vertical = 4.dp)
        )
    }
}

@Composable
private fun CustomerStepContent(
    customerName: String,
    onCustomerNameChange: (String) -> Unit,
    customerPhone: String,
    onCustomerPhoneChange: (String) -> Unit,
    address: String,
    onAddressChange: (String) -> Unit,
    jobNotes: String,
    onJobNotesChange: (String) -> Unit
) {
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
            onValueChange = onCustomerNameChange,
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
            onValueChange = onCustomerPhoneChange,
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
            onValueChange = onAddressChange,
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
        label = "Job / notes"
    ) {
        OutlinedTextField(
            value = jobNotes,
            onValueChange = onJobNotesChange,
            placeholder = { Text("Job details, parts, follow-up") },
            modifier = Modifier.fillMaxWidth(),
            minLines = 2,
            maxLines = 4,
            colors = samsungFieldColors()
        )
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
    showTime: Boolean,
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
        if (showTime) {
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
