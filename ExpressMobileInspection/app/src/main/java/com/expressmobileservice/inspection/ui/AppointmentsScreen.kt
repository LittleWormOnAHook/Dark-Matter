package com.expressmobileservice.inspection.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.border
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
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowLeft
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material.icons.filled.Phone
import androidx.compose.material.icons.filled.Today
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SegmentedButton
import androidx.compose.material3.SegmentedButtonDefaults
import androidx.compose.material3.SingleChoiceSegmentedButtonRow
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.expressmobileservice.inspection.Appointment
import com.expressmobileservice.inspection.AppointmentStore
import com.expressmobileservice.inspection.CalendarViewMode
import com.expressmobileservice.inspection.COMPANY_NAME
import com.expressmobileservice.inspection.appointmentsForDay
import com.expressmobileservice.inspection.appointmentsForWeek
import com.expressmobileservice.inspection.dialPhone
import com.expressmobileservice.inspection.formatDayHeader
import com.expressmobileservice.inspection.formatMonthAbbrev
import com.expressmobileservice.inspection.formatMonthYear
import com.expressmobileservice.inspection.formatTime
import com.expressmobileservice.inspection.formatTimeRange
import com.expressmobileservice.inspection.openWaze
import com.expressmobileservice.inspection.weekDaysContaining
import java.time.LocalDate
import java.time.YearMonth

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AppointmentsScreen(
    store: AppointmentStore,
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    var appointments by remember { mutableStateOf(store.getAll()) }
    var viewMode by remember { mutableStateOf(CalendarViewMode.MONTH) }
    var selectedDate by remember { mutableStateOf(LocalDate.now()) }
    var displayedMonth by remember { mutableStateOf(YearMonth.from(selectedDate)) }
    var showEditor by remember { mutableStateOf(false) }
    var editingAppointment by remember { mutableStateOf<Appointment?>(null) }
    var appointmentToDelete by remember { mutableStateOf<Appointment?>(null) }

    fun refresh() {
        appointments = store.getAll()
    }

    if (showEditor) {
        AppointmentEditorScreen(
            initial = editingAppointment,
            defaultDate = selectedDate,
            onDismiss = {
                showEditor = false
                editingAppointment = null
            },
            onSave = { appointment ->
                store.save(appointment)
                refresh()
                showEditor = false
                editingAppointment = null
            }
        )
        return
    }

    appointmentToDelete?.let { apt ->
        AlertDialog(
            onDismissRequest = { appointmentToDelete = null },
            title = { Text("Delete appointment?") },
            text = { Text("Remove ${apt.displayTitle}?") },
            confirmButton = {
                TextButton(
                    onClick = {
                        store.delete(apt.id)
                        refresh()
                        appointmentToDelete = null
                    }
                ) {
                    Text("Delete")
                }
            },
            dismissButton = {
                TextButton(onClick = { appointmentToDelete = null }) {
                    Text("Cancel")
                }
            }
        )
    }

    Scaffold(
        modifier = modifier,
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text(COMPANY_NAME, fontWeight = FontWeight.Bold)
                        Text(
                            text = when (viewMode) {
                                CalendarViewMode.MONTH -> formatMonthYear(displayedMonth)
                                CalendarViewMode.WEEK -> formatDayHeader(
                                    weekDaysContaining(selectedDate).first()
                                ) + " – " + formatDayHeader(
                                    weekDaysContaining(selectedDate).last()
                                )
                                CalendarViewMode.DAY -> formatDayHeader(selectedDate)
                            },
                            style = MaterialTheme.typography.bodySmall
                        )
                    }
                },
                actions = {
                    IconButton(onClick = {
                        selectedDate = LocalDate.now()
                        displayedMonth = YearMonth.from(selectedDate)
                    }) {
                        Icon(Icons.Default.Today, contentDescription = "Go to today")
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = MaterialTheme.colorScheme.primary,
                    titleContentColor = Color.White,
                    actionIconContentColor = Color.White
                )
            )
        },
        floatingActionButton = {
            FloatingActionButton(
                onClick = {
                    editingAppointment = null
                    showEditor = true
                },
                containerColor = MaterialTheme.colorScheme.tertiary
            ) {
                Icon(Icons.Default.Add, contentDescription = "Add appointment", tint = Color.White)
            }
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            ViewModeSelector(
                viewMode = viewMode,
                onViewModeChange = { viewMode = it },
                modifier = Modifier.padding(horizontal = 12.dp, vertical = 8.dp)
            )

            when (viewMode) {
                CalendarViewMode.MONTH -> {
                    MonthCalendarGrid(
                        yearMonth = displayedMonth,
                        selectedDate = selectedDate,
                        appointments = appointments,
                        onPreviousMonth = {
                            displayedMonth = displayedMonth.minusMonths(1)
                        },
                        onNextMonth = {
                            displayedMonth = displayedMonth.plusMonths(1)
                        },
                        onDateSelected = { date ->
                            selectedDate = date
                            displayedMonth = YearMonth.from(date)
                        },
                        modifier = Modifier.padding(horizontal = 8.dp)
                    )
                    HorizontalDivider(modifier = Modifier.padding(vertical = 4.dp))
                    DayAgendaList(
                        date = selectedDate,
                        appointments = appointmentsForDay(appointments, selectedDate),
                        onEdit = { apt ->
                            editingAppointment = apt
                            showEditor = true
                        },
                        onDelete = { appointmentToDelete = it },
                        onDial = { dialPhone(context, it.customerPhone) },
                        onNavigate = { openWaze(context, it.address) },
                        modifier = Modifier.weight(1f)
                    )
                }
                CalendarViewMode.WEEK -> {
                    WeekCalendarView(
                        anchorDate = selectedDate,
                        appointments = appointments,
                        onPreviousWeek = { selectedDate = selectedDate.minusWeeks(1) },
                        onNextWeek = { selectedDate = selectedDate.plusWeeks(1) },
                        onDateSelected = { selectedDate = it },
                        onEdit = { apt ->
                            editingAppointment = apt
                            showEditor = true
                        },
                        onDelete = { appointmentToDelete = it },
                        onDial = { dialPhone(context, it.customerPhone) },
                        onNavigate = { openWaze(context, it.address) },
                        modifier = Modifier.fillMaxSize()
                    )
                }
                CalendarViewMode.DAY -> {
                    DayHeaderNav(
                        date = selectedDate,
                        onPrevious = { selectedDate = selectedDate.minusDays(1) },
                        onNext = { selectedDate = selectedDate.plusDays(1) }
                    )
                    DayAgendaList(
                        date = selectedDate,
                        appointments = appointmentsForDay(appointments, selectedDate),
                        onEdit = { apt ->
                            editingAppointment = apt
                            showEditor = true
                        },
                        onDelete = { appointmentToDelete = it },
                        onDial = { dialPhone(context, it.customerPhone) },
                        onNavigate = { openWaze(context, it.address) },
                        modifier = Modifier.weight(1f)
                    )
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun ViewModeSelector(
    viewMode: CalendarViewMode,
    onViewModeChange: (CalendarViewMode) -> Unit,
    modifier: Modifier = Modifier
) {
    SingleChoiceSegmentedButtonRow(modifier = modifier.fillMaxWidth()) {
        CalendarViewMode.entries.forEachIndexed { index, mode ->
            SegmentedButton(
                selected = viewMode == mode,
                onClick = { onViewModeChange(mode) },
                shape = SegmentedButtonDefaults.itemShape(index, CalendarViewMode.entries.size),
                label = {
                    Text(
                        text = when (mode) {
                            CalendarViewMode.DAY -> "Day"
                            CalendarViewMode.WEEK -> "Week"
                            CalendarViewMode.MONTH -> "Month"
                        }
                    )
                }
            )
        }
    }
}

@Composable
private fun DayHeaderNav(
    date: LocalDate,
    onPrevious: () -> Unit,
    onNext: () -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 8.dp, vertical = 4.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        IconButton(onClick = onPrevious) {
            Icon(Icons.AutoMirrored.Filled.KeyboardArrowLeft, contentDescription = "Previous day")
        }
        Text(
            text = formatDayHeader(date),
            fontWeight = FontWeight.SemiBold,
            style = MaterialTheme.typography.titleMedium
        )
        IconButton(onClick = onNext) {
            Icon(Icons.AutoMirrored.Filled.KeyboardArrowRight, contentDescription = "Next day")
        }
    }
}

@Composable
private fun MonthCalendarGrid(
    yearMonth: YearMonth,
    selectedDate: LocalDate,
    appointments: List<Appointment>,
    onPreviousMonth: () -> Unit,
    onNextMonth: () -> Unit,
    onDateSelected: (LocalDate) -> Unit,
    modifier: Modifier = Modifier
) {
    val today = LocalDate.now()
    val days = remember(yearMonth) { com.expressmobileservice.inspection.daysInMonthGrid(yearMonth) }

    Column(modifier = modifier) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            IconButton(onClick = onPreviousMonth) {
                Icon(Icons.AutoMirrored.Filled.KeyboardArrowLeft, contentDescription = "Previous month")
            }
            Text(
                text = formatMonthAbbrev(yearMonth.atDay(1)).uppercase(),
                fontWeight = FontWeight.Bold,
                fontSize = 18.sp
            )
            IconButton(onClick = onNextMonth) {
                Icon(Icons.AutoMirrored.Filled.KeyboardArrowRight, contentDescription = "Next month")
            }
        }

        Row(modifier = Modifier.fillMaxWidth()) {
            listOf("S", "M", "T", "W", "T", "F", "S").forEach { label ->
                Text(
                    text = label,
                    modifier = Modifier.weight(1f),
                    textAlign = TextAlign.Center,
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }

        Spacer(modifier = Modifier.height(4.dp))

        days.chunked(7).forEach { week ->
            Row(modifier = Modifier.fillMaxWidth()) {
                week.forEach { date ->
                    val inMonth = date.month == yearMonth.month
                    val isSelected = date == selectedDate
                    val isToday = date == today
                    val dayAppointments = appointmentsForDay(appointments, date)

                    Box(
                        modifier = Modifier
                            .weight(1f)
                            .padding(2.dp)
                            .clip(RoundedCornerShape(8.dp))
                            .then(
                                if (isSelected) {
                                    Modifier.border(2.dp, MaterialTheme.colorScheme.onSurface, RoundedCornerShape(8.dp))
                                } else {
                                    Modifier
                                }
                            )
                            .clickable { onDateSelected(date) }
                            .padding(vertical = 6.dp),
                        contentAlignment = Alignment.TopCenter
                    ) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Text(
                                text = date.dayOfMonth.toString(),
                                fontSize = 13.sp,
                                fontWeight = if (isToday) FontWeight.Bold else FontWeight.Normal,
                                color = when {
                                    !inMonth -> MaterialTheme.colorScheme.onSurface.copy(alpha = 0.35f)
                                    isToday -> MaterialTheme.colorScheme.primary
                                    else -> MaterialTheme.colorScheme.onSurface
                                }
                            )
                            if (dayAppointments.isNotEmpty()) {
                                Spacer(modifier = Modifier.height(2.dp))
                                Row(horizontalArrangement = Arrangement.spacedBy(2.dp)) {
                                    dayAppointments.take(3).forEach { apt ->
                                        Box(
                                            modifier = Modifier
                                                .size(width = 14.dp, height = 3.dp)
                                                .clip(RoundedCornerShape(2.dp))
                                                .background(Color(apt.colorArgb))
                                        )
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun WeekCalendarView(
    anchorDate: LocalDate,
    appointments: List<Appointment>,
    onPreviousWeek: () -> Unit,
    onNextWeek: () -> Unit,
    onDateSelected: (LocalDate) -> Unit,
    onEdit: (Appointment) -> Unit,
    onDelete: (Appointment) -> Unit,
    onDial: (Appointment) -> Unit,
    onNavigate: (Appointment) -> Unit,
    modifier: Modifier = Modifier
) {
    val weekDays = weekDaysContaining(anchorDate)
    val weekAppointments = appointmentsForWeek(appointments, anchorDate)
    val today = LocalDate.now()

    Column(modifier = modifier) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 4.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            IconButton(onClick = onPreviousWeek) {
                Icon(Icons.AutoMirrored.Filled.KeyboardArrowLeft, contentDescription = "Previous week")
            }
            IconButton(onClick = onNextWeek) {
                Icon(Icons.AutoMirrored.Filled.KeyboardArrowRight, contentDescription = "Next week")
            }
        }

        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 8.dp)
        ) {
            weekDays.forEach { date ->
                val isSelected = date == anchorDate
                val isToday = date == today
                Column(
                    modifier = Modifier
                        .weight(1f)
                        .clip(RoundedCornerShape(8.dp))
                        .background(
                            if (isSelected) MaterialTheme.colorScheme.primaryContainer
                            else Color.Transparent
                        )
                        .clickable { onDateSelected(date) }
                        .padding(vertical = 8.dp),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Text(
                        text = date.dayOfWeek.name.take(1),
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    Text(
                        text = date.dayOfMonth.toString(),
                        fontWeight = if (isToday) FontWeight.Bold else FontWeight.Normal,
                        color = if (isToday) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.onSurface
                    )
                }
            }
        }

        HorizontalDivider(modifier = Modifier.padding(vertical = 8.dp))

        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 12.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            weekDays.forEach { date ->
                val dayItems = appointmentsForDay(weekAppointments, date)
                if (dayItems.isNotEmpty()) {
                    Text(
                        text = formatDayHeader(date),
                        fontWeight = FontWeight.SemiBold,
                        color = MaterialTheme.colorScheme.primary,
                        modifier = Modifier.padding(top = 4.dp)
                    )
                    dayItems.forEach { apt ->
                        AppointmentCard(
                            appointment = apt,
                            onEdit = { onEdit(apt) },
                            onDelete = { onDelete(apt) },
                            onDial = { onDial(apt) },
                            onNavigate = { onNavigate(apt) }
                        )
                    }
                }
            }
            if (weekAppointments.isEmpty()) {
                Text(
                    text = "No appointments this week.",
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(16.dp)
                )
            }
            Spacer(modifier = Modifier.height(72.dp))
        }
    }
}

@Composable
private fun DayAgendaList(
    date: LocalDate,
    appointments: List<Appointment>,
    onEdit: (Appointment) -> Unit,
    onDelete: (Appointment) -> Unit,
    onDial: (Appointment) -> Unit,
    onNavigate: (Appointment) -> Unit,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 12.dp)
    ) {
        Text(
            text = formatDayHeader(date),
            fontWeight = FontWeight.SemiBold,
            modifier = Modifier.padding(vertical = 8.dp)
        )
        if (appointments.isEmpty()) {
            Text(
                text = "No appointments. Tap + to add a customer job.",
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(vertical = 24.dp)
            )
        } else {
            appointments.forEach { apt ->
                AppointmentCard(
                    appointment = apt,
                    onEdit = { onEdit(apt) },
                    onDelete = { onDelete(apt) },
                    onDial = { onDial(apt) },
                    onNavigate = { onNavigate(apt) }
                )
                Spacer(modifier = Modifier.height(8.dp))
            }
        }
        Spacer(modifier = Modifier.height(80.dp))
    }
}

@Composable
private fun AppointmentCard(
    appointment: Appointment,
    onEdit: () -> Unit,
    onDelete: () -> Unit,
    onDial: () -> Unit,
    onNavigate: () -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant)
    ) {
        Row(modifier = Modifier.fillMaxWidth()) {
            Box(
                modifier = Modifier
                    .width(4.dp)
                    .height(80.dp)
                    .background(Color(appointment.colorArgb))
            )
            Column(
                modifier = Modifier
                    .weight(1f)
                    .padding(12.dp)
            ) {
                Text(
                    text = formatTimeRange(
                        appointment.startEpochMillis,
                        appointment.endEpochMillis,
                        appointment.allDay
                    ),
                    style = MaterialTheme.typography.labelMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Text(
                    text = appointment.displayTitle,
                    fontWeight = FontWeight.SemiBold,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis
                )
                if (appointment.customerPhone.isNotBlank()) {
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        modifier = Modifier
                            .clickable(onClick = onDial)
                            .padding(top = 4.dp)
                    ) {
                        Icon(
                            Icons.Default.Phone,
                            contentDescription = "Call",
                            modifier = Modifier.size(16.dp),
                            tint = MaterialTheme.colorScheme.tertiary
                        )
                        Spacer(modifier = Modifier.width(4.dp))
                        Text(
                            text = appointment.customerPhone,
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.tertiary
                        )
                    }
                }
                if (appointment.address.isNotBlank()) {
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        modifier = Modifier
                            .clickable(onClick = onNavigate)
                            .padding(top = 2.dp)
                    ) {
                        Icon(
                            Icons.Default.LocationOn,
                            contentDescription = "Open in Waze",
                            modifier = Modifier.size(16.dp),
                            tint = MaterialTheme.colorScheme.tertiary
                        )
                        Spacer(modifier = Modifier.width(4.dp))
                        Text(
                            text = appointment.address,
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.tertiary,
                            maxLines = 1,
                            overflow = TextOverflow.Ellipsis
                        )
                    }
                }
                if (appointment.jobNotes.isNotBlank() && appointment.customerName.isNotBlank()) {
                    Text(
                        text = appointment.jobNotes,
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        maxLines = 2,
                        overflow = TextOverflow.Ellipsis,
                        modifier = Modifier.padding(top = 2.dp)
                    )
                }
            }
            Column {
                IconButton(onClick = onEdit) {
                    Icon(Icons.Default.Edit, contentDescription = "Edit")
                }
                IconButton(onClick = onDelete) {
                    Icon(Icons.Default.Delete, contentDescription = "Delete")
                }
            }
        }
    }
}
