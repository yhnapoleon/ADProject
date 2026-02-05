package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.content.Context
import android.graphics.Color
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.view.View
import android.widget.Button
import android.widget.ProgressBar
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.airbnb.lottie.LottieAnimationView
import com.google.android.material.appbar.MaterialToolbar
import iss.nus.edu.sg.sharedprefs.admobile.R
import iss.nus.edu.sg.sharedprefs.admobile.data.model.PostTreeRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.network.NetworkClient
import kotlinx.coroutines.launch
import java.util.Calendar

class TreePlantingActivity : AppCompatActivity() {

    private lateinit var lottieBg: LottieAnimationView
    private lateinit var lottieSwitch: LottieAnimationView
    private lateinit var lottiePlant: LottieAnimationView
    private lateinit var treeProgress: ProgressBar
    private lateinit var tvTodaySteps: TextView
    private lateinit var tvAvailableSteps: TextView
    private lateinit var tvCarbonImpact: TextView
    private lateinit var tvPlantedCount: TextView
    private lateinit var btnConvert: Button
    private lateinit var tvFloatTip: TextView

    private var isNightMode = false

    // 🌟 将默认值全部设为 0
    private var todaySteps = 0
    private var availableSteps = 0
    private var currentTreeGrowth = 0
    private var totalPlantedTrees = 0

    private var isCelebrating = false
    private lateinit var lottieCelebration: LottieAnimationView

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_tree_planting)

        window.statusBarColor = Color.WHITE
        window.decorView.systemUiVisibility = View.SYSTEM_UI_FLAG_LIGHT_STATUS_BAR

        initViews()

        // 🌟 初始化时禁用按钮，直到数据加载完成
        btnConvert.isEnabled = false

        isNightMode = Calendar.getInstance().get(Calendar.HOUR_OF_DAY).let { it < 6 || it >= 18 }
        initThemeState()

        // 🌟 初始获取数据，fetchTreeData 内部会调用 refreshUI
        fetchTreeData()

        btnConvert.setOnClickListener {
            handleStepConversion()
        }

        lottieSwitch.setOnClickListener {
            isNightMode = !isNightMode
            performThemeSwitch()
        }

        lottieCelebration = findViewById(R.id.lottie_celebration)
    }

    private fun initViews() {
        val toolbar = findViewById<MaterialToolbar>(R.id.tree_toolbar)
        toolbar.setNavigationOnClickListener { onBackPressed() }

        lottieBg = findViewById(R.id.lottie_background)
        lottieSwitch = findViewById(R.id.lottie_day_night_switch)
        lottiePlant = findViewById(R.id.lottie_plant)
        treeProgress = findViewById(R.id.tree_progress)
        tvTodaySteps = findViewById(R.id.tv_today_steps)
        tvAvailableSteps = findViewById(R.id.tv_available_steps)
        tvCarbonImpact = findViewById(R.id.tv_carbon_impact_text)
        tvPlantedCount = findViewById(R.id.tv_trees_planted_count)
        btnConvert = findViewById(R.id.btn_convert_steps)
        tvFloatTip = findViewById(R.id.tv_float_tip)
    }

    private fun fetchTreeData() {
        lifecycleScope.launch {
            try {
                val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
                val token = "Bearer ${prefs.getString("access_token", "")}"

                val response = NetworkClient.apiService.getTreeData(token)
                if (response.isSuccessful && response.body() != null) {
                    val data = response.body()!!
                    todaySteps = data.todaySteps
                    availableSteps = data.availableSteps
                    currentTreeGrowth = data.currentProgress
                    totalPlantedTrees = data.totalTrees

                    // 🌟 只有成功拿到数据后才启用按钮并刷新 UI
                    btnConvert.isEnabled = true
                    refreshUI()
                }
            } catch (e: Exception) {
                tvTodaySteps.text = "Sync Failed"
                Toast.makeText(this@TreePlantingActivity, "Failed to sync tree data", Toast.LENGTH_SHORT).show()
            }
        }
    }

    private fun handleStepConversion() {
        if (isCelebrating) return

        if (availableSteps > 0) {
            val growthGain = availableSteps / 150
            val totalPotential = currentTreeGrowth + growthGain
            val usedStepsThisTime = availableSteps

            lifecycleScope.launch {
                try {
                    val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
                    val token = "Bearer ${prefs.getString("access_token", "")}"

                    val newTotalTrees = if (totalPotential >= 100) totalPlantedTrees + (totalPotential / 100) else totalPlantedTrees
                    val newProgress = totalPotential % 100

                    val request = PostTreeRequest(newTotalTrees, newProgress, usedStepsThisTime)
                    val response = NetworkClient.apiService.postTreeData(token, request)

                    if (response.isSuccessful) {
                        performGrowthAnimation(growthGain, totalPotential)
                        fetchTreeData()
                    }
                } catch (e: Exception) {
                    showAtTreeTop("Network error!")
                }
            }
        } else {
            showAtTreeTop("No steps to convert!")
        }
    }

    private fun performGrowthAnimation(gain: Int, potential: Int) {
        lottiePlant.animate().scaleX(1.1f).scaleY(1.1f).setDuration(150).withEndAction {
            lottiePlant.animate().scaleX(1.0f).scaleY(1.0f).setDuration(150).start()
        }.start()

        if (potential >= 100) {
            val leftover = potential % 100
            currentTreeGrowth = 100
            refreshUI()
            startCelebration(leftover)
        } else {
            currentTreeGrowth = potential
            showAtTreeTop("Growth +$gain%")
            refreshUI()
        }
    }

    private fun startCelebration(leftover: Int) {
        isCelebrating = true
        btnConvert.isEnabled = false
        lottieCelebration.visibility = View.VISIBLE
        lottieCelebration.playAnimation()
        showAtTreeTop("Congratulations! New tree planted! 🎉")

        Handler(Looper.getMainLooper()).postDelayed({
            resetToNewTree(leftover)
        }, 3000)
    }

    private fun resetToNewTree(leftover: Int) {
        currentTreeGrowth = leftover
        isCelebrating = false
        btnConvert.isEnabled = true
        lottieCelebration.cancelAnimation()
        lottieCelebration.visibility = View.GONE
        refreshUI()
    }

    private fun showAtTreeTop(message: String) {
        tvFloatTip.text = message
        tvFloatTip.visibility = View.VISIBLE
        tvFloatTip.alpha = 1.0f
        tvFloatTip.animate().cancel()
        val displayDuration = if (isCelebrating) 2800L else 2000L

        Handler(Looper.getMainLooper()).postDelayed({
            tvFloatTip.animate().alpha(0.0f).setDuration(500).withEndAction { tvFloatTip.visibility = View.GONE }.start()
        }, displayDuration)
    }

    private fun refreshUI() {
        treeProgress.progress = currentTreeGrowth
        tvTodaySteps.text = "Today's Total Steps: $todaySteps"
        tvAvailableSteps.text = "Available Steps: $availableSteps"
        tvPlantedCount.text = "Trees: $totalPlantedTrees"

        // 🌟 动态生成文案，如果没有树则显示空提示
        if (totalPlantedTrees > 0) {
            tvCarbonImpact.text = "Your walking equivalent: $totalPlantedTrees trees planted!"
        } else {
            tvCarbonImpact.text = "Start walking to grow your first tree!"
        }

        val stage = when {
            currentTreeGrowth < 17 -> 1
            currentTreeGrowth < 34 -> 2
            currentTreeGrowth < 51 -> 3
            currentTreeGrowth < 68 -> 4
            currentTreeGrowth < 85 -> 5
            else -> 6
        }
        updatePlantStage(stage)
    }

    private fun updatePlantStage(stage: Int) {
        val res = when (stage) {
            1 -> R.raw.plant1
            2 -> R.raw.plant2
            3 -> R.raw.plant3
            4 -> R.raw.plant4
            5 -> R.raw.plant5
            else -> R.raw.plant6
        }
        if (lottiePlant.tag != res) {
            lottiePlant.setAnimation(res)
            lottiePlant.playAnimation()
            lottiePlant.tag = res
        }
    }

    private fun initThemeState() {
        if (isNightMode) {
            lottieBg.setAnimation(R.raw.background_night)
            lottieSwitch.progress = 0.5f
        } else {
            lottieBg.setAnimation(R.raw.background_day)
            lottieSwitch.progress = 1.0f
        }
        lottieBg.playAnimation()
    }

    private fun performThemeSwitch() {
        if (isNightMode) {
            lottieSwitch.setMinAndMaxProgress(0f, 0.5f)
            lottieBg.setAnimation(R.raw.background_night)
        } else {
            lottieSwitch.setMinAndMaxProgress(0.5f, 1.0f)
            lottieBg.setAnimation(R.raw.background_day)
        }
        lottieSwitch.playAnimation()
        lottieBg.playAnimation()
    }
}