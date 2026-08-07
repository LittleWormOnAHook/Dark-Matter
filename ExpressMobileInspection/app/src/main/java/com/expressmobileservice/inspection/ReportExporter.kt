package com.expressmobileservice.inspection

import android.content.Context
import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Paint
import android.graphics.Rect
import android.graphics.RectF
import android.graphics.Typeface
import android.graphics.pdf.PdfDocument
import androidx.core.content.ContextCompat
import java.io.File
import java.io.FileOutputStream
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

object ReportExporter {

    private const val PAGE_WIDTH = 612
    private const val PAGE_HEIGHT = 792
    private const val IMAGE_WIDTH = 1080

    fun exportPdf(context: Context, state: InspectionFormState): File {
        PdfLinkAnnotator.init(context)
        val logo = loadLogoBitmap(context, (PAGE_WIDTH * 0.11f).toInt())
        val renderer = ReportRenderer(PAGE_WIDTH, logo, collectLinks = true)
        val pages = renderer.buildPages(state, PAGE_HEIGHT)
        val pdf = PdfDocument()

        pages.forEachIndexed { index, pageContent ->
            val pageInfo = PdfDocument.PageInfo.Builder(PAGE_WIDTH, PAGE_HEIGHT, index + 1).create()
            val page = pdf.startPage(pageInfo)
            renderer.drawPage(page.canvas, state, pageContent, index)
            pdf.finishPage(page)
        }

        val file = File(reportsDir(context), reportFileName("pdf"))
        FileOutputStream(file).use { pdf.writeTo(it) }
        pdf.close()
        PdfLinkAnnotator.annotate(file, renderer.linkAnnotations, PAGE_HEIGHT)
        return file
    }

    fun exportImage(context: Context, state: InspectionFormState): File {
        val logo = loadLogoBitmap(context, (IMAGE_WIDTH * 0.11f).toInt())
        val renderer = ReportRenderer(IMAGE_WIDTH, logo)
        val height = renderer.measureTotalHeight(state)
        val bitmap = Bitmap.createBitmap(IMAGE_WIDTH, height, Bitmap.Config.ARGB_8888)
        val canvas = Canvas(bitmap)
        canvas.drawColor(ReportRenderer.COLOR_WHITE)
        renderer.drawFullReport(canvas, state)
        val file = File(reportsDir(context), reportFileName("jpg"))
        FileOutputStream(file).use { out ->
            bitmap.compress(Bitmap.CompressFormat.JPEG, 92, out)
        }
        bitmap.recycle()
        return file
    }

    private fun reportsDir(context: Context): File =
        File(context.cacheDir, "reports").apply { mkdirs() }

    private fun reportFileName(extension: String): String {
        val stamp = SimpleDateFormat("yyyyMMdd_HHmmss", Locale.US).format(Date())
        return "ExpressMobileInspection_$stamp.$extension"
    }

    private fun loadLogoBitmap(context: Context, sizePx: Int): Bitmap {
        val drawable = ContextCompat.getDrawable(context, R.drawable.ic_company_logo)
            ?: return Bitmap.createBitmap(sizePx, sizePx, Bitmap.Config.ARGB_8888)
        val bitmap = Bitmap.createBitmap(sizePx, sizePx, Bitmap.Config.ARGB_8888)
        val canvas = Canvas(bitmap)
        drawable.setBounds(0, 0, sizePx, sizePx)
        drawable.draw(canvas)
        return bitmap
    }
}

internal class ReportRenderer(
    private val pageWidth: Int,
    private val logoBitmap: Bitmap? = null,
    private val collectLinks: Boolean = false
) {
    val linkAnnotations = mutableListOf<ReportLink>()

    private val margin = (pageWidth * 0.06f).toInt()
    private val contentWidth = pageWidth - margin * 2

    private val titlePaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = COLOR_WHITE
        textSize = pageWidth * 0.048f
        typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
    }
    private val subtitlePaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = COLOR_WHITE
        textSize = pageWidth * 0.028f
    }
    private val headingPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = COLOR_NAVY
        textSize = pageWidth * 0.034f
        typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
    }
    private val labelPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = COLOR_MUTED
        textSize = pageWidth * 0.024f
        typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
    }
    private val valuePaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = COLOR_TEXT
        textSize = pageWidth * 0.028f
    }
    private val sectionPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = COLOR_PRIMARY
        textSize = pageWidth * 0.03f
        typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
    }
    private val itemPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = COLOR_TEXT
        textSize = pageWidth * 0.027f
    }
    private val notePaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = COLOR_MUTED
        textSize = pageWidth * 0.024f
    }
    private val footerPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = COLOR_MUTED
        textSize = pageWidth * 0.022f
    }
    private val linkPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = COLOR_LINK
        textSize = pageWidth * 0.024f
        typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
    }
    private val headerLinkPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = COLOR_WHITE
        textSize = pageWidth * 0.028f
        typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
    }
    private val headerFill = Paint().apply { color = COLOR_PRIMARY }
    private val boxFill = Paint().apply { color = COLOR_LIGHT_BG }
    private val rowAltFill = Paint().apply { color = COLOR_ROW_ALT }
    private val linePaint = Paint().apply {
        color = COLOR_BORDER
        strokeWidth = 1.5f
    }
    private val tableHeaderFill = Paint().apply { color = COLOR_NAVY }
    private val tableHeaderText = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = COLOR_WHITE
        textSize = pageWidth * 0.024f
        typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
    }

    data class PageContent(
        val drawHeader: Boolean,
        val drawCustomerBox: Boolean,
        val drawTableHeader: Boolean,
        val drawFooter: Boolean,
        val sections: List<SectionSlice>
    )

    data class SectionSlice(
        val sectionTitle: String?,
        val items: List<InspectionItem>
    )

    fun measureTotalHeight(state: InspectionFormState): Int {
        var y = margin
        y += headerHeight()
        y += customerBoxHeight()
        y += tableHeaderHeight()
        state.sections.forEach { section ->
            y += sectionHeaderHeight()
            section.items.forEach { item -> y += itemRowHeight(item) }
        }
        y += footerHeight(state)
        return y + margin
    }

    fun buildPages(state: InspectionFormState, pageHeight: Int): List<PageContent> {
        val slices = mutableListOf<SectionSlice>()
        state.sections.forEach { section ->
            slices.add(SectionSlice(section.title, emptyList()))
            section.items.forEach { item -> slices.add(SectionSlice(null, listOf(item))) }
        }

        val pages = mutableListOf<PageContent>()
        var sliceIndex = 0
        var currentSections = mutableListOf<SectionSlice>()
        var drawHeader = true
        var drawCustomer = true
        var drawTable = true

        fun usedHeight(includeFooter: Boolean): Int {
            var h = margin
            if (drawHeader) h += headerHeight()
            if (drawCustomer) h += customerBoxHeight()
            if (drawTable) h += tableHeaderHeight()
            currentSections.forEach { slice ->
                if (slice.sectionTitle != null) h += sectionHeaderHeight()
                slice.items.forEach { item -> h += itemRowHeight(item) }
            }
            if (includeFooter) h += footerHeight(state)
            return h
        }

        fun flushPage(drawFooter: Boolean) {
            pages.add(
                PageContent(
                    drawHeader = drawHeader,
                    drawCustomerBox = drawCustomer,
                    drawTableHeader = drawTable,
                    drawFooter = drawFooter,
                    sections = currentSections.toList()
                )
            )
            currentSections = mutableListOf()
            drawHeader = false
            drawCustomer = false
            drawTable = false
        }

        while (sliceIndex < slices.size) {
            val slice = slices[sliceIndex]
            val sliceHeight = when {
                slice.sectionTitle != null -> sectionHeaderHeight()
                slice.items.isNotEmpty() -> itemRowHeight(slice.items.first())
                else -> 0
            }

            if (usedHeight(includeFooter = false) + sliceHeight > pageHeight - margin &&
                currentSections.isNotEmpty()
            ) {
                flushPage(drawFooter = false)
                continue
            }

            currentSections.add(slice)
            sliceIndex++
        }

        if (usedHeight(includeFooter = true) > pageHeight - margin && currentSections.isNotEmpty()) {
            flushPage(drawFooter = false)
        }

        flushPage(drawFooter = true)
        return pages
    }

    fun drawFullReport(canvas: Canvas, state: InspectionFormState) {
        var y = margin
        y = drawHeader(canvas, y, pageIndex = 0)
        y = drawCustomerBox(canvas, state, y)
        y = drawTableHeader(canvas, y)
        state.sections.forEach { section ->
            y = drawSectionHeader(canvas, section.title, y)
            section.items.forEachIndexed { index, item ->
                y = drawItemRow(canvas, item, y, index % 2 == 1)
            }
        }
        drawFooter(canvas, state, y, pageIndex = 0)
    }

    fun drawPage(canvas: Canvas, state: InspectionFormState, page: PageContent, pageIndex: Int) {
        canvas.drawColor(COLOR_WHITE)
        var y = margin
        if (page.drawHeader) y = drawHeader(canvas, y, pageIndex)
        if (page.drawCustomerBox) y = drawCustomerBox(canvas, state, y)
        if (page.drawTableHeader) y = drawTableHeader(canvas, y)
        page.sections.forEach { slice ->
            if (slice.sectionTitle != null) y = drawSectionHeader(canvas, slice.sectionTitle, y)
            slice.items.forEachIndexed { index, item ->
                y = drawItemRow(canvas, item, y, index % 2 == 1)
            }
        }
        if (page.drawFooter) drawFooter(canvas, state, y, pageIndex)
    }

    private fun headerHeight() = (pageWidth * 0.16f).toInt()
    private fun customerBoxHeight() = (pageWidth * 0.22f).toInt()
    private fun tableHeaderHeight() = (pageWidth * 0.065f).toInt()
    private fun sectionHeaderHeight() = (pageWidth * 0.055f).toInt()
    private fun footerHeight(state: InspectionFormState): Int {
        val lineHeight = (pageWidth * 0.035f).toInt()
        val linkLines = 3
        val base = (pageWidth * 0.12f).toInt() + linkLines * lineHeight
        return base + generalNotesHeight(state.generalNotes)
    }

    private fun generalNotesHeight(notes: String): Int {
        if (notes.isBlank()) return 0
        val lines = wrapText(notes, valuePaint, contentWidth.toFloat())
        val lineHeight = (pageWidth * 0.035f).toInt()
        return (pageWidth * 0.07f).toInt() + lines.size * lineHeight
    }

    private fun wrapText(text: String, paint: Paint, maxWidth: Float): List<String> {
        val words = text.trim().split(Regex("\\s+"))
        if (words.isEmpty() || words.singleOrNull()?.isEmpty() == true) return emptyList()
        val lines = mutableListOf<String>()
        var current = words.first()
        words.drop(1).forEach { word ->
            val candidate = "$current $word"
            if (paint.measureText(candidate) <= maxWidth) {
                current = candidate
            } else {
                lines.add(current)
                current = word
            }
        }
        lines.add(current)
        return lines
    }

    private fun itemRowHeight(item: InspectionItem): Int {
        val base = (pageWidth * 0.075f).toInt()
        return if (item.notes.isBlank()) base else base + (pageWidth * 0.04f).toInt()
    }

    private fun drawHeader(canvas: Canvas, yStart: Int, pageIndex: Int): Int {
        val h = headerHeight()
        canvas.drawRect(0f, yStart.toFloat(), pageWidth.toFloat(), (yStart + h).toFloat(), headerFill)
        val pad = margin.toFloat()
        val logoSize = (pageWidth * 0.11f).toInt()
        val textStartX = if (logoBitmap != null) pad + logoSize + pageWidth * 0.025f else pad

        logoBitmap?.let { logo ->
            val logoTop = yStart + (h - logoSize) / 2
            canvas.drawBitmap(logo, null, Rect(margin, logoTop, margin + logoSize, logoTop + logoSize), null)
        }

        canvas.drawText(COMPANY_NAME, textStartX, yStart + h * 0.38f, titlePaint)
        canvas.drawText("Vehicle Inspection Report", textStartX, yStart + h * 0.62f, subtitlePaint)
        val phonePrefix = "Phone: "
        val phoneY = yStart + h * 0.62f
        val phonePrefixWidth = subtitlePaint.measureText(phonePrefix)
        val phoneLabelWidth = headerLinkPaint.measureText(COMPANY_PHONE_DISPLAY)
        val phoneBlockWidth = phonePrefixWidth + phoneLabelWidth
        val phoneX = pageWidth - pad - phoneBlockWidth
        canvas.drawText(phonePrefix, phoneX, phoneY, subtitlePaint)
        val linkX = phoneX + phonePrefixWidth
        canvas.drawText(COMPANY_PHONE_DISPLAY, linkX, phoneY, headerLinkPaint)
        drawLinkUnderline(canvas, linkX, phoneY, phoneLabelWidth, headerLinkPaint)
        recordLink(
            pageIndex = pageIndex,
            url = COMPANY_PHONE_URI,
            left = linkX,
            top = phoneY - headerLinkPaint.textSize * 0.85f,
            right = linkX + phoneLabelWidth,
            bottom = phoneY + headerLinkPaint.textSize * 0.2f
        )
        val date = SimpleDateFormat("MMMM d, yyyy", Locale.US).format(Date())
        val dateWidth = subtitlePaint.measureText(date)
        canvas.drawText(date, pageWidth - pad - dateWidth, yStart + h * 0.38f, subtitlePaint)
        return yStart + h + (pageWidth * 0.02f).toInt()
    }

    private fun drawCustomerBox(canvas: Canvas, state: InspectionFormState, yStart: Int): Int {
        val h = customerBoxHeight()
        val rect = RectF(margin.toFloat(), yStart.toFloat(), (pageWidth - margin).toFloat(), (yStart + h).toFloat())
        canvas.drawRoundRect(rect, 12f, 12f, boxFill)
        canvas.drawRoundRect(rect, 12f, 12f, Paint().apply {
            color = COLOR_BORDER
            style = Paint.Style.STROKE
            strokeWidth = 2f
        })

        val col1 = margin + (pageWidth * 0.03f).toInt()
        val col2 = pageWidth / 2 + (pageWidth * 0.01f).toInt()
        var y = yStart + (pageWidth * 0.05f).toInt()
        val rowGap = (pageWidth * 0.055f).toInt()

        fun field(label: String, value: String, x: Int, rowY: Int) {
            canvas.drawText(label.uppercase(Locale.US), x.toFloat(), rowY.toFloat(), labelPaint)
            canvas.drawText(value.ifBlank { "—" }, x.toFloat(), rowY + pageWidth * 0.032f, valuePaint)
        }

        field("Customer", state.customerInfo.customerName, col1, y)
        field("Phone", state.customerInfo.customerPhone, col2, y)
        y += rowGap
        field("Vehicle", state.customerInfo.vehicle, col1, y)
        field("Mileage", state.customerInfo.mileage, col2, y)

        return yStart + h + (pageWidth * 0.025f).toInt()
    }

    private fun drawTableHeader(canvas: Canvas, yStart: Int): Int {
        val h = tableHeaderHeight()
        canvas.drawRect(
            margin.toFloat(), yStart.toFloat(),
            (pageWidth - margin).toFloat(), (yStart + h).toFloat(),
            tableHeaderFill
        )
        val itemX = margin + (pageWidth * 0.02f)
        val statusX = pageWidth - margin - (pageWidth * 0.28f)
        val notesX = pageWidth - margin - (pageWidth * 0.14f)
        val textY = yStart + h * 0.62f
        canvas.drawText("Inspection Item", itemX, textY, tableHeaderText)
        canvas.drawText("Status", statusX, textY, tableHeaderText)
        canvas.drawText("Notes", notesX, textY, tableHeaderText)
        return yStart + h
    }

    private fun drawSectionHeader(canvas: Canvas, title: String, yStart: Int): Int {
        val h = sectionHeaderHeight()
        canvas.drawRect(
            margin.toFloat(), yStart.toFloat(),
            (pageWidth - margin).toFloat(), (yStart + h).toFloat(),
            Paint().apply { color = COLOR_SECTION_BG }
        )
        canvas.drawText(title, margin + pageWidth * 0.02f, yStart + h * 0.65f, sectionPaint)
        return yStart + h
    }

    private fun drawItemRow(canvas: Canvas, item: InspectionItem, yStart: Int, alt: Boolean): Int {
        val h = itemRowHeight(item)
        if (alt) {
            canvas.drawRect(
                margin.toFloat(), yStart.toFloat(),
                (pageWidth - margin).toFloat(), (yStart + h).toFloat(),
                rowAltFill
            )
        }
        canvas.drawLine(
            margin.toFloat(), (yStart + h).toFloat(),
            (pageWidth - margin).toFloat(), (yStart + h).toFloat(),
            linePaint
        )

        val itemX = margin + (pageWidth * 0.02f)
        canvas.drawText(item.label, itemX, yStart + h * 0.42f, itemPaint)
        drawStatusBadge(canvas, item.status, pageWidth - margin - (pageWidth * 0.26f).toInt(), yStart + (h * 0.18f).toInt())

        if (item.notes.isNotBlank()) {
            canvas.drawText(
                item.notes,
                pageWidth - margin - (pageWidth * 0.34f),
                yStart + h * 0.78f,
                notePaint
            )
        }

        return yStart + h
    }

    private fun drawStatusBadge(canvas: Canvas, status: InspectionStatus, x: Int, y: Int) {
        val (label, bg, fg) = when (status) {
            InspectionStatus.GOOD -> Triple("GOOD", COLOR_GOOD_BG, COLOR_GOOD)
            InspectionStatus.BAD -> Triple("BAD", COLOR_BAD_BG, COLOR_BAD)
            InspectionStatus.REPLACE -> Triple("REPLACE", COLOR_REPLACE_BG, COLOR_REPLACE)
            InspectionStatus.NONE -> Triple("—", COLOR_NONE_BG, COLOR_MUTED)
        }
        val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color = fg
            textSize = pageWidth * 0.022f
            typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
        }
        val padH = pageWidth * 0.018f
        val textW = paint.measureText(label)
        val badgeW = textW + padH * 2
        val badgeH = pageWidth * 0.042f
        val rect = RectF(x.toFloat(), y.toFloat(), x + badgeW, y + badgeH)
        canvas.drawRoundRect(rect, 8f, 8f, Paint().apply { color = bg })
        canvas.drawRoundRect(rect, 8f, 8f, Paint().apply {
            color = fg
            style = Paint.Style.STROKE
            strokeWidth = 2f
        })
        canvas.drawText(label, x + padH, y + badgeH * 0.72f, paint)
    }

    private fun drawFooter(canvas: Canvas, state: InspectionFormState, yStart: Int, pageIndex: Int) {
        val items = state.sections.flatMap { it.items }
        val good = items.count { it.status == InspectionStatus.GOOD }
        val bad = items.count { it.status == InspectionStatus.BAD }
        val replace = items.count { it.status == InspectionStatus.REPLACE }
        val unchecked = items.count { it.status == InspectionStatus.NONE }

        var y = yStart + (pageWidth * 0.03f).toInt()
        canvas.drawLine(margin.toFloat(), y.toFloat(), (pageWidth - margin).toFloat(), y.toFloat(), linePaint)
        y += (pageWidth * 0.04f).toInt()

        val summary = "Summary:  $good Good   •   $bad Bad   •   $replace Replace   •   $unchecked Not Checked"
        canvas.drawText(summary, margin.toFloat(), y.toFloat(), headingPaint)
        y += (pageWidth * 0.045f).toInt()

        if (state.generalNotes.isNotBlank()) {
            canvas.drawText("Additional Notes", margin.toFloat(), y.toFloat(), sectionPaint)
            y += (pageWidth * 0.04f).toInt()
            val lines = wrapText(state.generalNotes, valuePaint, contentWidth.toFloat())
            val lineHeight = (pageWidth * 0.035f).toInt()
            lines.forEach { line ->
                canvas.drawText(line, margin.toFloat(), y.toFloat(), valuePaint)
                y += lineHeight
            }
            y += (pageWidth * 0.02f).toInt()
        }

        val lineHeight = (pageWidth * 0.035f).toInt()
        canvas.drawText("Thank you for choosing $COMPANY_NAME.", margin.toFloat(), y.toFloat(), footerPaint)
        y += lineHeight
        y = drawFooterLinkRow(
            canvas,
            pageIndex,
            y,
            "Website: ",
            COMPANY_WEBSITE_DISPLAY,
            COMPANY_WEBSITE
        )
        y = drawFooterLinkRow(
            canvas,
            pageIndex,
            y,
            "Call: ",
            COMPANY_PHONE_DISPLAY,
            COMPANY_PHONE_URI
        )
        drawFooterLinkRow(
            canvas,
            pageIndex,
            y,
            "Google review: ",
            "Leave a review on Google Maps",
            COMPANY_GOOGLE_REVIEW_URL
        )
    }

    private fun drawFooterLinkRow(
        canvas: Canvas,
        pageIndex: Int,
        y: Int,
        prefix: String,
        linkLabel: String,
        url: String
    ): Int {
        val x = margin.toFloat()
        val textY = y.toFloat()
        canvas.drawText(prefix, x, textY, footerPaint)
        val prefixWidth = footerPaint.measureText(prefix)
        val linkX = x + prefixWidth
        canvas.drawText(linkLabel, linkX, textY, linkPaint)
        val linkWidth = linkPaint.measureText(linkLabel)
        drawLinkUnderline(canvas, linkX, textY, linkWidth, linkPaint)
        recordLink(
            pageIndex = pageIndex,
            url = url,
            left = linkX,
            top = textY - linkPaint.textSize * 0.85f,
            right = linkX + linkWidth,
            bottom = textY + linkPaint.textSize * 0.2f
        )
        return y + (pageWidth * 0.035f).toInt()
    }

    private fun drawLinkUnderline(canvas: Canvas, x: Float, textY: Float, width: Float, paint: Paint) {
        val underlineY = textY + paint.textSize * 0.08f
        canvas.drawLine(x, underlineY, x + width, underlineY, paint)
    }

    private fun recordLink(
        pageIndex: Int,
        url: String,
        left: Float,
        top: Float,
        right: Float,
        bottom: Float
    ) {
        if (!collectLinks) return
        linkAnnotations.add(
            ReportLink(
                pageIndex = pageIndex,
                url = url,
                left = left,
                top = top,
                right = right,
                bottom = bottom
            )
        )
    }

    companion object {
        const val COLOR_PRIMARY = 0xFF1565C0.toInt()
        const val COLOR_NAVY = 0xFF0D47A1.toInt()
        const val COLOR_WHITE = 0xFFFFFFFF.toInt()
        const val COLOR_TEXT = 0xFF1A1A1A.toInt()
        const val COLOR_MUTED = 0xFF616161.toInt()
        const val COLOR_LIGHT_BG = 0xFFF5F7FA.toInt()
        const val COLOR_ROW_ALT = 0xFFFAFBFC.toInt()
        const val COLOR_BORDER = 0xFFE0E0E0.toInt()
        const val COLOR_SECTION_BG = 0xFFE3F2FD.toInt()
        const val COLOR_GOOD = 0xFF2E7D32.toInt()
        const val COLOR_GOOD_BG = 0xFFE8F5E9.toInt()
        const val COLOR_BAD = 0xFFC62828.toInt()
        const val COLOR_BAD_BG = 0xFFFFEBEE.toInt()
        const val COLOR_REPLACE = 0xFFE65100.toInt()
        const val COLOR_REPLACE_BG = 0xFFFFF3E0.toInt()
        const val COLOR_NONE_BG = 0xFFEEEEEE.toInt()
        const val COLOR_LINK = 0xFF1565C0.toInt()
    }
}
