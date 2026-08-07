package com.expressmobileservice.inspection

import android.content.Context

class ThemePreferences(context: Context) {
    private val prefs = context.applicationContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

    fun isDarkMode(): Boolean = prefs.getBoolean(KEY_DARK_MODE, false)

    fun setDarkMode(enabled: Boolean) {
        prefs.edit().putBoolean(KEY_DARK_MODE, enabled).apply()
    }

    private companion object {
        const val PREFS_NAME = "express_mobile_inspection_theme"
        const val KEY_DARK_MODE = "dark_mode"
    }
}
