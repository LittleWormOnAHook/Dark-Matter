package com.expressmobileservice.inspection.ui

import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.combinedClickable
import androidx.compose.foundation.pager.HorizontalPager
import androidx.compose.foundation.pager.rememberPagerState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowLeft
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Menu
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.runtime.snapshotFlow
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.expressmobileservice.inspection.Appointment
import com.expressmobileservice.inspection.AppointmentStore
import com.expressmobileservice.inspection.CalendarPreferencesStore
import com.expressmobileservice.inspection.InspectionStore
import com.expressmobileservice.inspection.CalendarViewMode
import com.expressmobileservice.inspection.appointmentsForDay
import com.expressmobileservice.inspection.compareAppointmentsForDay
import com.expressmobileservice.inspection.dialPhone
import com.expressmobileservice.inspection.formatDayHeader
import com.expressmobileservice.inspection.formatMonthAbbrev
import com.expressmobileservice.inspection.formatTimeRange
import com.expressmobileservice.inspection.hasSavedInspection
import com.expressmobileservice.inspection.daysInMonthGrid
import com.expressmobileservice.inspection.openWaze
import com.expressmobileservice.inspection.toClipboardText
import com.expressmobileservice.inspection.weekDaysContaining
import com.expressmobileservice.inspection.ui.theme.SamsungCalendarColors
import java.time.LocalDate
import java.time.YearMonth
import java.time.temporal.ChronoUnit

@OptIn(ExperimentalMaterial3Api::class, ExperimentalFoundationApi::class)
@Composable
fun AppointmentsScreen(
    store: AppointmentStore,
    inspectionStore: InspectionStore,
    onInspectionLinked: (String) -> Unit,
    onOpenInspection: (String) -> Unit,
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    val calendarPrefs = remember { CalendarPreferencesStore(context) }
    var appointments by remember { mutableStateOf(store.getAll()) }
    var viewMode by remember { mutableStateOf(calendarPrefs.getViewMode()) }
    var selectedDate by remember {
        mutableStateOf(calendarPrefs.getSelectedDate() ?: LocalDate.now())
    }
    var displayedMonth by remember { mutableStateOf(YearMonth.from(selectedDate)) }

    fun persistCalendarState() {
        calendarPrefs.setViewMode(viewMode)
        calendarPrefs.setSelectedDate(selectedDate)
    }
    var showEditor by remember { mutableStateOf(false) }
    var editingAppointment by remember { mutableStateOf<Appointment?>(null) }
    var appointmentToDelete by remember { mutableStateOf<Appointment?>(null) }
    var showViewMenu by remember { mutableStateOf(false) }
    var showSearch by remember { mutableStateOf(false) }
    var searchQuery by remember { mutableStateOf("") }
    var quickAddText by remember { mutableStateOf("") }
    var editorQuickNotes by remember { mutableStateOf<String?>(null) }
    var agendaActionTarget by remember { mutableStateOf<Appointment?>(null) }
    val copyToClipboard = rememberCopyHandler()

    fun refresh() {
        appointments = store.getAll()
    }

    val filteredAppointments = remember(appointments, searchQuery) {
        if (searchQuery.isBlank()) appointments
        else appointments.filter { apt ->
            val q = searchQuery.lowercase()
            apt.customerName.lowercase().contains(q) ||
                apt.customerPhone.contains(q) ||
                apt.jobNotes.lowercase().contains(q) ||
                apt.address.lowercase().contains(q)
        }
    }

    if (showEditor) {
        AppointmentEditorScreen(
            appointmentStore = store,
            initial = editingAppointment,
            defaultDate = selectedDate,
            prefilledJobNotes = editorQuickNotes,
            onDismiss = {
                showEditor = false
                editingAppointment = null
                editorQuickNotes = null
            },
            onSave = { appointment ->
                store.save(appointment)
                val inspection = inspectionStore.saveFromAppointment(appointment)
                if (appointment.inspectionId != inspection.id) {
                    store.save(appointment.copy(inspectionId = inspection.id))
                }
                refresh()
                onInspectionLinked(inspection.id)
                showEditor = false
                editingAppointment = null
                editorQuickNotes = null
                quickAddText = ""
            }
        )
        return
    }

    appointmentToDelete?.let { apt ->
        AlertDialog(
            onDismissRequest = { appointmentToDelete = null },
            title = { Text("Delete appointment?") },
            text = { Text("Remove ${apt.agendaTitle}?") },
            confirmButton = {
                TextButton(
                    onClick = {
                        store.delete(apt.id)
                        refresh()
                        appointmentToDelete = null
                    }
                ) {
                    Text("Delete", color = SamsungCalendarColors.green)
                }
            },
            dismissButton = {
                TextButton(onClick = { appointmentToDelete = null }) {
                    Text("Cancel")
                }
            }
        )
    }

    agendaActionTarget?.let { apt ->
        AlertDialog(
            onDismissRequest = { agendaActionTarget = null },
            title = { Text("Appointment") },
            text = { Text(apt.agendaTitle) },
            confirmButton = {
                TextButton(
                    onClick = {
                        copyToClipboard(apt.toClipboardText(), "Appointment copied")
                        agendaActionTarget = null
                    }
                ) {
                    Text("Copy text", color = SamsungCalendarColors.green)
                }
            },
            dismissButton = {
                TextButton(
                    onClick = {
                        agendaActionTarget = null
                        appointmentToDelete = apt
                    }
                ) {
                    Text("Delete")
                }
            }
        )
    }

    if (showViewMenu) {
        ModalBottomSheet(
            onDismissRequest = { showViewMenu = false },
            sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true),
            containerColor = SamsungCalendarColors.surface
        ) {
            Column(modifier = Modifier.padding(bottom = 32.dp)) {
                Text(
                    text = "Calendar view",
                    modifier = Modifier.padding(horizontal = 24.dp, vertical = 8.dp),
                    fontWeight = FontWeight.SemiBold,
                    color = SamsungCalendarColors.muted
                )
                CalendarViewMode.entries.forEach { mode ->
                    Text(
                        text = when (mode) {
                            CalendarViewMode.DAY -> "Day"
                            CalendarViewMode.WEEK -> "Week"
                            CalendarViewMode.MONTH -> "Month"
                        },
                        modifier = Modifier
                            .fillMaxWidth()
                            .clickable {
                                viewMode = mode
                                if (mode == CalendarViewMode.MONTH) {
                                    displayedMonth = YearMonth.from(selectedDate)
                                }
                                persistCalendarState()
                                showViewMenu = false
                            }
                            .padding(horizontal = 24.dp, vertical = 16.dp),
                        fontSize = 18.sp,
                        fontWeight = if (viewMode == mode) FontWeight.Bold else FontWeight.Normal,
                        color = if (viewMode == mode) SamsungCalendarColors.green else MaterialTheme.colorScheme.onSurface
                    )
                }
            }
        }
    }

    Scaffold(
        modifier = modifier.background(SamsungCalendarColors.background),
        containerColor = SamsungCalendarColors.background,
        floatingActionButton = {
            FloatingActionButton(
                onClick = {
                    editingAppointment = null
                    editorQuickNotes = null
                    showEditor = true
                },
                containerColor = SamsungCalendarColors.green,
                contentColor = Color.Black
            ) {
                Icon(Icons.Default.Add, contentDescription = "Add appointment")
            }
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .background(SamsungCalendarColors.background)
        ) {
            SamsungCalendarHeader(
                viewMode = viewMode,
                displayedMonth = displayedMonth,
                selectedDate = selectedDate,
                showSearch = showSearch,
                searchQuery = searchQuery,
                onMenuClick = { showViewMenu = true },
                onSearchToggle = {
                    showSearch = !showSearch
                    if (!showSearch) searchQuery = ""
                },
                onSearchChange = { searchQuery = it },
                onGoToToday = {
                    selectedDate = LocalDate.now()
                    displayedMonth = YearMonth.from(selectedDate)
                    persistCalendarState()
                }
            )

            when (viewMode) {
                CalendarViewMode.MONTH -> {
                    MonthCalendarGrid(
                        yearMonth = displayedMonth,
                        selectedDate = selectedDate,
                        appointments = filteredAppointments,
                        onMonthChange = { displayedMonth = it },
                        onDateSelected = { date ->
                            selectedDate = date
                            displayedMonth = YearMonth.from(date)
                            persistCalendarState()
                        },
                        modifier = Modifier.padding(horizontal = 4.dp)
                    )
                    HorizontalDivider(color = SamsungCalendarColors.divider, thickness = 1.dp)
                    SamsungAgendaPanel(
                        date = selectedDate,
                        appointments = appointmentsForDay(filteredAppointments, selectedDate),
                        quickAddText = quickAddText,
                        onQuickAddChange = { quickAddText = it },
                        onQuickAddSubmit = {
                            if (quickAddText.isNotBlank()) {
                                editorQuickNotes = quickAddText.trim()
                                editingAppointment = null
                                showEditor = true
                            }
                        },
                        onAppointmentClick = { apt ->
                            editingAppointment = apt
                            showEditor = true
                        },
                        onAppointmentLongPress = { agendaActionTarget = it },
                        onDial = { dialPhone(context, it.customerPhone) },
                        onNavigate = { openWaze(context, it.address) },
                        inspectionStore = inspectionStore,
                        onOpenInspection = onOpenInspection,
                        modifier = Modifier.weight(1f)
                    )
                }
                CalendarViewMode.WEEK -> {
                    WeekCalendarView(
                        anchorDate = selectedDate,
                        appointments = filteredAppointments,
                        onAnchorDateChange = {
                            selectedDate = it
                            persistCalendarState()
                        },
                        modifier = Modifier.fillMaxWidth()
                    )
                    HorizontalDivider(color = SamsungCalendarColors.divider, thickness = 1.dp)
                    SamsungAgendaPanel(
                        date = selectedDate,
                        appointments = appointmentsForDay(filteredAppointments, selectedDate),
                        quickAddText = quickAddText,
                        onQuickAddChange = { quickAddText = it },
                        onQuickAddSubmit = {
                            if (quickAddText.isNotBlank()) {
                                editorQuickNotes = quickAddText.trim()
                                editingAppointment = null
                                showEditor = true
                            }
                        },
                        onAppointmentClick = { apt ->
                            editingAppointment = apt
                            showEditor = true
                        },
                        onAppointmentLongPress = { agendaActionTarget = it },
                        onDial = { dialPhone(context, it.customerPhone) },
                        onNavigate = { openWaze(context, it.address) },
                        inspectionStore = inspectionStore,
                        onOpenInspection = onOpenInspection,
                        modifier = Modifier.weight(1f)
                    )
                }
                CalendarViewMode.DAY -> {
                    DayHeaderNav(
                        date = selectedDate,
                        appointmentCount = appointmentsForDay(filteredAppointments, selectedDate).size,
                        onPrevious = {
                            selectedDate = selectedDate.minusDays(1)
                            persistCalendarState()
                        },
                        onNext = {
                            selectedDate = selectedDate.plusDays(1)
                            persistCalendarState()
                        }
                    )
                    SamsungAgendaPanel(
                        date = selectedDate,
                        appointments = appointmentsForDay(filteredAppointments, selectedDate),
                        quickAddText = quickAddText,
                        onQuickAddChange = { quickAddText = it },
                        onQuickAddSubmit = {
                            if (quickAddText.isNotBlank()) {
                                editorQuickNotes = quickAddText.trim()
                                editingAppointment = null
                                showEditor = true
                            }
                        },
                        onAppointmentClick = { apt ->
                            editingAppointment = apt
                            showEditor = true
                        },
                        onAppointmentLongPress = { agendaActionTarget = it },
                        onDial = { dialPhone(context, it.customerPhone) },
                        onNavigate = { openWaze(context, it.address) },
                        inspectionStore = inspectionStore,
                        onOpenInspection = onOpenInspection,
                        modifier = Modifier.weight(1f)
                    )
                }
            }
        }
    }
}

@Composable
private fun SamsungCalendarHeader(
    viewMode: CalendarViewMode,
    displayedMonth: YearMonth,
    selectedDate: LocalDate,
    showSearch: Boolean,
    searchQuery: String,
    onMenuClick: () -> Unit,
    onSearchToggle: () -> Unit,
    onSearchChange: (String) -> Unit,
    onGoToToday: () -> Unit
) {
    val today = LocalDate.now()
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .background(SamsungCalendarColors.background)
            .padding(horizontal = 4.dp, vertical = 8.dp)
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            IconButton(onClick = onMenuClick) {
                Icon(
                    Icons.Default.Menu,
                    contentDescription = "Calendar views",
                    tint = MaterialTheme.colorScheme.onSurface
                )
            }

            Text(
                text = when (viewMode) {
                    CalendarViewMode.MONTH -> formatMonthAbbrev(displayedMonth.atDay(1)).uppercase()
                    CalendarViewMode.WEEK -> formatMonthAbbrev(selectedDate).uppercase()
                    CalendarViewMode.DAY -> formatMonthAbbrev(selectedDate).uppercase()
                },
                fontWeight = FontWeight.Bold,
                fontSize = 22.sp,
                letterSpacing = 1.sp
            )

            Row(verticalAlignment = Alignment.CenterVertically) {
                IconButton(onClick = onSearchToggle) {
                    Icon(
                        if (showSearch) Icons.Default.Close else Icons.Default.Search,
                        contentDescription = "Search",
                        tint = MaterialTheme.colorScheme.onSurface
                    )
                }
                Box(
                    modifier = Modifier
                        .padding(end = 8.dp)
                        .size(36.dp)
                        .clip(RoundedCornerShape(8.dp))
                        .border(1.dp, SamsungCalendarColors.muted, RoundedCornerShape(8.dp))
                        .clickable(onClick = onGoToToday),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        text = today.dayOfMonth.toString(),
                        fontWeight = FontWeight.Bold,
                        fontSize = 16.sp
                    )
                }
            }
        }

        if (showSearch) {
            CopyableOutlinedTextField(
                value = searchQuery,
                onValueChange = onSearchChange,
                placeholder = { Text("Search customers, phone, jobs…") },
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 12.dp, vertical = 4.dp),
                singleLine = true,
                colors = OutlinedTextFieldDefaults.colors(
                    focusedBorderColor = SamsungCalendarColors.green,
                    cursorColor = SamsungCalendarColors.green
                )
            )
        }
    }
}

@Composable
private fun DayHeaderNav(
    date: LocalDate,
    appointmentCount: Int,
    onPrevious: () -> Unit,
    onNext: () -> Unit
) {
    Column(modifier = Modifier.fillMaxWidth()) {
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
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Text(
                    text = formatDayHeader(date),
                    fontWeight = FontWeight.SemiBold,
                    style = MaterialTheme.typography.titleMedium
                )
                if (appointmentCount > 0) {
                    AppointmentGreenIndicators(
                        appointmentCount = appointmentCount,
                        modifier = Modifier
                            .width(72.dp)
                            .padding(top = 6.dp)
                    )
                }
            }
            IconButton(onClick = onNext) {
                Icon(Icons.AutoMirrored.Filled.KeyboardArrowRight, contentDescription = "Next day")
            }
        }
    }
}

@OptIn(ExperimentalFoundationApi::class)
@Composable
private fun MonthCalendarGrid(
    yearMonth: YearMonth,
    selectedDate: LocalDate,
    appointments: List<Appointment>,
    onMonthChange: (YearMonth) -> Unit,
    onDateSelected: (LocalDate) -> Unit,
    modifier: Modifier = Modifier
) {
    val monthAnchor = remember { YearMonth.now() }
    val centerPage = 1_200
    val pageCount = centerPage * 2 + 1
    val pagerState = rememberPagerState(
        initialPage = centerPage + monthsBetween(monthAnchor, yearMonth),
        pageCount = { pageCount }
    )

    fun pageToMonth(page: Int): YearMonth = monthAnchor.plusMonths((page - centerPage).toLong())

    LaunchedEffect(yearMonth) {
        val targetPage = centerPage + monthsBetween(monthAnchor, yearMonth)
        if (pagerState.currentPage != targetPage && !pagerState.isScrollInProgress) {
            pagerState.scrollToPage(targetPage)
        }
    }

    LaunchedEffect(pagerState) {
        snapshotFlow { pagerState.settledPage }
            .collect { page ->
                val month = pageToMonth(page)
                if (month != yearMonth) onMonthChange(month)
            }
    }

    Column(modifier = modifier.padding(horizontal = 8.dp)) {
        HorizontalPager(
            state = pagerState,
            modifier = Modifier.fillMaxWidth()
        ) { page ->
            MonthCalendarPage(
                yearMonth = pageToMonth(page),
                selectedDate = selectedDate,
                appointments = appointments,
                onDateSelected = onDateSelected
            )
        }

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            IconButton(
                onClick = { onMonthChange(yearMonth.minusMonths(1)) },
                modifier = Modifier.size(32.dp)
            ) {
                Icon(
                    Icons.AutoMirrored.Filled.KeyboardArrowLeft,
                    contentDescription = "Previous month",
                    modifier = Modifier.size(20.dp)
                )
            }
            IconButton(
                onClick = { onMonthChange(yearMonth.plusMonths(1)) },
                modifier = Modifier.size(32.dp)
            ) {
                Icon(
                    Icons.AutoMirrored.Filled.KeyboardArrowRight,
                    contentDescription = "Next month",
                    modifier = Modifier.size(20.dp)
                )
            }
        }
    }
}

@Composable
private fun MonthCalendarPage(
    yearMonth: YearMonth,
    selectedDate: LocalDate,
    appointments: List<Appointment>,
    onDateSelected: (LocalDate) -> Unit
) {
    val today = LocalDate.now()
    val days = remember(yearMonth) { daysInMonthGrid(yearMonth) }
    val dayLabels = listOf("M", "T", "W", "T", "F", "S", "S")

    Column(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(bottom = 6.dp)
        ) {
            dayLabels.forEach { label ->
                Text(
                    text = label,
                    modifier = Modifier.weight(1f),
                    textAlign = TextAlign.Center,
                    style = MaterialTheme.typography.labelSmall,
                    color = SamsungCalendarColors.muted,
                    fontSize = 12.sp,
                    fontWeight = FontWeight.Medium
                )
            }
        }

        days.chunked(7).forEach { week ->
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .heightIn(min = 52.dp)
                    .padding(vertical = 2.dp),
                verticalAlignment = Alignment.Top
            ) {
                week.forEach { date ->
                    SamsungDayCell(
                        date = date,
                        inMonth = date.month == yearMonth.month,
                        isSelected = date == selectedDate,
                        isToday = date == today,
                        appointments = appointmentsForDay(appointments, date),
                        onClick = { onDateSelected(date) },
                        modifier = Modifier.weight(1f)
                    )
                }
            }
        }
    }
}

private fun monthsBetween(start: YearMonth, end: YearMonth): Int =
    (end.year - start.year) * 12 + (end.monthValue - start.monthValue)

@Composable
private fun SamsungDayCell(
    date: LocalDate,
    inMonth: Boolean,
    isSelected: Boolean,
    isToday: Boolean,
    appointments: List<Appointment>,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier
            .padding(horizontal = 2.dp, vertical = 2.dp)
            .clickable(onClick = onClick),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Box(
            modifier = Modifier
                .size(32.dp)
                .then(
                    if (isSelected) {
                        Modifier.border(1.5.dp, SamsungCalendarColors.selectedRing, CircleShape)
                    } else Modifier
                ),
            contentAlignment = Alignment.Center
        ) {
            Text(
                text = date.dayOfMonth.toString(),
                fontSize = 13.sp,
                fontWeight = if (isToday || isSelected) FontWeight.Bold else FontWeight.Normal,
                color = when {
                    !inMonth -> SamsungCalendarColors.muted.copy(alpha = 0.45f)
                    isToday -> SamsungCalendarColors.green
                    else -> MaterialTheme.colorScheme.onSurface
                }
            )
        }
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .heightIn(min = 10.dp)
                .padding(horizontal = 4.dp, vertical = 2.dp),
            verticalArrangement = Arrangement.spacedBy(2.dp)
        ) {
            AppointmentGreenIndicators(
                appointmentCount = appointments.size,
                modifier = Modifier.fillMaxWidth(),
                maxLines = 3
            )
        }
    }
}

@Composable
private fun AppointmentGreenIndicators(
    appointmentCount: Int,
    modifier: Modifier = Modifier,
    maxLines: Int = 4
) {
    if (appointmentCount <= 0) return
    Column(
        modifier = modifier,
        verticalArrangement = Arrangement.spacedBy(2.dp)
    ) {
        repeat(appointmentCount.coerceAtMost(maxLines)) {
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(2.dp)
                    .clip(RoundedCornerShape(1.dp))
                    .background(SamsungCalendarColors.green)
            )
        }
    }
}

@Composable
private fun SamsungAgendaPanel(
    date: LocalDate,
    appointments: List<Appointment>,
    quickAddText: String,
    onQuickAddChange: (String) -> Unit,
    onQuickAddSubmit: () -> Unit,
    onAppointmentClick: (Appointment) -> Unit,
    onAppointmentLongPress: (Appointment) -> Unit,
    onDial: (Appointment) -> Unit,
    onNavigate: (Appointment) -> Unit,
    inspectionStore: InspectionStore,
    onOpenInspection: (String) -> Unit,
    modifier: Modifier = Modifier
) {
    val sortedAppointments = remember(date, appointments) {
        appointments.sortedWith(compareAppointmentsForDay(date))
    }

    Column(
        modifier = modifier
            .fillMaxWidth()
            .background(SamsungCalendarColors.agendaSurface)
    ) {
        Column(
            modifier = Modifier
                .weight(1f)
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 16.dp, vertical = 8.dp)
        ) {
            if (sortedAppointments.isEmpty()) {
                Text(
                    text = "No events",
                    color = SamsungCalendarColors.muted,
                    modifier = Modifier.padding(vertical = 16.dp)
                )
            } else {
                sortedAppointments.forEach { apt ->
                    SamsungAgendaRow(
                        appointment = apt,
                        inspectionStore = inspectionStore,
                        onClick = { onAppointmentClick(apt) },
                        onLongClick = { onAppointmentLongPress(apt) },
                        onDial = { onDial(apt) },
                        onNavigate = { onNavigate(apt) },
                        onOpenInspection = { onOpenInspection(apt.inspectionId) }
                    )
                    Spacer(modifier = Modifier.height(12.dp))
                }
            }
            Spacer(modifier = Modifier.height(72.dp))
        }

        Row(
            modifier = Modifier
                .fillMaxWidth()
                .background(SamsungCalendarColors.background)
                .padding(horizontal = 16.dp, vertical = 10.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            CopyableOutlinedTextField(
                value = quickAddText,
                onValueChange = onQuickAddChange,
                placeholder = {
                    Text("7 PM brake job", color = SamsungCalendarColors.muted)
                },
                modifier = Modifier.weight(1f),
                singleLine = true,
                colors = OutlinedTextFieldDefaults.colors(
                    focusedContainerColor = SamsungCalendarColors.quickAddField,
                    unfocusedContainerColor = SamsungCalendarColors.quickAddField,
                    focusedBorderColor = Color.Transparent,
                    unfocusedBorderColor = Color.Transparent,
                    cursorColor = SamsungCalendarColors.green
                ),
                keyboardOptions = KeyboardOptions(imeAction = ImeAction.Done),
                keyboardActions = KeyboardActions(onDone = { onQuickAddSubmit() })
            )
        }
    }
}

@OptIn(ExperimentalFoundationApi::class)
@Composable
private fun SamsungAgendaRow(
    appointment: Appointment,
    inspectionStore: InspectionStore,
    onClick: () -> Unit,
    onLongClick: () -> Unit,
    onDial: () -> Unit,
    onNavigate: () -> Unit,
    onOpenInspection: () -> Unit
) {
    val showInsp = appointment.hasSavedInspection(inspectionStore)
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(8.dp))
            .background(SamsungCalendarColors.quickAddField.copy(alpha = 0.55f))
            .combinedClickable(
                onClick = onClick,
                onLongClick = onLongClick
            )
            .padding(horizontal = 8.dp, vertical = 8.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            text = formatTimeRange(
                appointment.startEpochMillis,
                appointment.endEpochMillis,
                appointment.allDay
            ),
            modifier = Modifier.width(96.dp),
            fontSize = 12.sp,
            color = SamsungCalendarColors.muted,
            lineHeight = 16.sp
        )
        Box(
            modifier = Modifier
                .width(3.dp)
                .height(44.dp)
                .clip(RoundedCornerShape(2.dp))
                .background(SamsungCalendarColors.green)
        )
        Spacer(modifier = Modifier.width(10.dp))
        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = appointment.agendaTitle,
                fontSize = 15.sp,
                maxLines = 2,
                overflow = TextOverflow.Ellipsis,
                lineHeight = 20.sp
            )
            if (appointment.hasPhone) {
                Text(
                    text = "Tap to call",
                    fontSize = 11.sp,
                    color = SamsungCalendarColors.green,
                    modifier = Modifier
                        .padding(top = 2.dp)
                        .clickable(onClick = onDial)
                )
            }
            if (appointment.hasAddress) {
                Text(
                    text = appointment.address,
                    fontSize = 11.sp,
                    color = SamsungCalendarColors.green,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                    modifier = Modifier
                        .padding(top = 2.dp)
                        .clickable(onClick = onNavigate)
                )
            }
        }
        if (showInsp) {
            Spacer(modifier = Modifier.width(8.dp))
            InspBadgeButton(onClick = onOpenInspection)
        }
    }
}

@OptIn(ExperimentalFoundationApi::class)
@Composable
private fun WeekCalendarView(
    anchorDate: LocalDate,
    appointments: List<Appointment>,
    onAnchorDateChange: (LocalDate) -> Unit,
    modifier: Modifier = Modifier
) {
    val weekAnchor = remember { weekDaysContaining(LocalDate.now()).first() }
    val centerPage = 1_200
    val pageCount = centerPage * 2 + 1
    val pagerState = rememberPagerState(
        initialPage = centerPage + weeksBetween(weekAnchor, weekStartFor(anchorDate)),
        pageCount = { pageCount }
    )

    fun pageToWeekStart(page: Int): LocalDate =
        weekAnchor.plusWeeks((page - centerPage).toLong())

    LaunchedEffect(anchorDate) {
        val targetPage = centerPage + weeksBetween(weekAnchor, weekStartFor(anchorDate))
        if (pagerState.currentPage != targetPage && !pagerState.isScrollInProgress) {
            pagerState.scrollToPage(targetPage)
        }
    }

    LaunchedEffect(pagerState) {
        snapshotFlow { pagerState.settledPage }
            .collect { page ->
                val newWeekStart = pageToWeekStart(page)
                val currentWeekStart = weekStartFor(anchorDate)
                if (newWeekStart != currentWeekStart) {
                    val dayOffset = ChronoUnit.DAYS.between(currentWeekStart, anchorDate)
                    onAnchorDateChange(newWeekStart.plusDays(dayOffset))
                }
            }
    }

    Column(modifier = modifier.background(SamsungCalendarColors.background)) {
        HorizontalPager(
            state = pagerState,
            modifier = Modifier.fillMaxWidth()
        ) { page ->
            val weekStart = pageToWeekStart(page)
            val highlightDate = if (weekStartFor(anchorDate) == weekStart) {
                anchorDate
            } else {
                weekStart.plusDays(ChronoUnit.DAYS.between(weekStartFor(anchorDate), anchorDate))
            }
            WeekDayHeaderRow(
                weekStart = weekStart,
                selectedDate = highlightDate,
                appointments = appointments,
                onDateSelected = onAnchorDateChange
            )
        }

        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 4.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            IconButton(onClick = { onAnchorDateChange(anchorDate.minusWeeks(1)) }) {
                Icon(Icons.AutoMirrored.Filled.KeyboardArrowLeft, contentDescription = "Previous week")
            }
            Text(
                text = formatDayHeader(anchorDate),
                fontWeight = FontWeight.SemiBold,
                color = SamsungCalendarColors.muted,
                fontSize = 13.sp
            )
            IconButton(onClick = { onAnchorDateChange(anchorDate.plusWeeks(1)) }) {
                Icon(Icons.AutoMirrored.Filled.KeyboardArrowRight, contentDescription = "Next week")
            }
        }
    }
}

@Composable
private fun WeekDayHeaderRow(
    weekStart: LocalDate,
    selectedDate: LocalDate,
    appointments: List<Appointment>,
    onDateSelected: (LocalDate) -> Unit
) {
    val weekDays = weekDaysContaining(weekStart)
    val today = LocalDate.now()

    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 8.dp, vertical = 4.dp)
    ) {
        weekDays.forEach { date ->
            val isSelected = date == selectedDate
            val isToday = date == today
            val dayCount = appointmentsForDay(appointments, date).size
            Column(
                modifier = Modifier
                    .weight(1f)
                    .clip(RoundedCornerShape(8.dp))
                    .then(
                        if (isSelected) {
                            Modifier.border(1.5.dp, SamsungCalendarColors.selectedRing, RoundedCornerShape(8.dp))
                        } else Modifier
                    )
                    .clickable { onDateSelected(date) }
                    .padding(vertical = 8.dp, horizontal = 2.dp),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Text(
                    text = date.dayOfWeek.getDisplayName(java.time.format.TextStyle.SHORT, java.util.Locale.getDefault())
                        .take(1)
                        .uppercase(),
                    style = MaterialTheme.typography.labelSmall,
                    color = SamsungCalendarColors.muted
                )
                Text(
                    text = date.dayOfMonth.toString(),
                    fontWeight = if (isToday || isSelected) FontWeight.Bold else FontWeight.Normal,
                    color = when {
                        isToday -> SamsungCalendarColors.green
                        isSelected -> MaterialTheme.colorScheme.onSurface
                        else -> MaterialTheme.colorScheme.onSurface
                    }
                )
                AppointmentGreenIndicators(
                    appointmentCount = dayCount,
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 4.dp, vertical = 4.dp),
                    maxLines = 3
                )
            }
        }
    }
}

private fun weekStartFor(date: LocalDate): LocalDate = weekDaysContaining(date).first()

private fun weeksBetween(startWeek: LocalDate, endWeek: LocalDate): Int =
    ChronoUnit.WEEKS.between(startWeek, endWeek).toInt()
