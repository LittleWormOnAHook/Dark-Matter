package com.expressmobileservice.inspection.ui

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import com.expressmobileservice.inspection.EngineSizeOptions
import com.expressmobileservice.inspection.VehicleCatalogRepository
import com.expressmobileservice.inspection.VehicleCategory
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
    var makes by remember { mutableStateOf<List<String>>(emptyList()) }
    var models by remember { mutableStateOf<List<String>>(emptyList()) }
    var loadingMakes by remember { mutableStateOf(false) }
    var loadingModels by remember { mutableStateOf(false) }

    val years = remember { VehicleCategory.yearRange.toList().reversed() }
    val engineSizes = remember(vehicleCategory) { EngineSizeOptions.forCategory(vehicleCategory) }

    LaunchedEffect(vehicleCategory) {
        loadingMakes = true
        makes = catalog.getMakes(vehicleCategory)
        loadingMakes = false
        if (vehicleMake.isNotBlank() && vehicleMake !in makes) {
            makes = (makes + vehicleMake).sorted()
        }
    }

    LaunchedEffect(vehicleCategory, vehicleMake, vehicleYear) {
        if (vehicleMake.isBlank() || vehicleYear == null) {
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

    Column(modifier = modifier, verticalArrangement = androidx.compose.foundation.layout.Arrangement.spacedBy(8.dp)) {
        Text(
            text = "Vehicle (US 1970–2026 · NHTSA + PWC data)",
            color = SamsungCalendarColors.muted,
            modifier = Modifier.padding(bottom = 4.dp)
        )

        VehicleDropdown(
            label = "Type",
            value = vehicleCategory.label,
            options = VehicleCategory.entries.map { it.label },
            onSelect = { label ->
                VehicleCategory.entries.firstOrNull { it.label == label }?.let { onCategoryChange(it) }
            }
        )

        VehicleDropdown(
            label = "Year",
            value = vehicleYear?.toString().orEmpty(),
            options = years.map { it.toString() },
            onSelect = { onYearChange(it.toIntOrNull()) }
        )

        if (loadingMakes) {
            CircularProgressIndicator(modifier = Modifier.padding(8.dp))
        } else {
            VehicleDropdown(
                label = "Make",
                value = vehicleMake,
                options = makes,
                onSelect = {
                    onMakeChange(it)
                    onModelChange("")
                },
                enabled = makes.isNotEmpty()
            )
        }

        if (loadingModels) {
            CircularProgressIndicator(modifier = Modifier.padding(8.dp))
        } else {
            VehicleDropdown(
                label = "Model",
                value = vehicleModel,
                options = models,
                onSelect = { onModelChange(it) },
                enabled = vehicleMake.isNotBlank() && vehicleYear != null && models.isNotEmpty()
            )
        }

        VehicleDropdown(
            label = "Engine size",
            value = engineSize,
            options = engineSizes,
            onSelect = { onEngineSizeChange(it) }
        )

        OutlinedTextField(
            value = mileage,
            onValueChange = onMileageChange,
            label = { Text("Mileage / hours") },
            modifier = Modifier.fillMaxWidth(),
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
private fun VehicleDropdown(
    label: String,
    value: String,
    options: List<String>,
    onSelect: (String) -> Unit,
    enabled: Boolean = true
) {
    var expanded by remember { mutableStateOf(false) }
    ExposedDropdownMenuBox(
        expanded = expanded,
        onExpandedChange = { if (enabled) expanded = it }
    ) {
        OutlinedTextField(
            value = value,
            onValueChange = {},
            readOnly = true,
            enabled = enabled,
            label = { Text(label) },
            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded) },
            modifier = Modifier
                .menuAnchor()
                .fillMaxWidth(),
            colors = ExposedDropdownMenuDefaults.outlinedTextFieldColors(
                focusedBorderColor = SamsungCalendarColors.green,
                cursorColor = SamsungCalendarColors.green
            )
        )
        ExposedDropdownMenu(
            expanded = expanded,
            onDismissRequest = { expanded = false }
        ) {
            options.forEach { option ->
                DropdownMenuItem(
                    text = { Text(option) },
                    onClick = {
                        onSelect(option)
                        expanded = false
                    }
                )
            }
        }
    }
}
