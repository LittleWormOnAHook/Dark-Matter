package com.expressmobileservice.inspection.ui

import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.combinedClickable
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.ArrowForward
import androidx.compose.material.icons.filled.AccessTime
import androidx.compose.material.icons.filled.DirectionsCar
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
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Switch
import androidx.compose.material3.SwitchDefaults
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
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
import com.expressmobileservice.inspection.AppointmentStore
import com.expressmobileservice.inspection.CustomerRecord
import com.expressmobileservice.inspection.distinctCustomerRecords
import com.expressmobileservice.inspection.searchCustomers
import com.expressmobileservice.inspection.VehicleCategory
import com.expressmobileservice.inspection.autofillLabel
import com.expressmobileservice.inspection.autofillSuggestions
import com.expressmobileservice.inspection.toClipboardText
import com.expressmobileservice.inspection.BUSINESS_DAY_START_HOUR
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

@OptIn(ExperimentalMaterial3Api::class, ExperimentalFoundationApi::class)
@Composable
fun AppointmentEditorScreen(
    appointmentStore: AppointmentStore,
    initial: Appointment?,
    defaultDate: LocalDate,
    prefilledJobNotes: String? = null,
    onDismiss: () -> Unit,
    onSave: (Appointment) -> Unit
) {
    val isEditing = initial != null
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
    var vehicleCategory by remember {
        mutableStateOf(
            runCatching {
                VehicleCategory.valueOf(initial?.vehicleCategory ?: VehicleCategory.CAR_TRUCK.name)
            }.getOrDefault(VehicleCategory.CAR_TRUCK)
        )
    }
    var vehicleYear by remember { mutableStateOf(initial?.vehicleYear) }
    var vehicleMake by remember { mutableStateOf(initial?.vehicleMake.orEmpty()) }
    var vehicleModel by remember { mutableStateOf(initial?.vehicleModel.orEmpty()) }
    var engineSize by remember { mutableStateOf(initial?.engineSize.orEmpty()) }
    var mileage by remember { mutableStateOf(initial?.mileage.orEmpty()) }
    var allDay by remember { mutableStateOf(initial?.allDay ?: false) }
    var startMillis by remember { mutableStateOf(defaultStart) }
    var endMillis by remember { mutableStateOf(defaultEnd) }
    var showPicker by remember { mutableStateOf<PickerTarget?>(null) }
    var validationError by remember { mutableStateOf<String?>(null) }
    val copyToClipboard = rememberCopyHandler()

    val customerRecords = remember(appointmentStore) {
        appointmentStore.getAll().distinctCustomerRecords()
    }

    val nameSuggestions = remember(customerRecords, customerName, isEditing) {
        if (isEditing || customerName.isBlank()) emptyList()
        else customerRecords.searchCustomers(customerName)
    }
    val phoneSuggestions = remember(customerRecords, customerPhone, isEditing) {
        if (isEditing || customerPhone.isBlank()) emptyList()
        else customerRecords.searchCustomers(customerPhone)
    }
    val addressSuggestions = remember(customerRecords, address, isEditing) {
        if (isEditing || address.isBlank()) emptyList()
        else customerRecords.searchCustomers(address)
    }

    val autofillQuery = remember(title, jobNotes, customerName) {
        listOf(title, jobNotes, customerName).firstOrNull { it.isNotBlank() }.orEmpty()
    }
    val autofillSuggestions = remember(appointmentStore, autofillQuery, defaultDate, isEditing) {
        val history = appointmentStore.getAll()
        if (isEditing) {
            history.autofillSuggestions(autofillQuery)
        } else {
            val forDay = history.autofillSuggestions("", defaultDate)
            val matched = history.autofillSuggestions(autofillQuery, defaultDate)
            (if (autofillQuery.isBlank()) forDay else matched)
                .ifEmpty { history.autofillSuggestions(autofillQuery) }
        }
    }

    fun applyAutofill(source: Appointment) {
        customerName = source.customerName
        customerPhone = source.customerPhone
        jobNotes = source.jobNotes
        title = when {
            source.jobNotes.isNotBlank() -> source.jobNotes
            source.customerName.isNotBlank() -> source.customerName
            else -> title
        }
        address = source.address
        vehicleCategory = runCatching {
            VehicleCategory.valueOf(source.vehicleCategory)
        }.getOrDefault(vehicleCategory)
        vehicleYear = source.vehicleYear
        vehicleMake = source.vehicleMake
        vehicleModel = source.vehicleModel
        engineSize = source.engineSize
        mileage = source.mileage
        if (!isEditing) {
            startMillis = defaultAppointmentStart(defaultDate)
            endMillis = defaultAppointmentEnd(startMillis)
        }
    }

    fun applyCustomerRecord(record: CustomerRecord) {
        applyAutofill(record.toAppointmentFields())
    }

    fun maybeAutofillFromPhone(phone: String) {
        if (isEditing) return
        val digits = phone.filter { it.isDigit() }
        if (digits.length < 7) return
        val exact = customerRecords.firstOrNull { it.customerPhone.filter { d -> d.isDigit() } == digits }
        if (exact != null && (customerName.isBlank() || customerName == exact.customerName)) {
            applyCustomerRecord(exact)
        }
    }

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
            validationError = "Enter a job title or customer name."
            return
        }
        if (!allDay && endMillis <= startMillis) {
            validationError = "End time must be after start time."
            return
        }
        validationError = null
        val inspectionId = initial?.inspectionId?.takeIf { it.isNotBlank() }
            ?: java.util.UUID.randomUUID().toString()
        onSave(
            Appointment(
                id = initial?.id ?: java.util.UUID.randomUUID().toString(),
                customerName = resolvedName,
                customerPhone = customerPhone.trim(),
                jobNotes = resolvedJob,
                address = address.trim(),
                vehicleCategory = vehicleCategory.name,
                vehicleYear = vehicleYear,
                vehicleMake = vehicleMake.trim(),
                vehicleModel = vehicleModel.trim(),
                engineSize = engineSize.trim(),
                mileage = mileage.trim(),
                inspectionId = inspectionId,
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
                    else -> LocalTime.of(BUSINESS_DAY_START_HOUR, 0)
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
                                    PickerTarget.START_TIME -> applyStartChange(combineDateAndTime(date, time))
                                    PickerTarget.END_TIME -> endMillis = combineDateAndTime(date, time)
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
        modifier = Modifier.imePadding(),
        containerColor = SamsungCalendarColors.background,
        topBar = {
            TopAppBar(
                title = { Text(if (isEditing) "Edit job" else "Add job") },
                navigationIcon = {
                    IconButton(onClick = onDismiss) {
                        Icon(
                            Icons.AutoMirrored.Filled.ArrowBack,
                            contentDescription = "Back to calendar",
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
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .background(SamsungCalendarColors.background)
                    .navigationBarsPadding()
                    .padding(horizontal = 20.dp, vertical = 12.dp),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                TextButton(onClick = onDismiss) {
                    Text(
                        "Cancel",
                        color = SamsungCalendarColors.green,
                        fontWeight = FontWeight.Bold,
                        fontSize = 17.sp
                    )
                }
                Button(
                    onClick = { save() },
                    colors = ButtonDefaults.buttonColors(
                        containerColor = SamsungCalendarColors.green,
                        contentColor = Color.Black
                    )
                ) {
                    Text("Save", fontWeight = FontWeight.Bold, fontSize = 17.sp)
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

            SectionHeader("Job & date / time")

            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 20.dp, vertical = 8.dp),
                horizontalArrangement = Arrangement.End
            ) {
                Box(
                    modifier = Modifier
                        .size(14.dp)
                        .clip(CircleShape)
                        .background(SamsungCalendarColors.green)
                )
            }

            CopyableTextField(
                value = title,
                onValueChange = {
                    title = it
                    if (jobNotes.isBlank() || jobNotes == title) jobNotes = it
                },
                placeholder = { Text("Job title", color = SamsungCalendarColors.muted) },
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp),
                singleLine = true,
                colors = samsungTitleFieldColors(),
                textStyle = MaterialTheme.typography.headlineSmall.copy(fontWeight = FontWeight.Normal)
            )

            EditorToggleRow(
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
                DateTimeColumn(
                    dateText = formatDayHeader(startMillis.toLocalDate()),
                    timeText = if (allDay) "" else formatTime(startMillis),
                    showTime = !allDay,
                    onDateClick = { showPicker = PickerTarget.START_DATE },
                    onTimeClick = { showPicker = PickerTarget.START_TIME },
                    modifier = Modifier.weight(1f)
                )
                Icon(
                    Icons.AutoMirrored.Filled.ArrowForward,
                    contentDescription = null,
                    tint = SamsungCalendarColors.muted,
                    modifier = Modifier.padding(horizontal = 8.dp)
                )
                DateTimeColumn(
                    dateText = formatDayHeader(endMillis.toLocalDate()),
                    timeText = if (allDay) "" else formatTime(endMillis),
                    showTime = !allDay,
                    onDateClick = { showPicker = PickerTarget.END_DATE },
                    onTimeClick = { showPicker = PickerTarget.END_TIME },
                    modifier = Modifier.weight(1f)
                )
            }

            if (!allDay) {
                Text(
                    text = "New jobs start at 8:00 AM. Changing start time sets end to 1 hour later on the same day.",
                    color = SamsungCalendarColors.muted,
                    fontSize = 12.sp,
                    modifier = Modifier.padding(horizontal = 20.dp, vertical = 4.dp)
                )
            }

            EditorField(
                icon = { Icon(Icons.Default.Notes, null, tint = SamsungCalendarColors.green) },
                label = "Notes"
            ) {
                CopyableOutlinedTextField(
                    value = jobNotes,
                    onValueChange = {
                        jobNotes = it
                        if (title.isBlank() || title == jobNotes) title = it
                    },
                    placeholder = { Text("Job details, parts, follow-up") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = false,
                    minLines = 2,
                    maxLines = 4,
                    colors = samsungFieldColors()
                )
                if (autofillSuggestions.isNotEmpty()) {
                    Text(
                        text = "Tap a previous job to fill fields (earliest time first). Hold to copy.",
                        color = SamsungCalendarColors.muted,
                        fontSize = 11.sp,
                        modifier = Modifier.padding(top = 6.dp, bottom = 4.dp)
                    )
                    LazyColumn(
                        modifier = Modifier
                            .fillMaxWidth()
                            .heightIn(max = 180.dp)
                    ) {
                        items(autofillSuggestions, key = { it.id }) { suggestion ->
                            Text(
                                text = suggestion.autofillLabel(),
                                fontSize = 13.sp,
                                color = SamsungCalendarColors.green,
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .combinedClickable(
                                        onClick = { applyAutofill(suggestion) },
                                        onLongClick = {
                                            copyToClipboard(
                                                suggestion.toClipboardText(),
                                                "Customer info copied"
                                            )
                                        }
                                    )
                                    .padding(vertical = 8.dp)
                            )
                            HorizontalDivider(color = SamsungCalendarColors.divider)
                        }
                    }
                }
            }

            HorizontalDivider(
                color = SamsungCalendarColors.divider,
                modifier = Modifier.padding(vertical = 12.dp)
            )

            SectionHeader("Customer info")

            EditorField(
                icon = { Icon(Icons.Default.Person, null, tint = SamsungCalendarColors.green) },
                label = "Customer name"
            ) {
                CopyableOutlinedTextField(
                    value = customerName,
                    onValueChange = { customerName = it },
                    placeholder = { Text("Customer name") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true,
                    colors = samsungFieldColors(),
                    keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.Words)
                )
                CustomerSuggestionList(
                    suggestions = nameSuggestions,
                    onSelect = { applyCustomerRecord(it) },
                    onCopy = copyToClipboard
                )
            }

            EditorField(
                icon = { Icon(Icons.Default.Phone, null, tint = SamsungCalendarColors.green) },
                label = "Phone"
            ) {
                CopyableOutlinedTextField(
                    value = customerPhone,
                    onValueChange = {
                        customerPhone = it
                        maybeAutofillFromPhone(it)
                    },
                    placeholder = { Text("Any phone number") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true,
                    colors = samsungFieldColors(),
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Phone)
                )
                CustomerSuggestionList(
                    suggestions = phoneSuggestions,
                    onSelect = { applyCustomerRecord(it) },
                    onCopy = copyToClipboard
                )
            }

            EditorField(
                icon = { Icon(Icons.Default.LocationOn, null, tint = SamsungCalendarColors.green) },
                label = "Address (opens Waze)"
            ) {
                CopyableOutlinedTextField(
                    value = address,
                    onValueChange = { address = it },
                    placeholder = { Text("Street address") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = false,
                    minLines = 1,
                    maxLines = 2,
                    colors = samsungFieldColors(),
                    keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.Words)
                )
                CustomerSuggestionList(
                    suggestions = addressSuggestions,
                    onSelect = { applyCustomerRecord(it) },
                    onCopy = copyToClipboard
                )
            }

            EditorField(
                icon = { Icon(Icons.Default.DirectionsCar, null, tint = SamsungCalendarColors.green) },
                label = "Vehicle"
            ) {
                VehicleDropdownFields(
                    vehicleCategory = vehicleCategory,
                    onCategoryChange = {
                        vehicleCategory = it
                        vehicleMake = ""
                        vehicleModel = ""
                        engineSize = ""
                    },
                    vehicleYear = vehicleYear,
                    onYearChange = {
                        vehicleYear = it
                        vehicleModel = ""
                    },
                    vehicleMake = vehicleMake,
                    onMakeChange = { vehicleMake = it },
                    vehicleModel = vehicleModel,
                    onModelChange = { vehicleModel = it },
                    engineSize = engineSize,
                    onEngineSizeChange = { engineSize = it },
                    mileage = mileage,
                    onMileageChange = { mileage = it },
                    modifier = Modifier.fillMaxWidth()
                )
            }

            Text(
                text = "Save creates the calendar job and an inspection file with this customer info. Long-press any field to copy its text.",
                color = SamsungCalendarColors.green,
                fontSize = 12.sp,
                modifier = Modifier.padding(horizontal = 20.dp, vertical = 12.dp)
            )

            Spacer(modifier = Modifier.height(24.dp))
        }
    }
}

@OptIn(ExperimentalFoundationApi::class)
@Composable
private fun CustomerSuggestionList(
    suggestions: List<CustomerRecord>,
    onSelect: (CustomerRecord) -> Unit,
    onCopy: (String, String) -> Unit
) {
    if (suggestions.isEmpty()) return
    Text(
        text = "Tap to fill · hold to copy",
        color = SamsungCalendarColors.muted,
        fontSize = 11.sp,
        modifier = Modifier.padding(top = 6.dp, bottom = 4.dp)
    )
    LazyColumn(
        modifier = Modifier
            .fillMaxWidth()
            .heightIn(max = 120.dp)
    ) {
        items(suggestions, key = { "${it.customerPhone}|${it.customerName}" }) { record ->
            Text(
                text = record.displayLabel(),
                fontSize = 13.sp,
                color = SamsungCalendarColors.green,
                modifier = Modifier
                    .fillMaxWidth()
                    .combinedClickable(
                        onClick = { onSelect(record) },
                        onLongClick = {
                            onCopy(record.toAppointmentFields().toClipboardText(), "Customer info copied")
                        }
                    )
                    .padding(vertical = 8.dp)
            )
            HorizontalDivider(color = SamsungCalendarColors.divider)
        }
    }
}

@Composable
private fun SectionHeader(text: String) {
    Text(
        text = text,
        fontWeight = FontWeight.Bold,
        fontSize = 14.sp,
        color = SamsungCalendarColors.green,
        modifier = Modifier.padding(horizontal = 20.dp, vertical = 8.dp)
    )
}

@Composable
private fun EditorToggleRow(
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
private fun EditorField(
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
private fun DateTimeColumn(
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
private fun samsungTitleFieldColors() = TextFieldDefaults.colors(
    focusedContainerColor = Color.Transparent,
    unfocusedContainerColor = Color.Transparent,
    focusedIndicatorColor = Color.Transparent,
    unfocusedIndicatorColor = Color.Transparent,
    cursorColor = SamsungCalendarColors.green
)

@Composable
private fun samsungFieldColors() = androidx.compose.material3.OutlinedTextFieldDefaults.colors(
    focusedBorderColor = SamsungCalendarColors.green,
    unfocusedBorderColor = SamsungCalendarColors.divider,
    cursorColor = SamsungCalendarColors.green,
    focusedContainerColor = SamsungCalendarColors.surface,
    unfocusedContainerColor = SamsungCalendarColors.surface
)
