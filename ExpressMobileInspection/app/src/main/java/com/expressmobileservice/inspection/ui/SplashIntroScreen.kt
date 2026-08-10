package com.expressmobileservice.inspection.ui

import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.unit.dp
import com.expressmobileservice.inspection.audio.appSound
import com.expressmobileservice.inspection.ui.theme.SamsungCalendarColors
import kotlin.math.cos
import kotlin.math.sin
import kotlin.random.Random
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

private data class SparkParticle(
    val angle: Float,
    val speed: Float,
    val size: Float,
    val hue: Float
)

@Composable
fun SplashIntroScreen(
    onFinished: () -> Unit,
    modifier: Modifier = Modifier
) {
    val offsetY = remember { Animatable(-420f) }
    val scale = remember { Animatable(0.25f) }
    val alpha = remember { Animatable(0f) }
    val sparkProgress = remember { Animatable(0f) }
    val sparks = remember {
        List(48) {
            SparkParticle(
                angle = Random.nextFloat() * 360f,
                speed = 80f + Random.nextFloat() * 220f,
                size = 2f + Random.nextFloat() * 5f,
                hue = Random.nextFloat()
            )
        }
    }

    LaunchedEffect(Unit) {
        appSound()?.awaitReady()
        appSound()?.playFlyIn()
        coroutineScope {
            launch { offsetY.animateTo(0f, tween(900, easing = FastOutSlowInEasing)) }
            launch { scale.animateTo(1f, tween(900, easing = FastOutSlowInEasing)) }
            launch { alpha.animateTo(1f, tween(500)) }
            launch { sparkProgress.animateTo(1f, tween(1100, easing = FastOutSlowInEasing)) }
        }
        delay(1400)
        onFinished()
    }

    Box(
        modifier = modifier
            .fillMaxSize()
            .background(SamsungCalendarColors.background),
        contentAlignment = Alignment.Center
    ) {
        Canvas(modifier = Modifier.fillMaxSize()) {
            val center = Offset(size.width / 2f, size.height / 2f)
            val progress = sparkProgress.value
            sparks.forEach { spark ->
                val rad = Math.toRadians(spark.angle.toDouble()).toFloat()
                val dist = spark.speed * progress
                val x = center.x + cos(rad) * dist
                val y = center.y + sin(rad) * dist
                val particleAlpha = (1f - progress).coerceIn(0f, 1f)
                val goldMix = if (spark.hue > 0.5f) {
                    SamsungCalendarColors.metallicGold
                } else {
                    SamsungCalendarColors.orchidPurple
                }
                drawCircle(
                    color = goldMix.copy(alpha = particleAlpha * 0.9f),
                    radius = spark.size * (1f - progress * 0.4f),
                    center = Offset(x, y)
                )
            }
            if (progress < 0.85f) {
                drawCircle(
                    color = SamsungCalendarColors.metallicGold.copy(alpha = 0.35f * (1f - progress)),
                    radius = 40f + progress * 120f,
                    center = center,
                    style = Stroke(width = 2f)
                )
            }
        }

        Box(
            modifier = Modifier
                .graphicsLayer {
                    translationY = offsetY.value
                    scaleX = scale.value
                    scaleY = scale.value
                    this.alpha = alpha.value
                }
        ) {
            ExpressMobileLogo()
        }
    }
}
