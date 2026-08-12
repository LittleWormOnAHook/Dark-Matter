package com.expressmobileservice.inspection

import android.content.Context
import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Paint
import android.graphics.RectF
import android.graphics.Typeface
import android.text.Layout
import android.text.StaticLayout
import android.text.TextPaint
import java.io.File
import java.io.FileOutputStream
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

/**
 * Renders the thank-you preview as a JPEG for MMS — purple/gold card with button rows.
 * Google Messages does not render HTML; the card image is the styled message body.
 */
object ThankYouCardExporter {

    const val CARD_WIDTH = 1080

    private const val HORIZONTAL_PAD = 72f
    private const val COLOR_SURFACE = 0xFF2C1432.toInt()
    private const val COLOR_GOLD = 0xFFFFD700.toInt()
    private const val COLOR_WHITE = 0xFFFFFFFF.toInt()
    private const val COLOR_MUTED = 0xFFBDB5D5.toInt()
    private const val COLOR_BUTTON_BG = 0xFF441F4D.toInt()

    fun export(context: Context): File {
        val height = measureCardHeight(CARD_WIDTH.toFloat()).toInt().coerceAtLeast(900)
        val bitmap = Bitmap.createBitmap(CARD_WIDTH, height, Bitmap.Config.ARGB_8888)
        val canvas = Canvas(bitmap)
        drawCard(canvas, CARD_WIDTH.toFloat(), collectLinks = false)
        val file = File(context.cacheDir, "reports").apply { mkdirs() }
            .resolve(cardFileName())
        FileOutputStream(file).use { out ->
            bitmap.compress(Bitmap.CompressFormat.JPEG, 92, out)
        }
        bitmap.recycle()
        return file
    }

    fun measureCardHeight(cardWidth: Float): Float {
        val scale = cardWidth / CARD_WIDTH
        val contentWidth = ((CARD_WIDTH - HORIZONTAL_PAD * 2) * scale).toInt().coerceAtLeast(1)
        val bodyPaint = bodyPaintForScale(scale)
        val promptPaint = promptPaintForScale(scale)
        val buttonHeight = 96f * scale
        val buttonGap = 24f * scale
        val sectionGap = 32f * scale

        val bodyLayout = staticLayout(THANK_YOU_BODY, bodyPaint, contentWidth)
        val promptLayout = staticLayout(THANK_YOU_PROMPT, promptPaint, contentWidth)

        var y = 56f * scale
        y += 80f * scale
        y += bodyLayout.height + sectionGap
        y += promptLayout.height + sectionGap
        y += buttonHeight + buttonGap // PDF badge
        y += buttonHeight + buttonGap // Google review
        y += buttonHeight + 48f * scale // Website
        return y + 24f * scale
    }

    fun drawCard(
        canvas: Canvas,
        cardWidth: Float,
        collectLinks: Boolean = false,
        pageIndex: Int = 0,
        links: MutableList<ReportLink>? = null
    ) {
        val scale = cardWidth / CARD_WIDTH
        canvas.drawColor(COLOR_SURFACE)

        val horizontalPad = HORIZONTAL_PAD * scale
        val contentWidth = (cardWidth - horizontalPad * 2).toInt().coerceAtLeast(1)
        val headingPaint = headingPaintForScale(scale)
        val bodyPaint = bodyPaintForScale(scale)
        val promptPaint = promptPaintForScale(scale)
        val buttonPaint = buttonPaintForScale(scale)

        val bodyLayout = staticLayout(THANK_YOU_BODY, bodyPaint, contentWidth)
        val promptLayout = staticLayout(THANK_YOU_PROMPT, promptPaint, contentWidth)

        val buttonHeight = 96f * scale
        val buttonGap = 24f * scale
        val sectionGap = 32f * scale
        val cornerRadius = 28f * scale

        val cardRect = RectF(
            24f * scale,
            24f * scale,
            cardWidth - 24f * scale,
            measureCardHeight(cardWidth) - 12f * scale
        )
        val cardPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply { color = COLOR_SURFACE }
        canvas.drawRoundRect(cardRect, 32f * scale, 32f * scale, cardPaint)

        var y = 120f * scale
        canvas.drawText(THANK_YOU_HEADING, cardWidth / 2f, y, headingPaint)

        y += sectionGap
        canvas.save()
        canvas.translate(horizontalPad, y)
        bodyLayout.draw(canvas)
        canvas.restore()
        y += bodyLayout.height + sectionGap

        canvas.save()
        canvas.translate(horizontalPad, y)
        promptLayout.draw(canvas)
        canvas.restore()
        y += promptLayout.height + sectionGap

        y = drawButtonRow(
            canvas = canvas,
            left = horizontalPad,
            cardWidth = cardWidth,
            top = y,
            label = THANK_YOU_PDF_LABEL,
            buttonHeight = buttonHeight,
            cornerRadius = cornerRadius,
            buttonPaint = buttonPaint,
            url = null,
            collectLinks = collectLinks,
            pageIndex = pageIndex,
            links = links
        )
        y += buttonGap
        y = drawButtonRow(
            canvas = canvas,
            left = horizontalPad,
            cardWidth = cardWidth,
            top = y,
            label = THANK_YOU_GOOGLE_REVIEW_LABEL,
            buttonHeight = buttonHeight,
            cornerRadius = cornerRadius,
            buttonPaint = buttonPaint,
            url = COMPANY_GOOGLE_REVIEW_URL,
            collectLinks = collectLinks,
            pageIndex = pageIndex,
            links = links
        )
        y += buttonGap
        drawButtonRow(
            canvas = canvas,
            left = horizontalPad,
            cardWidth = cardWidth,
            top = y,
            label = THANK_YOU_WEBSITE_LABEL,
            buttonHeight = buttonHeight,
            cornerRadius = cornerRadius,
            buttonPaint = buttonPaint,
            url = COMPANY_WEBSITE,
            collectLinks = collectLinks,
            pageIndex = pageIndex,
            links = links
        )
    }

    private fun drawButtonRow(
        canvas: Canvas,
        left: Float,
        cardWidth: Float,
        top: Float,
        label: String,
        buttonHeight: Float,
        cornerRadius: Float,
        buttonPaint: TextPaint,
        url: String?,
        collectLinks: Boolean,
        pageIndex: Int,
        links: MutableList<ReportLink>?
    ): Float {
        val rect = RectF(
            left,
            top,
            cardWidth - left,
            top + buttonHeight
        )
        val fillPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply { color = COLOR_BUTTON_BG }
        canvas.drawRoundRect(rect, cornerRadius, cornerRadius, fillPaint)
        val strokePaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            color = COLOR_GOLD
            style = Paint.Style.STROKE
            strokeWidth = 3f
        }
        canvas.drawRoundRect(rect, cornerRadius, cornerRadius, strokePaint)
        canvas.drawText(
            label,
            rect.centerX(),
            rect.centerY() - (buttonPaint.descent() + buttonPaint.ascent()) / 2f,
            buttonPaint
        )
        if (collectLinks && url != null && links != null) {
            links.add(
                ReportLink(
                    pageIndex = pageIndex,
                    url = url,
                    left = rect.left,
                    top = rect.top,
                    right = rect.right,
                    bottom = rect.bottom
                )
            )
        }
        return top + buttonHeight
    }

    private fun headingPaintForScale(scale: Float) = TextPaint(Paint.ANTI_ALIAS_FLAG).apply {
        color = COLOR_GOLD
        textSize = 64f * scale
        typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
        textAlign = Paint.Align.CENTER
    }

    private fun bodyPaintForScale(scale: Float) = TextPaint(Paint.ANTI_ALIAS_FLAG).apply {
        color = COLOR_WHITE
        textSize = 34f * scale
        textAlign = Paint.Align.CENTER
    }

    private fun promptPaintForScale(scale: Float) = TextPaint(Paint.ANTI_ALIAS_FLAG).apply {
        color = COLOR_MUTED
        textSize = 28f * scale
        textAlign = Paint.Align.CENTER
    }

    private fun buttonPaintForScale(scale: Float) = TextPaint(Paint.ANTI_ALIAS_FLAG).apply {
        color = COLOR_GOLD
        textSize = 36f * scale
        typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
        textAlign = Paint.Align.CENTER
    }

    private fun staticLayout(text: String, paint: TextPaint, width: Int): StaticLayout =
        StaticLayout.Builder.obtain(text, 0, text.length, paint, width)
            .setAlignment(Layout.Alignment.ALIGN_CENTER)
            .setLineSpacing(0f, 1.15f)
            .setIncludePad(false)
            .build()

    private fun cardFileName(): String {
        val stamp = SimpleDateFormat("yyyyMMdd_HHmmss", Locale.US).format(Date())
        return "ExpressMobileThankYou_$stamp.jpg"
    }
}
