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
 * Google Messages does not render HTML in SMS bodies; this image replaces raw HTML links.
 */
object ThankYouCardExporter {

    private const val CARD_WIDTH = 1080
    private const val HORIZONTAL_PAD = 72f
    private const val COLOR_SURFACE = 0xFF2C1432.toInt()
    private const val COLOR_GOLD = 0xFFFFD700.toInt()
    private const val COLOR_WHITE = 0xFFFFFFFF.toInt()
    private const val COLOR_MUTED = 0xFFBDB5D5.toInt()
    private const val COLOR_BUTTON_BG = 0xFF441F4D.toInt()

    fun export(context: Context): File {
        val contentWidth = (CARD_WIDTH - HORIZONTAL_PAD * 2).toInt()
        val headingPaint = TextPaint(Paint.ANTI_ALIAS_FLAG).apply {
            color = COLOR_GOLD
            textSize = 64f
            typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
            textAlign = Paint.Align.CENTER
        }
        val bodyPaint = TextPaint(Paint.ANTI_ALIAS_FLAG).apply {
            color = COLOR_WHITE
            textSize = 34f
            textAlign = Paint.Align.CENTER
        }
        val promptPaint = TextPaint(Paint.ANTI_ALIAS_FLAG).apply {
            color = COLOR_MUTED
            textSize = 28f
            textAlign = Paint.Align.CENTER
        }
        val buttonPaint = TextPaint(Paint.ANTI_ALIAS_FLAG).apply {
            color = COLOR_GOLD
            textSize = 36f
            typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
            textAlign = Paint.Align.CENTER
        }

        val bodyLayout = staticLayout(THANK_YOU_BODY, bodyPaint, contentWidth)
        val promptLayout = staticLayout(THANK_YOU_PROMPT, promptPaint, contentWidth)

        val buttonHeight = 96f
        val buttonGap = 24f
        val sectionGap = 32f
        val cornerRadius = 28f

        var y = 56f
        y += 80f // heading baseline area
        y += bodyLayout.height + sectionGap
        y += promptLayout.height + sectionGap
        y += buttonHeight + buttonGap // PDF badge
        y += buttonHeight + buttonGap // Google review
        y += buttonHeight + 48f // Website

        val height = y.toInt().coerceAtLeast(900)
        val bitmap = Bitmap.createBitmap(CARD_WIDTH, height, Bitmap.Config.ARGB_8888)
        val canvas = Canvas(bitmap)
        canvas.drawColor(COLOR_SURFACE)

        val cardRect = RectF(24f, 24f, CARD_WIDTH - 24f, height - 24f)
        val cardPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply { color = COLOR_SURFACE }
        canvas.drawRoundRect(cardRect, 32f, 32f, cardPaint)

        y = 120f
        canvas.drawText(THANK_YOU_HEADING, CARD_WIDTH / 2f, y, headingPaint)

        y += sectionGap
        canvas.save()
        canvas.translate(HORIZONTAL_PAD, y)
        bodyLayout.draw(canvas)
        canvas.restore()
        y += bodyLayout.height + sectionGap

        canvas.save()
        canvas.translate(HORIZONTAL_PAD, y)
        promptLayout.draw(canvas)
        canvas.restore()
        y += promptLayout.height + sectionGap

        y = drawButtonRow(
            canvas = canvas,
            top = y,
            label = THANK_YOU_PDF_LABEL,
            buttonHeight = buttonHeight,
            cornerRadius = cornerRadius,
            buttonPaint = buttonPaint
        )
        y += buttonGap
        y = drawButtonRow(
            canvas = canvas,
            top = y,
            label = THANK_YOU_GOOGLE_REVIEW_LABEL,
            buttonHeight = buttonHeight,
            cornerRadius = cornerRadius,
            buttonPaint = buttonPaint
        )
        y += buttonGap
        drawButtonRow(
            canvas = canvas,
            top = y,
            label = THANK_YOU_WEBSITE_LABEL,
            buttonHeight = buttonHeight,
            cornerRadius = cornerRadius,
            buttonPaint = buttonPaint
        )

        val file = File(context.cacheDir, "reports").apply { mkdirs() }
            .resolve(cardFileName())
        FileOutputStream(file).use { out ->
            bitmap.compress(Bitmap.CompressFormat.JPEG, 92, out)
        }
        bitmap.recycle()
        return file
    }

    private fun drawButtonRow(
        canvas: Canvas,
        top: Float,
        label: String,
        buttonHeight: Float,
        cornerRadius: Float,
        buttonPaint: TextPaint
    ): Float {
        val rect = RectF(
            HORIZONTAL_PAD,
            top,
            CARD_WIDTH - HORIZONTAL_PAD,
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
        return top + buttonHeight
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
