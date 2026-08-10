package com.expressmobileservice.inspection

import android.content.Context
import java.time.LocalDate

class CalendarPreferencesStore(context: Context) {

    private val prefs = context.applicationContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

    fun getViewMode(): CalendarViewMode {
        val raw = prefs.getString(KEY_VIEW_MODE, null)
        return runCatching { CalendarViewMode.valueOf(raw!!) }
            .getOrDefault(CalendarViewMode.MONTH)
    }

    fun setViewMode(mode: CalendarViewMode) {
        prefs.edit().putString(KEY_VIEW_MODE, mode.name).apply()
    }

    fun getSelectedDate(): LocalDate? {
        if (!prefs.contains(KEY_SELECTED_EPOCH_DAY)) return null
        return LocalDate.ofEpochDay(prefs.getLong(KEY_SELECTED_EPOCH_DAY, 0L))
    }

    fun setSelectedDate(date: LocalDate) {
        prefs.edit().putLong(KEY_SELECTED_EPOCH_DAY, date.toEpochDay()).apply()
    }

    companion object {
        private const val PREFS_NAME = "express_calendar_prefs"
        private const val KEY_VIEW_MODE = "view_mode"
        private const val KEY_SELECTED_EPOCH_DAY = "selected_epoch_day"
    }
}
