package com.expressmobileservice.inspection.ui

import com.expressmobileservice.inspection.audio.appSound

fun playButtonClick(mainAction: Boolean = false) {
    val sound = appSound() ?: return
    if (mainAction) sound.playEngineRev() else sound.playButton()
}

fun playTypeClick() {
    appSound()?.playType()
}
