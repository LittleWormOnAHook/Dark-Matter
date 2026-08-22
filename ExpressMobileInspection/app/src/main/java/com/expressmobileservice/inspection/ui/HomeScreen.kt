package com.expressmobileservice.inspection.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Build
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.NavigationBarItemDefaults
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.expressmobileservice.inspection.AppointmentStore
import com.expressmobileservice.inspection.InspectionFormState
import com.expressmobileservice.inspection.InspectionStore
import com.expressmobileservice.inspection.ui.theme.SamsungCalendarColors

enum class ExpressTab {
    APPOINTMENTS,
    INSPECTION
}

@Composable
fun HomeScreen(
    appointmentStore: AppointmentStore,
    inspectionStore: InspectionStore,
    onShareReport: (InspectionFormState, ReportShareType, (Boolean) -> Unit) -> Unit,
    onShareError: (String) -> Unit,
    onNotify: (String) -> Unit = {},
    key: Int = 0,
    showRestoreBanner: Boolean = false,
    onRestoreFromDownloads: () -> Unit = {},
    onImportBackupFile: () -> Unit = {}
) {
    var selectedTab by remember(key) { mutableStateOf(ExpressTab.APPOINTMENTS) }
    var activeInspectionId by remember(key) {
        mutableStateOf(inspectionStore.mostRecent()?.id)
    }

    Scaffold(
        modifier = Modifier
            .fillMaxSize()
            .background(SamsungCalendarColors.background),
        containerColor = SamsungCalendarColors.background,
        bottomBar = {
            NavigationBar(
                containerColor = SamsungCalendarColors.surface,
                contentColor = SamsungCalendarColors.onBackground
            ) {
                NavigationBarItem(
                    selected = selectedTab == ExpressTab.APPOINTMENTS,
                    onClick = { selectedTab = ExpressTab.APPOINTMENTS },
                    icon = {
                        Icon(
                            imageVector = Icons.Default.CalendarMonth,
                            contentDescription = "Appointments"
                        )
                    },
                    label = { Text("Appointments") },
                    colors = NavigationBarItemDefaults.colors(
                        selectedIconColor = SamsungCalendarColors.green,
                        selectedTextColor = SamsungCalendarColors.green,
                        indicatorColor = SamsungCalendarColors.quickAddField,
                        unselectedIconColor = SamsungCalendarColors.muted,
                        unselectedTextColor = SamsungCalendarColors.muted
                    )
                )
                NavigationBarItem(
                    selected = selectedTab == ExpressTab.INSPECTION,
                    onClick = { selectedTab = ExpressTab.INSPECTION },
                    icon = {
                        Icon(
                            imageVector = Icons.Default.Build,
                            contentDescription = "Inspection"
                        )
                    },
                    label = { Text("Inspection") },
                    colors = NavigationBarItemDefaults.colors(
                        selectedIconColor = SamsungCalendarColors.green,
                        selectedTextColor = SamsungCalendarColors.green,
                        indicatorColor = SamsungCalendarColors.quickAddField,
                        unselectedIconColor = SamsungCalendarColors.muted,
                        unselectedTextColor = SamsungCalendarColors.muted
                    )
                )
            }
        }
    ) { padding ->
        when (selectedTab) {
            ExpressTab.APPOINTMENTS -> AppointmentsScreen(
                store = appointmentStore,
                inspectionStore = inspectionStore,
                onInspectionLinked = { inspectionId ->
                    activeInspectionId = inspectionId
                },
                onOpenInspection = { inspectionId ->
                    activeInspectionId = inspectionId
                    selectedTab = ExpressTab.INSPECTION
                },
                showRestoreBanner = showRestoreBanner,
                onRestoreFromDownloads = onRestoreFromDownloads,
                onImportBackupFile = onImportBackupFile,
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding)
            )
            ExpressTab.INSPECTION -> InspectionScreen(
                inspectionStore = inspectionStore,
                appointmentStore = appointmentStore,
                activeInspectionId = activeInspectionId,
                onShareReport = onShareReport,
                onShareError = onShareError,
                onInspectionSaved = onNotify,
                onActiveInspectionChange = { activeInspectionId = it },
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding)
            )
        }
    }
}

@Composable
fun RestoreBackupBanner(
    onRestoreFromDownloads: () -> Unit,
    onImportBackupFile: () -> Unit,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier
            .fillMaxWidth()
            .background(SamsungCalendarColors.quickAddField)
            .padding(horizontal = 16.dp, vertical = 12.dp)
    ) {
        Text(
            text = "Restore saved jobs",
            color = SamsungCalendarColors.onBackground,
            fontWeight = FontWeight.SemiBold
        )
        Text(
            text = "Tap to load your backup from Downloads, or choose a backup file.",
            color = SamsungCalendarColors.muted,
            modifier = Modifier.padding(top = 4.dp, bottom = 8.dp)
        )
        Text(
            text = "Restore from Downloads",
            color = SamsungCalendarColors.green,
            fontWeight = FontWeight.SemiBold,
            modifier = Modifier
                .clickable(onClick = onRestoreFromDownloads)
                .padding(vertical = 4.dp)
        )
        Text(
            text = "Choose backup file…",
            color = SamsungCalendarColors.green,
            fontWeight = FontWeight.SemiBold,
            modifier = Modifier
                .clickable(onClick = onImportBackupFile)
                .padding(vertical = 4.dp)
        )
    }
}
