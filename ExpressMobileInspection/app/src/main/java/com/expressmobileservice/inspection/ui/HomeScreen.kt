package com.expressmobileservice.inspection.ui

import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Build
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import com.expressmobileservice.inspection.AppointmentStore
import com.expressmobileservice.inspection.InspectionFormState

enum class ExpressTab {
    APPOINTMENTS,
    INSPECTION
}

@Composable
fun HomeScreen(
    appointmentStore: AppointmentStore,
    onShareReport: (InspectionFormState, ReportShareType, (Boolean) -> Unit) -> Unit,
    onShareError: (String) -> Unit
) {
    var selectedTab by remember { mutableStateOf(ExpressTab.APPOINTMENTS) }

    Scaffold(
        modifier = Modifier.fillMaxSize(),
        bottomBar = {
            NavigationBar(
                containerColor = MaterialTheme.colorScheme.surface,
                contentColor = MaterialTheme.colorScheme.onSurface
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
                    label = { Text("Appointments") }
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
                    label = { Text("Inspection") }
                )
            }
        }
    ) { padding ->
        when (selectedTab) {
            ExpressTab.APPOINTMENTS -> AppointmentsScreen(
                store = appointmentStore,
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding)
            )
            ExpressTab.INSPECTION -> InspectionScreen(
                onShareReport = onShareReport,
                onShareError = onShareError,
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding)
            )
        }
    }
}
