package iss.nus.edu.sg.sharedprefs.admobile.utils

import android.content.Context
import android.hardware.Sensor
import android.hardware.SensorEvent
import android.hardware.SensorEventListener
import android.hardware.SensorManager
import android.util.Log

class StepCounterManager(context: Context) : SensorEventListener {

    private val sensorManager = context.getSystemService(Context.SENSOR_SERVICE) as SensorManager
    private val stepSensor = sensorManager.getDefaultSensor(Sensor.TYPE_STEP_COUNTER)
    private val prefs = context.getSharedPreferences("step_prefs", Context.MODE_PRIVATE)

    private var onStepsDetected: ((Int) -> Unit)? = null

    fun startListening(callback: (Int) -> Unit) {
        onStepsDetected = callback
        stepSensor?.let {
            sensorManager.registerListener(this, it, SensorManager.SENSOR_DELAY_UI)
        }
    }

    override fun onSensorChanged(event: SensorEvent) {
        if (event.sensor.type == Sensor.TYPE_STEP_COUNTER) {
            val totalStepsSinceBoot = event.values[0].toInt()

            // 🌟 逻辑关键：计算今日步数
            // 今日步数 = 传感器当前值 - 今日凌晨时传感器的读数
            val todaySteps = calculateTodaySteps(totalStepsSinceBoot)

            onStepsDetected?.invoke(todaySteps)

            // 获取一次后即停止监听，节省电量
            sensorManager.unregisterListener(this)
        }
    }

    private fun calculateTodaySteps(totalSteps: Int): Int {
        val lastStoredTotal = prefs.getInt("last_total_steps", -1)

        // 如果是当天第一次获取，或者传感器数值由于重启变小了
        if (lastStoredTotal == -1 || totalSteps < lastStoredTotal) {
            prefs.edit().putInt("last_total_steps", totalSteps).apply()
            return 0
        }

        return totalSteps - lastStoredTotal
    }

    override fun onAccuracyChanged(sensor: Sensor?, accuracy: Int) {}
}