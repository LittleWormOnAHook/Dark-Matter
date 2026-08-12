package com.expressmobileservice.inspection.ui

import android.content.Context
import android.media.AudioAttributes
import android.media.SoundPool
import com.expressmobileservice.inspection.R

object ExpressUiSounds {
    private var soundPool: SoundPool? = null
    private var impactId = 0
    private var anchorId = 0
    private var initialized = false

    fun init(context: Context) {
        if (initialized) return
        val attrs = AudioAttributes.Builder()
            .setUsage(AudioAttributes.USAGE_ASSISTANCE_SONIFICATION)
            .setContentType(AudioAttributes.CONTENT_TYPE_SONIFICATION)
            .build()
        soundPool = SoundPool.Builder()
            .setMaxStreams(6)
            .setAudioAttributes(attrs)
            .build()
        impactId = soundPool!!.load(context, R.raw.ui_impact_click, 1)
        anchorId = soundPool!!.load(context, R.raw.ui_anchor_v8_rev, 1)
        initialized = true
    }

    fun playImpact() {
        soundPool?.play(impactId, 0.75f, 0.75f, 1, 0, 1f)
    }

    fun playAnchor() {
        soundPool?.play(anchorId, 1f, 1f, 1, 0, 1f)
    }

  /** Mechanic air-impact tick for typing and small utility taps. */
    fun onTypingValueChange(previous: String, updated: String, onValueChange: (String) -> Unit) {
        if (updated.length > previous.length) {
            playImpact()
        }
        onValueChange(updated)
    }

    fun withImpact(onClick: () -> Unit): () -> Unit = {
        playImpact()
        onClick()
    }

    /** Loud V8 exhaust rev for primary / anchor actions (tabs, save, send). */
    fun withAnchor(onClick: () -> Unit): () -> Unit = {
        playAnchor()
        onClick()
    }
}
