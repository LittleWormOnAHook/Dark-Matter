package com.expressmobileservice.inspection.ui

import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.expressmobileservice.inspection.VehicleCatalogRepository
import com.expressmobileservice.inspection.VehicleCategory
import com.expressmobileservice.inspection.VehicleEngineCatalog
import com.expressmobileservice.inspection.ui.theme.SamsungCalendarColors

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun VehicleDropdownFields(
    vehicleCategory: VehicleCategory,
    onCategoryChange: (VehicleCategory) -> Unit,
    vehicleYear: Int?,
    onYearChange: (Int?) -> Unit,
    vehicleMake: String,
    onMakeChange: (String) -> Unit,
    vehicleModel: String,
    onModelChange: (String) -> Unit,
    engineSize: String,
    onEngineSizeChange: (String) -> Unit,
    mileage: String,
    onMileageChange: (String) -> Unit,
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    val catalog = remember { VehicleCatalogRepository(context) }
    val copyHandler = rememberCopyHandler()
    var makes by remember { mutableStateOf<List<String>>(emptyList()) }
    var models by remember { mutableStateOf<List<String>>(emptyList()) }
    var engineSizes by remember { mutableStateOf<List<String>>(emptyList()) }
    var loadingMakes by remember { mutableStateOf(false) }
    var loadingModels by remember { mutableStateOf(false) }

    val years = remember { VehicleCategory.yearRange.toList().reversed() }

    LaunchedEffect(vehicleCategory) {
        loadingMakes = true
        makes = catalog.getMakes(vehicleCategory)
        loadingMakes = false
        if (vehicleMake.isNotBlank() && vehicleMake !in makes) {
            makes = (makes + vehicleMake).sorted()
        }
    }

    LaunchedEffect(vehicleCategory, vehicleMake, vehicleYear) {
        if (vehicleMake.isBlank()) {
            models = emptyList()
            return@LaunchedEffect
        }
        loadingModels = true
        models = catalog.getModels(vehicleCategory, vehicleMake, vehicleYear)
        loadingModels = false
        if (vehicleModel.isNotBlank() && vehicleModel !in models) {
            models = (models + vehicleModel).sorted()
        }
    }

    LaunchedEffect(vehicleCategory, vehicleMake, vehicleModel) {
        engineSizes = if (vehicleMake.isBlank()) {
            emptyList()
        } else {
            VehicleEngineCatalog.getOptions(vehicleCategory, vehicleMake, vehicleModel)
        }
        if (engineSize.isNotBlank() && engineSize !in engineSizes) {
            engineSizes = (engineSizes + engineSize).sorted()
        }
    }

    Column(modifier = modifier, verticalArrangement = Arrangement.spacedBy(8.dp)) {
        Text(
            text = "Vehicle (US 1970–2026 · NHTSA + PWC data)",
            color = SamsungCalendarColors.muted,
            modifier = Modifier.padding(bottom = 4.dp)
        )

        SearchableVehicleDropdown(
            label = "Type",
            value = vehicleCategory.label,
            options = VehicleCategory.entries.map { it.label },
            onSelect = { label ->
                VehicleCategory.entries.firstOrNull { it.label == label }?.let { onCategoryChange(it) }
            }
        )

        SearchableVehicleDropdown(
            label = "Year",
            value = vehicleYear?.toString().orEmpty(),
            options = years.map { it.toString() },
            onSelect = { onYearChange(it.toIntOrNull()) },
            searchHint = "Search year…"
        )

        if (loadingMakes) {
            CircularProgressIndicator(modifier = Modifier.padding(8.dp))
        } else {
            SearchableVehicleDropdown(
                label = "Make",
                value = vehicleMake,
                options = makes,
                onSelect = {
                    onMakeChange(it)
                    onModelChange("")
                    onEngineSizeChange("")
                },
                enabled = makes.isNotEmpty(),
                searchHint = "Search make…"
            )
        }

        if (loadingModels) {
            CircularProgressIndicator(modifier = Modifier.padding(8.dp))
        } else {
            SearchableVehicleDropdown(
                label = "Model",
                value = vehicleModel,
                options = models,
                onSelect = {
                    onModelChange(it)
                    onEngineSizeChange("")
                },
                enabled = vehicleMake.isNotBlank() && models.isNotEmpty(),
                searchHint = "Search model…"
            )
        }

        SearchableVehicleDropdown(
            label = "Engine size",
            value = engineSize,
            options = engineSizes,
            onSelect = { onEngineSizeChange(it) },
            enabled = vehicleMake.isNotBlank() && vehicleModel.isNotBlank() && engineSizes.isNotEmpty(),
            searchHint = "Search engine…"
        )

        OutlinedTextField(
            value = mileage,
            onValueChange = onMileageChange,
            label = { Text("Mileage / hours") },
            modifier = Modifier
                .fillMaxWidth()
                .pointerInput(mileage) {
                    detectTapGestures(
                        onLongPress = {
                            if (mileage.isNotBlank()) copyHandler(mileage, "Copied")
                        }
                    )
                },
            singleLine = true,
            colors = ExposedDropdownMenuDefaults.outlinedTextFieldColors(
                focusedBorderColor = SamsungCalendarColors.green,
                cursorColor = SamsungCalendarColors.green
            )
        )
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun SearchableVehicleDropdown(
    label: String,
    value: String,
    options: List<String>,
    onSelect: (String) -> Unit,
    enabled: Boolean = true,
    searchHint: String = "Search…"
) {
    var showSheet by remember { mutableStateOf(false) }
    var query by remember { mutableStateOf("") }
    val sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true)
    val copyHandler = rememberCopyHandler()

    val filtered = remember(options, query) {
        if (query.isBlank()) options
        else options.filter { it.contains(query, ignoreCase = true) }
    }

    Box(modifier = Modifier.fillMaxWidth()) {
        OutlinedTextField(
            value = value,
            onValueChange = {},
            readOnly = true,
            enabled = enabled,
            label = { Text(label) },
            trailingIcon = {
                Icon(
                    imageVector = Icons.Default.Search,
                    contentDescription = "Search $label"
                )
            },
            modifier = Modifier
                .fillMaxWidth()
                .pointerInput(value) {
                    detectTapGestures(
                        onLongPress = {
                            if (value.isNotBlank()) copyHandler(value, "Copied")
                        }
                    )
                },
            colors = ExposedDropdownMenuDefaults.outlinedTextFieldColors(
                focusedBorderColor = SamsungCalendarColors.green,
                cursorColor = SamsungCalendarColors.green,
                disabledTextColor = MaterialTheme.colorScheme.onSurface,
                disabledLabelColor = SamsungCalendarColors.muted
            )
        )
        if (enabled) {
            Box(
                modifier = Modifier
                    .matchParentSize()
                    .clickable(onClick = ExpressUiSounds.withImpact { showSheet = true })
            )
        }
    }

    if (showSheet) {
        ModalBottomSheet(
            onDismissRequest = {
                showSheet = false
                query = ""
            },
            sheetState = sheetState,
            containerColor = SamsungCalendarColors.surface
        ) {
            Column(modifier = Modifier.fillMaxWidth()) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp, vertical = 4.dp),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Text(text = label, fontWeight = FontWeight.SemiBold)
                    IconButton(onClick = ExpressUiSounds.withImpact {
                        showSheet = false
                        query = ""
                    }) {
                        Icon(Icons.Default.Close, contentDescription = "Close")
                    }
                }
                OutlinedTextField(
                    value = query,
                    onValueChange = { updated ->
                        ExpressUiSounds.onTypingValueChange(query, updated) { query = it }
                    },
                    placeholder = { Text(searchHint) },
                    leadingIcon = {
                        Icon(Icons.Default.Search, contentDescription = null)
                    },
                    singleLine = true,
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp, vertical = 8.dp),
                    colors = ExposedDropdownMenuDefaults.outlinedTextFieldColors(
                        focusedBorderColor = SamsungCalendarColors.green,
                        cursorColor = SamsungCalendarColors.green
                    )
                )
                Text(
                    text = "${filtered.size} of ${options.size}",
                    color = SamsungCalendarColors.muted,
                    modifier = Modifier.padding(horizontal = 16.dp, vertical = 4.dp)
                )
                HorizontalDivider(color = SamsungCalendarColors.divider)
                LazyColumn(
                    modifier = Modifier
                        .fillMaxWidth()
                        .heightIn(max = 420.dp)
                ) {
                    items(filtered, key = { it }) { option ->
                        Text(
                            text = option,
                            modifier = Modifier
                                .fillMaxWidth()
                                .clickable(
                                    onClick = ExpressUiSounds.withImpact {
                                        onSelect(option)
                                        showSheet = false
                                        query = ""
                                    }
                                )
                                .padding(horizontal = 16.dp, vertical = 14.dp)
                        )
                    }
                    if (filtered.isEmpty()) {
                        item {
                            Text(
                                text = "No matches",
                                color = SamsungCalendarColors.muted,
                                modifier = Modifier.padding(16.dp)
                            )
                        }
                    }
                }
            }
        }
    }
}
