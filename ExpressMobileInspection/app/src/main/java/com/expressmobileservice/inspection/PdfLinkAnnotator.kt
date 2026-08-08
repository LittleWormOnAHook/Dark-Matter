package com.expressmobileservice.inspection

import com.tom_roush.pdfbox.android.PDFBoxResourceLoader
import com.tom_roush.pdfbox.pdmodel.PDDocument
import com.tom_roush.pdfbox.pdmodel.common.PDRectangle
import com.tom_roush.pdfbox.pdmodel.interactive.action.PDActionURI
import com.tom_roush.pdfbox.pdmodel.interactive.annotation.PDAnnotationLink
import com.tom_roush.pdfbox.pdmodel.interactive.annotation.PDBorderStyleDictionary
import java.io.File

data class ReportLink(
    val pageIndex: Int,
    val url: String,
    val left: Float,
    val top: Float,
    val right: Float,
    val bottom: Float
)

object PdfLinkAnnotator {

    fun init(context: android.content.Context) {
        PDFBoxResourceLoader.init(context.applicationContext)
    }

    fun annotate(pdfFile: File, links: List<ReportLink>, pageHeight: Int) {
        if (links.isEmpty()) return
        val document = PDDocument.load(pdfFile)
        try {
            links.forEach { link ->
                if (link.pageIndex < 0 || link.pageIndex >= document.numberOfPages) return@forEach
                val page = document.getPage(link.pageIndex)
                val annotation = PDAnnotationLink()
                val pdfBottom = pageHeight - link.bottom
                val pdfTop = pageHeight - link.top
                val width = link.right - link.left
                val height = pdfTop - pdfBottom
                annotation.rectangle = PDRectangle(link.left, pdfBottom, width, height)
                val action = PDActionURI()
                action.uri = link.url
                annotation.action = action
                val border = PDBorderStyleDictionary()
                border.width = 0f
                annotation.borderStyle = border
                page.annotations.add(annotation)
            }
            document.save(pdfFile)
        } finally {
            document.close()
        }
    }
}
