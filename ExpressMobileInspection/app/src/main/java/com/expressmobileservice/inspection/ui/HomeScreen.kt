package com.expressmobileservice.inspection.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.Engineering
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
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import com.expressmobileservice.inspection.AppointmentStore
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.size
import com.expressmobileservice.inspection.InspectionFormState
import com.expressmobileservice.inspection.InspectionStore
import com.expressmobileservice.inspection.ui.theme.SamsungCalendarColors
import com.expressmobileservice.inspection.ui.playButtonClick

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
    onNotify: (String) -> Unit = {}
) {
    var selectedTab by remember { mutableStateOf(ExpressTab.APPOINTMENTS) }
    var activeInspectionId by remember {
        mutableStateOf(inspectionStore.mostRecent()?.id)
    }

    Scaffold(
        modifier = Modifier.fillMaxSize(),
        bottomBar = {
            NavigationBar(
                containerColor = SamsungCalendarColors.surface,
                contentColor = SamsungCalendarColors.eggWhite
            ) {
                NavigationBarItem(
                    selected = selectedTab == ExpressTab.APPOINTMENTS,
                    onClick = {
                        playButtonClick()
                        selectedTab = ExpressTab.APPOINTMENTS
                    },
                    icon = {
                        NavIcon(
                            selected = selectedTab == ExpressTab.APPOINTMENTS,
                            imageVector = Icons.Default.CalendarMonth,
                            contentDescription = "Appointments"
                        )
                    },
                    label = { Text("Appointments") },
                    colors = navItemColors()
                )
                NavigationBarItem(
                    selected = selectedTab == ExpressTab.INSPECTION,
                    onClick = {
                        playButtonClick()
                        selectedTab = ExpressTab.INSPECTION
                    },
                    icon = {
                        NavIcon(
                            selected = selectedTab == ExpressTab.INSPECTION,
                            imageVector = Icons.Default.Engineering,
                            contentDescription = "Inspection"
                        )
                    },
                    label = { Text("Inspection") },
                    colors = navItemColors()
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
private fun NavIcon(
    selected: Boolean,
    imageVector: androidx.compose.ui.graphics.vector.ImageVector,
    contentDescription: String
) {
    if (selected) {
        Box(
            modifier = Modifier
                .size(40.dp)
                .clip(RoundedCornerShape(20.dp))
                .background(SamsungCalendarColors.navSelectedPill),
            contentAlignment = androidx.compose.ui.Alignment.Center
        ) {
            Icon(
                imageVector = imageVector,
                contentDescription = contentDescription,
                tint = SamsungCalendarColors.metallicGold
            )
        }
    } else {
        Icon(
            imageVector = imageVector,
            contentDescription = contentDescription,
            tint = SamsungCalendarColors.muted
        )
    }
}

@Composable
private fun navItemColors() = NavigationBarItemDefaults.colors(
    selectedIconColor = SamsungCalendarColors.metallicGold,
    selectedTextColor = SamsungCalendarColors.eggWhite,
    unselectedIconColor = SamsungCalendarColors.muted,
    unselectedTextColor = SamsungCalendarColors.muted,
    indicatorColor = Color.Transparent
)
