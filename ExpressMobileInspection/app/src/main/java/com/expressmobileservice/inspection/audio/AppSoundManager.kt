package com.expressmobileservice.inspection.audio

import android.content.Context
import android.media.AudioAttributes
import android.media.SoundPool
import com.expressmobileservice.inspection.R

class AppSoundManager(context: Context) {

    companion object {
        @Volatile
        private var active: AppSoundManager? = null

        fun current(): AppSoundManager? = active
    }

    private val soundPool: SoundPool
    private var flyInId = 0
    private var typeId = 0
    private var buttonId = 0
    private var engineId = 0
    private var loadedCount = 0
    private var ready = false
    private var lastTypeAtMs = 0L

    init {
        val attrs = AudioAttributes.Builder()
            .setUsage(AudioAttributes.USAGE_ASSISTANCE_SONIFICATION)
            .setContentType(AudioAttributes.CONTENT_TYPE_SONIFICATION)
            .build()
        soundPool = SoundPool.Builder()
            .setMaxStreams(8)
            .setAudioAttributes(attrs)
            .build()
        soundPool.setOnLoadCompleteListener { _, _, status ->
            if (status == 0) {
                loadedCount++
                if (loadedCount >= 4) ready = true
            }
        }
        flyInId = soundPool.load(context, R.raw.fly_in_swoosh, 1)
        typeId = soundPool.load(context, R.raw.click_type, 1)
        buttonId = soundPool.load(context, R.raw.click_button, 1)
        engineId = soundPool.load(context, R.raw.engine_rev_v12, 1)
        active = this
    }

    fun playFlyIn() = play(flyInId, volume = 1f)

    fun playType() {
        val now = System.currentTimeMillis()
        if (now - lastTypeAtMs < 45) return
        lastTypeAtMs = now
        play(typeId, volume = 0.62f, rate = 1.08f)
    }

    fun playButton() = play(buttonId, volume = 0.78f, rate = 1f)

    fun playEngineRev() = play(engineId, volume = 1f)

    suspend fun awaitReady(timeoutMs: Long = 2500L) {
        val start = System.currentTimeMillis()
        while (!ready && System.currentTimeMillis() - start < timeoutMs) {
            kotlinx.coroutines.delay(50)
        }
    }

    private fun play(soundId: Int, volume: Float, rate: Float = 1f) {
        if (!ready || soundId == 0) return
        soundPool.play(soundId, volume, volume, 1, 0, rate)
    }

    fun release() {
        if (active === this) active = null
        soundPool.release()
    }
}
