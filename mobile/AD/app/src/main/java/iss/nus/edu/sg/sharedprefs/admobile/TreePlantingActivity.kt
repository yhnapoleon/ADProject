package iss.nus.edu.sg.sharedprefs.admobile

import android.graphics.Color
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.view.View
import android.widget.Button
import android.widget.ProgressBar
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import com.airbnb.lottie.LottieAnimationView
import com.google.android.material.appbar.MaterialToolbar
import java.util.*

class TreePlantingActivity : AppCompatActivity() {

    private lateinit var lottieBg: LottieAnimationView
    private lateinit var lottieSwitch: LottieAnimationView
    private lateinit var lottiePlant: LottieAnimationView
    private lateinit var treeProgress: ProgressBar
    private lateinit var tvTodaySteps: TextView
    private lateinit var tvCarbonImpact: TextView
    private lateinit var tvPlantedCount: TextView
    private lateinit var btnConvert: Button
    private lateinit var tvFloatTip: TextView

    private var isNightMode = false
    private var todaySteps = 11277
    private var currentTreeGrowth = 35
    private var totalPlantedTrees = 5

    // 🌟 新增：标记是否正在执行庆祝动画，防止重置前被干扰
    private var isCelebrating = false

    private lateinit var lottieCelebration: LottieAnimationView

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_tree_planting)

        window.statusBarColor = Color.WHITE
        window.decorView.systemUiVisibility = View.SYSTEM_UI_FLAG_LIGHT_STATUS_BAR

        initViews()

        isNightMode = Calendar.getInstance().get(Calendar.HOUR_OF_DAY).let { it < 6 || it >= 18 }
        initThemeState()
        refreshUI()

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
        tvCarbonImpact = findViewById(R.id.tv_carbon_impact_text)
        tvPlantedCount = findViewById(R.id.tv_trees_planted_count)
        btnConvert = findViewById(R.id.btn_convert_steps)
        tvFloatTip = findViewById(R.id.tv_float_tip)
    }

    private fun handleStepConversion() {
        if (isCelebrating) return

        if (todaySteps > 0) {
            val growthGain = todaySteps / 150
            // 1. 计算当前进度 + 增量的总和
            val totalPotential = currentTreeGrowth + growthGain
            todaySteps = 0

            // 树木互动动画
            lottiePlant.animate().scaleX(1.1f).scaleY(1.1f).setDuration(150).withEndAction {
                lottiePlant.animate().scaleX(1.0f).scaleY(1.0f).setDuration(150).start()
            }.start()

            if (totalPotential >= 100) {
                // 2. 🌟 核心：计算种成了几棵树，以及剩下多少进度给下一棵
                val treesPlantedThisTime = totalPotential / 100
                val leftoverGrowth = totalPotential % 100

                totalPlantedTrees += treesPlantedThisTime

                // 先显示当前这棵树为成树状态（100%）
                currentTreeGrowth = 100
                refreshUI()

                // 3. 传入剩余进度，开始庆祝
                startCelebration(leftoverGrowth)
            } else {
                currentTreeGrowth = totalPotential
                showAtTreeTop("Growth +$growthGain%")
                refreshUI()
            }
        } else {
            showAtTreeTop("No steps to convert!")
        }
    }

    /**
     * 🌟 庆祝阶段：显示成树 3 秒，展示庆祝语，之后重置
     */
    private fun startCelebration(leftover: Int) {
        isCelebrating = true
        btnConvert.isEnabled = false

        lottieCelebration.visibility = View.VISIBLE
        lottieCelebration.playAnimation()

        showAtTreeTop("Congratulations! New tree planted! 🎉")

        // 延迟 3 秒：展现成树和礼花
        Handler(Looper.getMainLooper()).postDelayed({
            resetToNewTree(leftover) // 🌟 传入剩余进度
        }, 3000)
    }

    /**
     * 🌟 重置阶段：清空进度，更新 UI 回到幼苗状态
     */
    private fun resetToNewTree(leftover: Int) {
        currentTreeGrowth = leftover // 🌟 新树的起始进度
        isCelebrating = false
        btnConvert.isEnabled = true

        lottieCelebration.cancelAnimation()
        lottieCelebration.visibility = View.GONE

        refreshUI()

        if (leftover > 0) {
            showAtTreeTop("New tree starts with $leftover%!")
        } else {
            showAtTreeTop("Let's grow a new one!")
        }
    }

    private fun showAtTreeTop(message: String) {
        tvFloatTip.text = message
        tvFloatTip.visibility = View.VISIBLE
        tvFloatTip.alpha = 1.0f

        tvFloatTip.animate().cancel()

        // 如果是庆祝语，我们让它停久一点，不要被自动淡出覆盖
        val displayDuration = if (isCelebrating) 2800L else 2000L

        Handler(Looper.getMainLooper()).postDelayed({
            tvFloatTip.animate()
                .alpha(0.0f)
                .setDuration(500)
                .withEndAction { tvFloatTip.visibility = View.GONE }
                .start()
        }, displayDuration)
    }

    private fun refreshUI() {
        treeProgress.progress = currentTreeGrowth
        tvTodaySteps.text = "Today's Steps: $todaySteps"
        tvPlantedCount.text = "Trees: $totalPlantedTrees"
        tvCarbonImpact.text = "Your carbon reduction from walking is equivalent to planting $totalPlantedTrees trees for the Earth."

        val calculatedStage = when {
            currentTreeGrowth <= 0 -> 1 // 🌟 刚重置
            currentTreeGrowth < 17 -> 1
            currentTreeGrowth < 34 -> 2
            currentTreeGrowth < 51 -> 3
            currentTreeGrowth < 68 -> 4
            currentTreeGrowth < 85 -> 5
            else -> 6 // 100% 状态
        }
        updatePlantStage(calculatedStage)
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

    // ... 原有的 initThemeState 和 performThemeSwitch 保持不变 ...
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