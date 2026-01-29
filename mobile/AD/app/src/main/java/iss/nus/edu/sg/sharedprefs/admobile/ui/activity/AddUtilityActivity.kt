package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.Manifest
import android.app.DatePickerDialog
import android.content.pm.PackageManager
import android.graphics.Bitmap
import android.net.Uri
import android.os.Bundle
import android.text.Editable
import android.text.TextWatcher
import android.view.View
import android.widget.*
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import com.google.android.material.appbar.MaterialToolbar
import com.google.android.material.card.MaterialCardView
import iss.nus.edu.sg.sharedprefs.admobile.R
import java.text.SimpleDateFormat
import java.util.*

class AddUtilityActivity : AppCompatActivity() {

    private val ELEC_FACTOR = 0.4085
    private val WATER_FACTOR = 0.191
    private var selectedDate = Calendar.getInstance()

    // 🌟 新增：图片预览和占位布局变量
    private lateinit var ivBillPreview: ImageView
    private lateinit var llPlaceholder: LinearLayout

    // 🌟 启动器：从相册选择图片
    private val pickImageLauncher = registerForActivityResult(ActivityResultContracts.GetContent()) { uri: Uri? ->
        uri?.let {
            ivBillPreview.setImageURI(it)
            showImagePreview()
        }
    }

    // 🌟 启动器：拍照获取缩略图
    private val takePictureLauncher = registerForActivityResult(ActivityResultContracts.TakePicturePreview()) { bitmap: Bitmap? ->
        bitmap?.let {
            ivBillPreview.setImageBitmap(it)
            showImagePreview()
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.add_utility_activity)

        // 视图绑定
        val toolbar: MaterialToolbar = findViewById(R.id.toolbar)
        val cardScan: MaterialCardView = findViewById(R.id.card_scan_bill)
        val etMonth: EditText = findViewById(R.id.et_month)
        val etElectricity: EditText = findViewById(R.id.et_electricity)
        val etWater: EditText = findViewById(R.id.et_water)
        val etNotes: EditText = findViewById(R.id.et_notes)
        val tvElecCarbon: TextView = findViewById(R.id.tv_electricity_carbon)
        val tvWaterCarbon: TextView = findViewById(R.id.tv_water_carbon)
        val btnSave: Button = findViewById(R.id.save_button)

        // 🌟 初始化新增的图片视图
        ivBillPreview = findViewById(R.id.iv_bill_preview)
        llPlaceholder = findViewById(R.id.ll_scan_placeholder)

        setSupportActionBar(toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        toolbar.setNavigationOnClickListener { onBackPressedDispatcher.onBackPressed() }

        // --- 1. 图片/扫码点击逻辑 ---
        cardScan.setOnClickListener {
            showImageSourceDialog()
        }

        // --- 2. 月份选择逻辑 (仅显示年月) ---
        val sdf = SimpleDateFormat("MMMM yyyy", Locale.getDefault())
        etMonth.setText(sdf.format(selectedDate.time))

        etMonth.setOnClickListener {
            val dpd = DatePickerDialog(this, { _, year, month, _ ->
                selectedDate.set(Calendar.YEAR, year)
                selectedDate.set(Calendar.MONTH, month)
                etMonth.setText(sdf.format(selectedDate.time))
            }, selectedDate.get(Calendar.YEAR), selectedDate.get(Calendar.MONTH), selectedDate.get(Calendar.DAY_OF_MONTH))

            try {
                dpd.datePicker.calendarViewShown = false
                val daySpinnerId = resources.getIdentifier("day", "id", "android")
                if (daySpinnerId != 0) {
                    val daySpinner = dpd.datePicker.findViewById<View>(daySpinnerId)
                    daySpinner?.visibility = View.GONE
                }
            } catch (e: Exception) {
                e.printStackTrace()
            }
            dpd.show()
        }

        // --- 3. 实时计算逻辑 ---
        etElectricity.addTextChangedListener(createWatcher { s ->
            calculateAndShowCarbon(s, ELEC_FACTOR, tvElecCarbon)
        })

        etWater.addTextChangedListener(createWatcher { s ->
            calculateAndShowCarbon(s, WATER_FACTOR, tvWaterCarbon)
        })

        // --- 4. 保存逻辑 ---
        btnSave.setOnClickListener {
            val elecValue = etElectricity.text.toString()
            val waterValue = etWater.text.toString()

            if (elecValue.isNotEmpty() || waterValue.isNotEmpty()) {
                Toast.makeText(this, "Record saved for ${etMonth.text}", Toast.LENGTH_SHORT).show()
                finish()
            } else {
                Toast.makeText(this, "Please enter usage", Toast.LENGTH_SHORT).show()
            }
        }
    }

    // 🌟 弹出对话框选择图片来源
    private fun showImageSourceDialog() {
        val options = arrayOf("Take Photo", "Choose from Gallery")
        AlertDialog.Builder(this)
            .setTitle("Upload Utility Bill")
            .setItems(options) { _, which ->
                when (which) {
                    0 -> checkCameraPermissionAndLaunch()
                    1 -> pickImageLauncher.launch("image/*")
                }
            }
            .show()
    }

    private fun checkCameraPermissionAndLaunch() {
        if (checkSelfPermission(Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED) {
            takePictureLauncher.launch(null)
        } else {
            requestPermissionLauncher.launch(Manifest.permission.CAMERA)
        }
    }

    // 🌟 权限请求回调
    private val requestPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { isGranted ->
        if (isGranted) {
            takePictureLauncher.launch(null)
        } else {
            Toast.makeText(this, "Camera permission denied", Toast.LENGTH_SHORT).show()
        }
    }

    // 🌟 显示图片预览并隐藏占位符，模拟自动识别数据
    private fun showImagePreview() {
        ivBillPreview.visibility = View.VISIBLE
        llPlaceholder.visibility = View.GONE

        // 模拟 OCR 自动识别效果
        Toast.makeText(this, "Bill uploaded! Auto-filling usage...", Toast.LENGTH_SHORT).show()
        findViewById<EditText>(R.id.et_electricity).setText("145.2")
        findViewById<EditText>(R.id.et_water).setText("12.5")
    }

    private fun createWatcher(onChanged: (String) -> Unit) = object : TextWatcher {
        override fun afterTextChanged(s: Editable?) { onChanged(s.toString()) }
        override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
        override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {}
    }

    private fun calculateAndShowCarbon(input: String, factor: Double, resultView: TextView) {
        val usage = input.toDoubleOrNull() ?: 0.0
        val carbon = usage * factor
        resultView.text = String.format("Carbon: %.2f kg CO2e", carbon)
    }
}