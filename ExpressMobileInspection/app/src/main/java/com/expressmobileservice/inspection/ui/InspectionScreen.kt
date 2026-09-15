package com.expressmobileservice.inspection.ui

import android.content.Intent
import android.net.Uri
import androidx.compose.foundation.Image
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
import androidx.compose.material.icons.filled.Star
import androidx.compose.material.icons.filled.Image
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.List
import androidx.compose.material.icons.filled.Save
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.IconButton
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.rememberUpdatedState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.painter.Painter
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.expressmobileservice.inspection.R
import com.expressmobileservice.inspection.COMPANY_GOOGLE_REVIEW_URL
import com.expressmobileservice.inspection.COMPANY_NAME
import com.expressmobileservice.inspection.COMPANY_PHONE_DISPLAY
import com.expressmobileservice.inspection.COMPANY_PHONE_URI
import com.expressmobileservice.inspection.COMPANY_WEBSITE
import com.expressmobileservice.inspection.COMPANY_WEBSITE_DISPLAY
import com.expressmobileservice.inspection.CustomerInfo
import com.expressmobileservice.inspection.InspectionFormState
import com.expressmobileservice.inspection.InspectionItem
import com.expressmobileservice.inspection.InspectionSection
import com.expressmobileservice.inspection.InspectionStatus
import com.expressmobileservice.inspection.defaultInspectionSections
import com.expressmobileservice.inspection.AppointmentStore
import com.expressmobileservice.inspection.InspectionStore
import com.expressmobileservice.inspection.displaySortMillis
import androidx.compose.ui.text.style.TextOverflow
import com.expressmobileservice.inspection.SavedInspection
import kotlinx.coroutines.delay
import com.expressmobileservice.inspection.ui.theme.InspectionColors
import com.expressmobileservice.inspection.toSavedInspection
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import java.util.UUID

enum class ReportShareType { PDF, IMAGE }

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun InspectionScreen(
    inspectionStore: InspectionStore,
    appointmentStore: AppointmentStore,
    activeInspectionId: String?,
    onShareReport: (InspectionFormState, ReportShareType, (Boolean) -> Unit) -> Unit,
    onShareError: (String) -> Unit = {},
    onInspectionSaved: (String) -> Unit = {},
    onActiveInspectionChange: (String) -> Unit = {},
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    var customerName by rememberSaveable { mutableStateOf("") }
    var customerPhone by rememberSaveable { mutableStateOf("") }
    var vehicle by rememberSaveable { mutableStateOf("") }
    var mileage by rememberSaveable { mutableStateOf("") }
    var generalNotes by rememberSaveable { mutableStateOf("") }
    var sections by remember { mutableStateOf(defaultInspectionSections()) }
    var isGenerating by remember { mutableStateOf(false) }
    var showUncheckedWarning by remember { mutableStateOf(false) }
    var pendingShareType by remember { mutableStateOf<ReportShareType?>(null) }
    var loadedInspectionId by remember { mutableStateOf<String?>(null) }
    var autoSaveHint by remember { mutableStateOf<String?>(null) }
    var showInspectionList by remember { mutableStateOf(false) }

    fun applySavedInspection(saved: SavedInspection) {
        val form = saved.toFormState()
        customerName = form.customerInfo.customerName
        customerPhone = form.customerInfo.customerPhone
        vehicle = form.customerInfo.vehicle
        mileage = form.customerInfo.mileage
        generalNotes = form.generalNotes
        sections = form.sections
        loadedInspectionId = saved.id
        autoSaveHint = "Opened: ${saved.listTitle()}"
        onActiveInspectionChange(saved.id)
    }

    LaunchedEffect(activeInspectionId) {
        val saved = activeInspectionId?.let { inspectionStore.getById(it) }
            ?: inspectionStore.mostRecent()
        if (saved != null) {
            applySavedInspection(saved)
            autoSaveHint = "Inspection loaded · auto-saved"
        }
    }

    val persistId = activeInspectionId ?: loadedInspectionId
    val currentStateProvider = rememberUpdatedState {
        InspectionFormState(
            customerInfo = CustomerInfo(
                customerName = customerName,
                customerPhone = customerPhone,
                vehicle = vehicle,
                mileage = mileage
            ),
            sections = sections,
            generalNotes = generalNotes
        )
    }

    LaunchedEffect(
        customerName,
        customerPhone,
        vehicle,
        mileage,
        generalNotes,
        sections,
        persistId
    ) {
        val id = persistId ?: return@LaunchedEffect
        delay(400)
        val saved = inspectionStore.getById(id)
        inspectionStore.save(
            currentStateProvider.value().toSavedInspection(
                id = id,
                appointmentId = saved?.appointmentId,
                inspectionDateMillis = saved?.inspectionDateMillis?.takeIf { it > 0L }
            )
        )
        autoSaveHint = "Saved automatically"
    }

    val allItems = sections.flatMap { it.items }
    val checkedCount = allItems.count { it.status != InspectionStatus.NONE }
    val totalCount = allItems.size
    val progress = if (totalCount == 0) 0f else checkedCount.toFloat() / totalCount

    fun currentState() = currentStateProvider.value()

    fun saveInspection() {
        val id = persistId ?: UUID.randomUUID().toString()
        val existing = inspectionStore.getById(id)
        inspectionStore.save(
            currentState().toSavedInspection(
                id = id,
                appointmentId = existing?.appointmentId,
                inspectionDateMillis = existing?.inspectionDateMillis?.takeIf { it > 0L }
            )
        )
        if (loadedInspectionId == null) {
            loadedInspectionId = id
        }
        autoSaveHint = "Inspection saved"
        onInspectionSaved("Inspection saved")
    }

    fun share(type: ReportShareType) {
        if (customerName.isBlank()) {
            onShareError("Enter the customer name before sending the report.")
            return
        }
        isGenerating = true
        onShareReport(currentState(), type) { success ->
            isGenerating = false
            if (!success) {
                onShareError("Could not create report. Please try again.")
            }
        }
    }

    fun beginComplete(type: ReportShareType) {
        if (customerName.isBlank()) {
            onShareError("Enter the customer name before sending the report.")
            return
        }
        val unchecked = allItems.count { it.status == InspectionStatus.NONE }
        if (unchecked > 0) {
            pendingShareType = type
            showUncheckedWarning = true
            return
        }
        share(type)
    }

    if (showUncheckedWarning) {
        val unchecked = allItems.count { it.status == InspectionStatus.NONE }
        AlertDialog(
            onDismissRequest = {
                showUncheckedWarning = false
                pendingShareType = null
            },
            title = { Text("Items not checked") },
            text = {
                Text("$unchecked inspection item${if (unchecked == 1) "" else "s"} still not marked. Send the report anyway?")
            },
            confirmButton = {
                Button(
                    onClick = {
                        showUncheckedWarning = false
                        pendingShareType?.let { share(it) }
                        pendingShareType = null
                    }
                ) {
                    Text("Send anyway")
                }
            },
            dismissButton = {
                TextButton(
                    onClick = {
                        showUncheckedWarning = false
                        pendingShareType = null
                    }
                ) {
                    Text("Go back")
                }
            }
        )
    }

    if (showInspectionList) {
        OpenInspectionsListSheet(
            inspectionStore = inspectionStore,
            appointmentStore = appointmentStore,
            currentInspectionId = persistId,
            onDismiss = { showInspectionList = false },
            onSelect = { saved ->
                applySavedInspection(saved)
                showInspectionList = false
            }
        )
    }

    Scaffold(
        modifier = modifier,
        topBar = {
            TopAppBar(
                title = {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Image(
                            painter = painterResource(R.drawable.ic_company_logo),
                            contentDescription = "Express Mobile Service logo",
                            modifier = Modifier
                                .size(40.dp)
                                .clip(RoundedCornerShape(8.dp))
                        )
                        Spacer(modifier = Modifier.width(12.dp))
                        Column {
                            Text(
                                text = COMPANY_NAME,
                                style = MaterialTheme.typography.titleMedium,
                                fontWeight = FontWeight.Bold
                            )
                            Row(
                                verticalAlignment = Alignment.CenterVertically,
                                modifier = Modifier.clickable { openUri(context, COMPANY_PHONE_URI) }
                            ) {
                                Icon(
                                    imageVector = Icons.Default.Phone,
                                    contentDescription = null,
                                    modifier = Modifier.size(14.dp)
                                )
                                Spacer(modifier = Modifier.width(4.dp))
                                Text(
                                    text = COMPANY_PHONE_DISPLAY,
                                    style = MaterialTheme.typography.bodySmall
                                )
                            }
                        }
                    }
                },
                actions = {
                    TextButton(onClick = { showInspectionList = true }) {
                        Icon(
                            imageVector = Icons.Default.List,
                            contentDescription = null,
                            tint = Color.White,
                            modifier = Modifier.size(18.dp)
                        )
                        Spacer(modifier = Modifier.width(4.dp))
                        Text(
                            text = "Open list",
                            color = Color.White,
                            fontWeight = FontWeight.SemiBold
                        )
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
                } else {
                    Text(
                        text = "$checkedCount of $totalCount items checked",
                        style = MaterialTheme.typography.bodySmall,
                        modifier = Modifier.padding(bottom = 4.dp)
                    )
                    LinearProgressIndicator(
                        progress = { progress },
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(bottom = 8.dp)
                    )
                }
                Button(
                    onClick = { saveInspection() },
                    enabled = !isGenerating,
                    modifier = Modifier.fillMaxWidth(),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = MaterialTheme.colorScheme.secondary
                    )
                ) {
                    Icon(Icons.Default.Save, contentDescription = null)
                    Spacer(modifier = Modifier.width(8.dp))
                    Text("Save inspection")
                }
                Spacer(modifier = Modifier.height(8.dp))
                Button(
                    onClick = { beginComplete(ReportShareType.PDF) },
                    enabled = !isGenerating,
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Icon(Icons.Default.PictureAsPdf, contentDescription = null)
                    Spacer(modifier = Modifier.width(8.dp))
                    Text("Send as PDF")
                }
                Spacer(modifier = Modifier.height(8.dp))
                OutlinedButton(
                    onClick = { beginComplete(ReportShareType.IMAGE) },
                    enabled = !isGenerating,
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Icon(Icons.Default.Image, contentDescription = null)
                    Spacer(modifier = Modifier.width(8.dp))
                    Text("Send as Image (JPEG)")
                }
                Spacer(modifier = Modifier.height(8.dp))
                OutlinedButton(
                    onClick = {
                        customerName = ""
                        customerPhone = ""
                        vehicle = ""
                        mileage = ""
                        generalNotes = ""
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
                Column(modifier = Modifier.padding(14.dp)) {
                    Text(
                        text = "Tap Good, Bad, or Replace. Add notes if needed.",
                        style = MaterialTheme.typography.bodyMedium
                    )
                    autoSaveHint?.let { hint ->
                        Text(
                            text = hint,
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.primary,
                            modifier = Modifier.padding(top = 4.dp)
                        )
                    }
                }
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

            Card(modifier = Modifier.fillMaxWidth()) {
                Column(
                    modifier = Modifier.padding(14.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Text("Additional Notes", fontWeight = FontWeight.SemiBold)
                    Text(
                        text = "Overall comments, recommendations, or follow-up for the customer.",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    OutlinedTextField(
                        value = generalNotes,
                        onValueChange = { generalNotes = it },
                        label = { Text("Notes") },
                        placeholder = { Text("e.g. Recommend brake service within 3,000 miles") },
                        modifier = Modifier.fillMaxWidth(),
                        minLines = 3,
                        maxLines = 6
                    )
                }
            }

            CompanyFooterLinks()

            Spacer(modifier = Modifier.height(100.dp))
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun OpenInspectionsListSheet(
    inspectionStore: InspectionStore,
    appointmentStore: AppointmentStore,
    currentInspectionId: String?,
    onDismiss: () -> Unit,
    onSelect: (SavedInspection) -> Unit
) {
    val sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true)
    var inspections by remember { mutableStateOf(inspectionStore.getAll(appointmentStore)) }
    val dateFormat = remember { SimpleDateFormat("MMM d, yyyy · h:mm a", Locale.US) }

    LaunchedEffect(Unit) {
        inspections = inspectionStore.getAll(appointmentStore)
    }

    ModalBottomSheet(
        onDismissRequest = onDismiss,
        sheetState = sheetState
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(bottom = 24.dp)
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 4.dp),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Text(
                    text = "Saved inspections",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold
                )
                IconButton(onClick = onDismiss) {
                    Icon(Icons.Default.Close, contentDescription = "Close")
                }
            }
            Text(
                text = "${inspections.size} saved · earliest first",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(horizontal = 16.dp, vertical = 4.dp)
            )
            HorizontalDivider(modifier = Modifier.padding(vertical = 8.dp))
            if (inspections.isEmpty()) {
                Text(
                    text = "No saved inspections yet.\nSave a job from Appointments or tap Save inspection.",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(horizontal = 16.dp, vertical = 24.dp)
                )
            } else {
                LazyColumn(
                    modifier = Modifier
                        .fillMaxWidth()
                        .heightIn(max = 480.dp)
                ) {
                    items(inspections, key = { it.id }) { inspection ->
                        val isCurrent = inspection.id == currentInspectionId
                        Column(
                            modifier = Modifier
                                .fillMaxWidth()
                                .clickable { onSelect(inspection) }
                                .background(
                                    if (isCurrent) {
                                        MaterialTheme.colorScheme.primaryContainer.copy(alpha = 0.45f)
                                    } else {
                                        Color.Transparent
                                    }
                                )
                                .padding(horizontal = 16.dp, vertical = 12.dp)
                        ) {
                            Text(
                                text = inspection.listTitle(),
                                fontWeight = FontWeight.SemiBold,
                                maxLines = 2,
                                overflow = TextOverflow.Ellipsis
                            )
                            val subtitle = inspection.listSubtitle()
                            if (subtitle.isNotBlank()) {
                                Text(
                                    text = subtitle,
                                    style = MaterialTheme.typography.bodySmall,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                                    maxLines = 1,
                                    overflow = TextOverflow.Ellipsis,
                                    modifier = Modifier.padding(top = 2.dp)
                                )
                            }
                            Text(
                                text = dateFormat.format(
                                    Date(inspection.displaySortMillis(appointmentStore))
                                ),
                                style = MaterialTheme.typography.labelSmall,
                                color = MaterialTheme.colorScheme.primary,
                                modifier = Modifier.padding(top = 4.dp)
                            )
                        }
                        HorizontalDivider()
                    }
                }
            }
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

@Composable
private fun CompanyFooterLinks() {
    val context = LocalContext.current
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(
            modifier = Modifier.padding(14.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Text(COMPANY_NAME, fontWeight = FontWeight.SemiBold)
            FooterLinkRow(
                prefix = "Website: ",
                label = COMPANY_WEBSITE_DISPLAY,
                onClick = { openUri(context, COMPANY_WEBSITE) },
                iconPainter = painterResource(R.drawable.ic_company_logo)
            )
            FooterLinkRow(
                icon = Icons.Default.Phone,
                prefix = "Call: ",
                label = COMPANY_PHONE_DISPLAY,
                onClick = { openUri(context, COMPANY_PHONE_URI) }
            )
            FooterLinkRow(
                icon = Icons.Default.Star,
                prefix = "Google review: ",
                label = "Leave a review on Google Maps",
                onClick = { openUri(context, COMPANY_GOOGLE_REVIEW_URL) }
            )
        }
    }
}

@Composable
private fun FooterLinkRow(
    prefix: String,
    label: String,
    onClick: () -> Unit,
    icon: ImageVector? = null,
    iconPainter: Painter? = null
) {
    Row(
        verticalAlignment = Alignment.CenterVertically,
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick)
    ) {
        when {
            iconPainter != null -> {
                Image(
                    painter = iconPainter,
                    contentDescription = null,
                    modifier = Modifier
                        .size(18.dp)
                        .clip(RoundedCornerShape(4.dp))
                )
            }
            icon != null -> {
                Icon(
                    imageVector = icon,
                    contentDescription = null,
                    modifier = Modifier.size(18.dp),
                    tint = MaterialTheme.colorScheme.primary
                )
            }
        }
        Spacer(modifier = Modifier.width(8.dp))
        Text(
            text = prefix + label,
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.primary
        )
    }
}

private fun openUri(context: android.content.Context, uri: String) {
    context.startActivity(Intent(Intent.ACTION_VIEW, Uri.parse(uri)))
}
