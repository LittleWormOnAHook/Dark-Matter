package com.expressmobileservice.inspection

import java.time.DayOfWeek
import java.time.Instant
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.LocalTime
import java.time.YearMonth
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.time.temporal.TemporalAdjusters
import java.time.temporal.WeekFields
import java.util.Locale

private val locale = Locale.getDefault()
private val monthYearFormatter = DateTimeFormatter.ofPattern("MMMM yyyy", locale)
private val dayHeaderFormatter = DateTimeFormatter.ofPattern("EEE, MMM d", locale)
private val timeFormatter = DateTimeFormatter.ofPattern("h:mm a", locale)
private val monthAbbrevFormatter = DateTimeFormatter.ofPattern("MMM", locale)

fun LocalDate.toEpochMillisAtStartOfDay(zone: ZoneId = ZoneId.systemDefault()): Long =
    atStartOfDay(zone).toInstant().toEpochMilli()

fun Long.toLocalDate(zone: ZoneId = ZoneId.systemDefault()): LocalDate =
    Instant.ofEpochMilli(this).atZone(zone).toLocalDate()

fun Long.toLocalDateTime(zone: ZoneId = ZoneId.systemDefault()): LocalDateTime =
    Instant.ofEpochMilli(this).atZone(zone).toLocalDateTime()

fun formatMonthYear(yearMonth: YearMonth): String = yearMonth.format(monthYearFormatter)

fun formatDayHeader(date: LocalDate): String = date.format(dayHeaderFormatter)

fun formatTime(millis: Long): String = millis.toLocalDateTime().format(timeFormatter)

fun formatTimeRange(startMillis: Long, endMillis: Long, allDay: Boolean): String {
    if (allDay) return "All day"
    return "${formatTime(startMillis)} – ${formatTime(endMillis)}"
}

fun formatMonthAbbrev(date: LocalDate): String = date.format(monthAbbrevFormatter)

fun daysInMonthGrid(yearMonth: YearMonth, firstDayOfWeek: DayOfWeek = DayOfWeek.SUNDAY): List<LocalDate> {
    val firstOfMonth = yearMonth.atDay(1)
    val startOffset = ((firstOfMonth.dayOfWeek.value - firstDayOfWeek.value + 7) % 7)
    val gridStart = firstOfMonth.minusDays(startOffset.toLong())
    return (0 until 42).map { gridStart.plusDays(it.toLong()) }
}

fun weekDaysContaining(date: LocalDate, firstDayOfWeek: DayOfWeek = DayOfWeek.SUNDAY): List<LocalDate> {
    val start = date.with(TemporalAdjusters.previousOrSame(firstDayOfWeek))
    return (0 until 7).map { start.plusDays(it.toLong()) }
}

fun defaultAppointmentStart(date: LocalDate = LocalDate.now()): Long {
    val nextHour = LocalDateTime.now().plusHours(1).withMinute(0).withSecond(0).withNano(0)
    val start = date.atTime(nextHour.toLocalTime())
    return start.atZone(ZoneId.systemDefault()).toInstant().toEpochMilli()
}

fun defaultAppointmentEnd(startMillis: Long): Long {
    val start = startMillis.toLocalDateTime()
    val end = start.plusHours(1)
    return end.atZone(ZoneId.systemDefault()).toInstant().toEpochMilli()
}

/** When start moves, keep end on the same day, one hour after start. */
fun syncEndAfterStartChange(startMillis: Long): Long = defaultAppointmentEnd(startMillis)

fun appointmentOverlapsDay(appointment: Appointment, day: LocalDate, zone: ZoneId = ZoneId.systemDefault()): Boolean {
    val dayStart = day.atStartOfDay(zone).toInstant().toEpochMilli()
    val dayEnd = day.plusDays(1).atStartOfDay(zone).toInstant().toEpochMilli()
    return appointment.startEpochMillis < dayEnd && appointment.endEpochMillis > dayStart
}

fun appointmentsForDay(appointments: List<Appointment>, day: LocalDate): List<Appointment> =
    appointments.filter { appointmentOverlapsDay(it, day) }.sortedBy { it.startEpochMillis }

fun appointmentsForWeek(appointments: List<Appointment>, anchorDate: LocalDate): List<Appointment> {
    val days = weekDaysContaining(anchorDate)
    val start = days.first().toEpochMillisAtStartOfDay()
    val end = days.last().plusDays(1).toEpochMillisAtStartOfDay()
    return appointments.filter { it.startEpochMillis < end && it.endEpochMillis > start }
        .sortedBy { it.startEpochMillis }
}

fun roundToNearestMinutes(millis: Long, minutes: Int = 15): Long {
    val dt = millis.toLocalDateTime()
    val totalMinutes = dt.hour * 60 + dt.minute
    val rounded = ((totalMinutes + minutes / 2) / minutes) * minutes
    val hour = rounded / 60
    val minute = rounded % 60
    return dt.withHour(hour.coerceAtMost(23)).withMinute(minute.coerceAtMost(59))
        .withSecond(0).withNano(0)
        .atZone(ZoneId.systemDefault()).toInstant().toEpochMilli()
}

fun combineDateAndTime(date: LocalDate, time: LocalTime, zone: ZoneId = ZoneId.systemDefault()): Long =
    date.atTime(time).atZone(zone).toInstant().toEpochMilli()

fun weekNumber(date: LocalDate): Int =
    date.get(WeekFields.of(locale).weekOfWeekBasedYear())
