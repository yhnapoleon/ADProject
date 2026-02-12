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
        btnConvert.isEnabled = false

        isNightMode = Calendar.getInstance().get(Calendar.HOUR_OF_DAY).let { it < 6 || it >= 18 }
        initThemeState()

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

                    btnConvert.isEnabled = true
                    // 如果当前正在播放庆祝动画，我们不在此处立即刷新 UI，防止进度条突变
                    if (!isCelebrating) refreshUI()
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

                    // 计算总共增加了多少棵树以及剩余进度
                    val treesAdded = totalPotential / 100
                    val leftoverProgress = totalPotential % 100
                    val newTotalTrees = totalPlantedTrees + treesAdded

                    val request = PostTreeRequest(newTotalTrees, leftoverProgress, usedStepsThisTime)
                    val response = NetworkClient.apiService.postTreeData(token, request)

                    if (response.isSuccessful) {
                        // 执行统一的动画逻辑
                        performGrowthAnimation(growthGain, totalPotential)
                        // 动画开始后同步后端数据，但在动画结束前 UI 不会因为 fetchTreeData 而突变
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
        // 植物抖动反馈
        lottiePlant.animate().scaleX(1.1f).scaleY(1.1f).setDuration(150).withEndAction {
            lottiePlant.animate().scaleX(1.0f).scaleY(1.0f).setDuration(150).start()
        }.start()

        val treesAdded = potential / 100
        val leftover = potential % 100

        if (treesAdded > 0) {
            // 只要有树成熟，将进度条拉满并播放一次庆祝
            currentTreeGrowth = 100
            refreshUI()
            startCelebration(leftover, treesAdded)
        } else {
            // 普通成长
            currentTreeGrowth = leftover
            showAtTreeTop("Growth +$gain%")
            refreshUI()
        }
    }

    private fun startCelebration(leftover: Int, treesCount: Int) {
        isCelebrating = true
        btnConvert.isEnabled = false
        lottieCelebration.visibility = View.VISIBLE
        lottieCelebration.playAnimation()

        // 根据种树数量适配文案
        val message = if (treesCount > 1) {
            "Amazing! $treesCount new trees planted! 🌳🎉"
        } else {
            "Congratulations! New tree planted! 🎉"
        }
        showAtTreeTop(message)

        // 3秒后重置状态到最终余数进度
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
        // 动画结束，恢复到真实的最新进度和总数
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