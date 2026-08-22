package com.expressmobileservice.inspection.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
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
import androidx.compose.material.icons.filled.PictureAsPdf
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Image
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
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
import androidx.compose.ui.text.input.KeyboardType
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
import com.expressmobileservice.inspection.defaultInspectionSections
import com.expressmobileservice.inspection.ui.theme.InspectionColors

enum class ReportShareType { PDF, IMAGE }

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun InspectionScreen(
    onShareReport: (InspectionFormState, ReportShareType, (Boolean) -> Unit) -> Unit
) {
    var customerName by rememberSaveable { mutableStateOf("") }
    var customerPhone by rememberSaveable { mutableStateOf("") }
    var vehicle by rememberSaveable { mutableStateOf("") }
    var mileage by rememberSaveable { mutableStateOf("") }
    var sections by remember { mutableStateOf(defaultInspectionSections()) }
    var isGenerating by remember { mutableStateOf(false) }

    fun currentState() = InspectionFormState(
        customerInfo = CustomerInfo(
            customerName = customerName,
            customerPhone = customerPhone,
            vehicle = vehicle,
            mileage = mileage
        ),
        sections = sections
    )

    fun share(type: ReportShareType) {
        isGenerating = true
        onShareReport(currentState(), type) { isGenerating = false }
    }

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
                            Text(text = COMPANY_PHONE, style = MaterialTheme.typography.bodySmall)
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
                if (isGenerating) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.Center,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        CircularProgressIndicator(modifier = Modifier.size(20.dp), strokeWidth = 2.dp)
                        Spacer(modifier = Modifier.width(12.dp))
                        Text("Creating report…")
                    }
                    Spacer(modifier = Modifier.height(8.dp))
                }
                Button(
                    onClick = { share(ReportShareType.PDF) },
                    enabled = !isGenerating,
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Icon(Icons.Default.PictureAsPdf, contentDescription = null)
                    Spacer(modifier = Modifier.width(8.dp))
                    Text("Send PDF Report")
                }
                Spacer(modifier = Modifier.height(8.dp))
                Button(
                    onClick = { share(ReportShareType.IMAGE) },
                    enabled = !isGenerating,
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Icon(Icons.Default.Image, contentDescription = null)
                    Spacer(modifier = Modifier.width(8.dp))
                    Text("Send Image Report")
                }
                Spacer(modifier = Modifier.height(8.dp))
                OutlinedButton(
                    onClick = {
                        customerName = ""
                        customerPhone = ""
                        vehicle = ""
                        mileage = ""
                        sections = defaultInspectionSections()
                    },
                    enabled = !isGenerating,
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
            verticalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            Card(
                modifier = Modifier.fillMaxWidth(),
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.primaryContainer)
            ) {
                Text(
                    text = "Tap Good, Bad, or Replace. Add notes if needed.",
                    modifier = Modifier.padding(14.dp),
                    style = MaterialTheme.typography.bodyMedium
                )
            }

            Card(modifier = Modifier.fillMaxWidth()) {
                Column(
                    modifier = Modifier.padding(14.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Text("Customer Info", fontWeight = FontWeight.SemiBold)
                    FormField("Customer Name", customerName) { customerName = it }
                    FormField(
                        "Phone",
                        customerPhone,
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Phone)
                    ) { customerPhone = it }
                    FormField("Vehicle (Year / Make / Model)", vehicle) { vehicle = it }
                    FormField("Mileage", mileage) { mileage = it }
                }
            }

            sections.forEachIndexed { sectionIndex, section ->
                SectionBlock(
                    section = section,
                    onItemStatusChange = { itemId, status ->
                        sections = sections.updateItemStatus(sectionIndex, itemId, status)
                    },
                    onItemNotesChange = { itemId, notes ->
                        sections = sections.updateItemNotes(sectionIndex, itemId, notes)
                    }
                )
            }

            Spacer(modifier = Modifier.height(100.dp))
        }
    }
}

@Composable
private fun FormField(
    label: String,
    value: String,
    keyboardOptions: KeyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.Words),
    onValueChange: (String) -> Unit
) {
    OutlinedTextField(
        value = value,
        onValueChange = onValueChange,
        label = { Text(label) },
        modifier = Modifier.fillMaxWidth(),
        singleLine = true,
        keyboardOptions = keyboardOptions
    )
}

@Composable
private fun SectionBlock(
    section: InspectionSection,
    onItemStatusChange: (String, InspectionStatus) -> Unit,
    onItemNotesChange: (String, String) -> Unit
) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(12.dp)) {
            Text(
                text = section.title,
                fontWeight = FontWeight.Bold,
                color = MaterialTheme.colorScheme.primary,
                modifier = Modifier.padding(bottom = 6.dp)
            )
            section.items.forEachIndexed { index, item ->
                CompactItemRow(
                    item = item,
                    onStatusChange = { onItemStatusChange(item.id, it) },
                    onNotesChange = { onItemNotesChange(item.id, it) }
                )
                if (index < section.items.lastIndex) {
                    HorizontalDivider(modifier = Modifier.padding(vertical = 6.dp))
                }
            }
        }
    }
}

@Composable
private fun CompactItemRow(
    item: InspectionItem,
    onStatusChange: (InspectionStatus) -> Unit,
    onNotesChange: (String) -> Unit
) {
    Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
        Text(text = item.label, fontWeight = FontWeight.Medium, fontSize = 15.sp)
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(6.dp)
        ) {
            StatusChip("Good", item.status == InspectionStatus.GOOD, InspectionColors.good, InspectionColors.goodContainer) {
                onStatusChange(if (item.status == InspectionStatus.GOOD) InspectionStatus.NONE else InspectionStatus.GOOD)
            }
            StatusChip("Bad", item.status == InspectionStatus.BAD, InspectionColors.bad, InspectionColors.badContainer) {
                onStatusChange(if (item.status == InspectionStatus.BAD) InspectionStatus.NONE else InspectionStatus.BAD)
            }
            StatusChip("Replace", item.status == InspectionStatus.REPLACE, InspectionColors.replace, InspectionColors.replaceContainer) {
                onStatusChange(if (item.status == InspectionStatus.REPLACE) InspectionStatus.NONE else InspectionStatus.REPLACE)
            }
        }
        OutlinedTextField(
            value = item.notes,
            onValueChange = onNotesChange,
            label = { Text("Notes") },
            placeholder = { Text("Optional") },
            modifier = Modifier.fillMaxWidth(),
            singleLine = true
        )
    }
}

@Composable
private fun RowScope.StatusChip(
    label: String,
    selected: Boolean,
    selectedColor: Color,
    selectedContainer: Color,
    onClick: () -> Unit
) {
    val background = if (selected) selectedContainer else Color.Transparent
    val borderColor = if (selected) selectedColor else MaterialTheme.colorScheme.outline
    val textColor = if (selected) selectedColor else MaterialTheme.colorScheme.onSurface

    Box(
        modifier = Modifier
            .weight(1f)
            .clip(RoundedCornerShape(8.dp))
            .background(background)
            .border(1.5.dp, borderColor, RoundedCornerShape(8.dp))
            .clickable(onClick = onClick)
            .padding(vertical = 8.dp),
        contentAlignment = Alignment.Center
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            if (selected) {
                Icon(Icons.Default.Check, contentDescription = null, tint = selectedColor, modifier = Modifier.size(14.dp))
                Spacer(modifier = Modifier.width(3.dp))
            }
            Text(
                text = label,
                color = textColor,
                fontWeight = if (selected) FontWeight.Bold else FontWeight.Normal,
                fontSize = 12.sp,
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
    else section.copy(items = section.items.map { if (it.id == itemId) it.copy(status = status) else it })
}

private fun List<InspectionSection>.updateItemNotes(
    sectionIndex: Int,
    itemId: String,
    notes: String
): List<InspectionSection> = mapIndexed { index, section ->
    if (index != sectionIndex) section
    else section.copy(items = section.items.map { if (it.id == itemId) it.copy(notes = notes) else it })
}
