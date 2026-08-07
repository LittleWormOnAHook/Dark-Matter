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
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Phone
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.automirrored.filled.Send
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.expressmobileservice.inspection.COMPANY_NAME
import com.expressmobileservice.inspection.COMPANY_PHONE
import com.expressmobileservice.inspection.CustomerInfo
import com.expressmobileservice.inspection.InspectionFormState
import com.expressmobileservice.inspection.InspectionItem
import com.expressmobileservice.inspection.InspectionSection
import com.expressmobileservice.inspection.InspectionStatus
import com.expressmobileservice.inspection.ReportFormatter
import com.expressmobileservice.inspection.defaultInspectionSections
import com.expressmobileservice.inspection.ui.theme.InspectionColors

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun InspectionScreen(onShareReport: (String) -> Unit) {
    var customerName by rememberSaveable { mutableStateOf("") }
    var customerContact by rememberSaveable { mutableStateOf("") }
    var vehicleYearMakeModel by rememberSaveable { mutableStateOf("") }
    var vin by rememberSaveable { mutableStateOf("") }
    var mileage by rememberSaveable { mutableStateOf("") }
    var licensePlate by rememberSaveable { mutableStateOf("") }
    var technicianName by rememberSaveable { mutableStateOf("") }
    var sections by remember { mutableStateOf(defaultInspectionSections()) }

    fun currentState() = InspectionFormState(
        customerInfo = CustomerInfo(
            customerName = customerName,
            customerContact = customerContact,
            vehicleYearMakeModel = vehicleYearMakeModel,
            vin = vin,
            mileage = mileage,
            licensePlate = licensePlate,
            technicianName = technicianName
        ),
        sections = sections
    )

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text(
                            text = COMPANY_NAME,
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold
                        )
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Icon(
                                imageVector = Icons.Default.Phone,
                                contentDescription = null,
                                modifier = Modifier.size(14.dp)
                            )
                            Spacer(modifier = Modifier.width(4.dp))
                            Text(
                                text = COMPANY_PHONE,
                                style = MaterialTheme.typography.bodySmall
                            )
                        }
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = MaterialTheme.colorScheme.primary,
                    titleContentColor = Color.White
                )
            )
        },
        bottomBar = {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .background(MaterialTheme.colorScheme.surface)
                    .padding(12.dp)
            ) {
                Button(
                    onClick = { onShareReport(ReportFormatter.formatReport(currentState())) },
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Icon(Icons.AutoMirrored.Filled.Send, contentDescription = null)
                    Spacer(modifier = Modifier.width(8.dp))
                    Text("Send Report (Text or Email)")
                }
                Spacer(modifier = Modifier.height(8.dp))
                OutlinedButton(
                    onClick = {
                        customerName = ""
                        customerContact = ""
                        vehicleYearMakeModel = ""
                        vin = ""
                        mileage = ""
                        licensePlate = ""
                        technicianName = ""
                        sections = defaultInspectionSections()
                    },
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Icon(Icons.Default.Refresh, contentDescription = null)
                    Spacer(modifier = Modifier.width(8.dp))
                    Text("Clear Form")
                }
            }
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 12.dp, vertical = 8.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            HeaderCard()
            CustomerInfoCard(
                customerName = customerName,
                onCustomerNameChange = { customerName = it },
                customerContact = customerContact,
                onCustomerContactChange = { customerContact = it },
                vehicleYearMakeModel = vehicleYearMakeModel,
                onVehicleChange = { vehicleYearMakeModel = it },
                vin = vin,
                onVinChange = { vin = it },
                mileage = mileage,
                onMileageChange = { mileage = it },
                licensePlate = licensePlate,
                onLicensePlateChange = { licensePlate = it },
                technicianName = technicianName,
                onTechnicianChange = { technicianName = it }
            )

            sections.forEachIndexed { sectionIndex, section ->
                SectionCard(
                    section = section,
                    onItemStatusChange = { itemId, status ->
                        sections = sections.updateItemStatus(sectionIndex, itemId, status)
                    },
                    onItemNotesChange = { itemId, notes ->
                        sections = sections.updateItemNotes(sectionIndex, itemId, notes)
                    }
                )
            }

            Spacer(modifier = Modifier.height(80.dp))
        }
    }
}

@Composable
private fun HeaderCard() {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.primaryContainer)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(
                text = "Multi-Point Vehicle Inspection",
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold
            )
            Spacer(modifier = Modifier.height(4.dp))
            Text(
                text = "Tap Good, Bad, or Replace for each item. Add notes with the keyboard.",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.75f)
            )
        }
    }
}

@Composable
private fun CustomerInfoCard(
    customerName: String,
    onCustomerNameChange: (String) -> Unit,
    customerContact: String,
    onCustomerContactChange: (String) -> Unit,
    vehicleYearMakeModel: String,
    onVehicleChange: (String) -> Unit,
    vin: String,
    onVinChange: (String) -> Unit,
    mileage: String,
    onMileageChange: (String) -> Unit,
    licensePlate: String,
    onLicensePlateChange: (String) -> Unit,
    technicianName: String,
    onTechnicianChange: (String) -> Unit
) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Text(
                text = "Customer & Vehicle Info",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.SemiBold
            )
            FormField("Customer Name", customerName, onCustomerNameChange)
            FormField("Phone / Email", customerContact, onCustomerContactChange)
            FormField("Year / Make / Model", vehicleYearMakeModel, onVehicleChange)
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                FormField(
                    label = "VIN",
                    value = vin,
                    onValueChange = onVinChange,
                    modifier = Modifier.weight(1f),
                    capitalization = KeyboardCapitalization.Characters
                )
                FormField(
                    label = "Mileage",
                    value = mileage,
                    onValueChange = onMileageChange,
                    modifier = Modifier.weight(0.5f),
                    keyboardOptions = KeyboardOptions.Default
                )
            }
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                FormField(
                    label = "License Plate",
                    value = licensePlate,
                    onValueChange = onLicensePlateChange,
                    modifier = Modifier.weight(1f),
                    capitalization = KeyboardCapitalization.Characters
                )
                FormField(
                    label = "Technician",
                    value = technicianName,
                    onValueChange = onTechnicianChange,
                    modifier = Modifier.weight(1f)
                )
            }
        }
    }
}

@Composable
private fun FormField(
    label: String,
    value: String,
    onValueChange: (String) -> Unit,
    modifier: Modifier = Modifier,
    capitalization: KeyboardCapitalization = KeyboardCapitalization.Words,
    keyboardOptions: KeyboardOptions = KeyboardOptions(capitalization = capitalization)
) {
    OutlinedTextField(
        value = value,
        onValueChange = onValueChange,
        label = { Text(label) },
        modifier = modifier.fillMaxWidth(),
        singleLine = true,
        keyboardOptions = keyboardOptions
    )
}

@Composable
private fun SectionCard(
    section: InspectionSection,
    onItemStatusChange: (String, InspectionStatus) -> Unit,
    onItemNotesChange: (String, String) -> Unit
) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(12.dp)) {
            Text(
                text = section.title,
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold,
                color = MaterialTheme.colorScheme.primary
            )
            Spacer(modifier = Modifier.height(8.dp))
            section.items.forEachIndexed { index, item ->
                InspectionItemRow(
                    item = item,
                    onStatusChange = { onItemStatusChange(item.id, it) },
                    onNotesChange = { onItemNotesChange(item.id, it) }
                )
                if (index < section.items.lastIndex) {
                    HorizontalDivider(modifier = Modifier.padding(vertical = 8.dp))
                }
            }
        }
    }
}

@Composable
private fun InspectionItemRow(
    item: InspectionItem,
    onStatusChange: (InspectionStatus) -> Unit,
    onNotesChange: (String) -> Unit
) {
    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
        Text(
            text = item.label,
            style = MaterialTheme.typography.bodyLarge,
            fontWeight = FontWeight.Medium
        )
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            StatusChip(
                label = "Good",
                selected = item.status == InspectionStatus.GOOD,
                selectedColor = InspectionColors.good,
                selectedContainer = InspectionColors.goodContainer,
                onClick = {
                    onStatusChange(
                        if (item.status == InspectionStatus.GOOD) InspectionStatus.NONE
                        else InspectionStatus.GOOD
                    )
                },
                modifier = Modifier.weight(1f)
            )
            StatusChip(
                label = "Bad",
                selected = item.status == InspectionStatus.BAD,
                selectedColor = InspectionColors.bad,
                selectedContainer = InspectionColors.badContainer,
                onClick = {
                    onStatusChange(
                        if (item.status == InspectionStatus.BAD) InspectionStatus.NONE
                        else InspectionStatus.BAD
                    )
                },
                modifier = Modifier.weight(1f)
            )
            StatusChip(
                label = "Replace",
                selected = item.status == InspectionStatus.REPLACE,
                selectedColor = InspectionColors.replace,
                selectedContainer = InspectionColors.replaceContainer,
                onClick = {
                    onStatusChange(
                        if (item.status == InspectionStatus.REPLACE) InspectionStatus.NONE
                        else InspectionStatus.REPLACE
                    )
                },
                modifier = Modifier.weight(1f)
            )
        }
        OutlinedTextField(
            value = item.notes,
            onValueChange = onNotesChange,
            label = { Text("Notes") },
            placeholder = { Text("Tap to add notation…") },
            modifier = Modifier.fillMaxWidth(),
            minLines = 1,
            maxLines = 3
        )
    }
}

@Composable
private fun StatusChip(
    label: String,
    selected: Boolean,
    selectedColor: Color,
    selectedContainer: Color,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    val background = if (selected) selectedContainer else Color.Transparent
    val borderColor = if (selected) selectedColor else MaterialTheme.colorScheme.outline
    val textColor = if (selected) selectedColor else MaterialTheme.colorScheme.onSurface

    Box(
        modifier = modifier
            .clip(RoundedCornerShape(8.dp))
            .background(background)
            .border(1.5.dp, borderColor, RoundedCornerShape(8.dp))
            .clickable(onClick = onClick)
            .padding(vertical = 10.dp, horizontal = 4.dp),
        contentAlignment = Alignment.Center
    ) {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.Center
        ) {
            if (selected) {
                Icon(
                    imageVector = Icons.Default.Check,
                    contentDescription = null,
                    tint = selectedColor,
                    modifier = Modifier.size(16.dp)
                )
                Spacer(modifier = Modifier.width(4.dp))
            }
            Text(
                text = label,
                color = textColor,
                fontWeight = if (selected) FontWeight.Bold else FontWeight.Normal,
                fontSize = 13.sp,
                textAlign = TextAlign.Center
            )
        }
    }
}

private fun List<InspectionSection>.updateItemStatus(
    sectionIndex: Int,
    itemId: String,
    status: InspectionStatus
): List<InspectionSection> = mapIndexed { index, section ->
    if (index != sectionIndex) section
    else section.copy(
        items = section.items.map { item ->
            if (item.id == itemId) item.copy(status = status) else item
        }
    )
}

private fun List<InspectionSection>.updateItemNotes(
    sectionIndex: Int,
    itemId: String,
    notes: String
): List<InspectionSection> = mapIndexed { index, section ->
    if (index != sectionIndex) section
    else section.copy(
        items = section.items.map { item ->
            if (item.id == itemId) item.copy(notes = notes) else item
        }
    )
}
