package com.expressmobileservice.inspection.audio

import androidx.compose.runtime.staticCompositionLocalOf
import com.expressmobileservice.inspection.audio.AppSoundManager.Companion.current

val LocalAppSoundManager = staticCompositionLocalOf<AppSoundManager?> { null }

fun appSound(): AppSoundManager? = current()
