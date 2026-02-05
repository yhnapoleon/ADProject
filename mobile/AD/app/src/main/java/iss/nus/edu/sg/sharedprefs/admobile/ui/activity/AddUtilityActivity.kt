package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.Manifest
import android.app.DatePickerDialog
import android.content.Context
import android.content.pm.PackageManager
import android.graphics.Bitmap
import android.net.Uri
import android.os.Bundle
import android.util.Log
import android.view.View
import android.widget.*
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.google.android.material.appbar.MaterialToolbar
import com.google.android.material.card.MaterialCardView
import iss.nus.edu.sg.sharedprefs.admobile.R
import iss.nus.edu.sg.sharedprefs.admobile.data.model.ManualUtilityRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.network.NetworkClient
import kotlinx.coroutines.launch
import okhttp3.MediaType.Companion.toMediaTypeOrNull
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.asRequestBody
import java.io.File
import java.io.FileOutputStream
import java.text.SimpleDateFormat
import java.util.*

class AddUtilityActivity : AppCompatActivity() {

    // 后端要求的日期格式
    private val apiDateFormat = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'", Locale.getDefault()).apply {
        timeZone = TimeZone.getTimeZone("UTC")
    }
    // 本地显示的日期格式
    private val displayFormat = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())

    private var startCalendar = Calendar.getInstance()
    private var endCalendar = Calendar.getInstance()

    private lateinit var etStartDate: EditText
    private lateinit var etEndDate: EditText
    private lateinit var etElectricity: EditText
    private lateinit var etWater: EditText
    private lateinit var ivBillPreview: ImageView
    private lateinit var llPlaceholder: LinearLayout
    private lateinit var etNotes: EditText

    // 🌟 相册选择回调 -> 触发上传识别
    private val pickImageLauncher = registerForActivityResult(ActivityResultContracts.GetContent()) { uri: Uri? ->
        uri?.let {
            ivBillPreview.setImageURI(it)
            showImagePreviewUI()
            uploadAndRecognizeBill(it)
        }
    }

    // 🌟 拍照回调 (注意：缩略图转文件比较麻烦，建议优先用相册或实现完整的拍照存文件逻辑)
    private val takePictureLauncher = registerForActivityResult(ActivityResultContracts.TakePicturePreview()) { bitmap: Bitmap? ->
        bitmap?.let {
            ivBillPreview.setImageBitmap(it)
            showImagePreviewUI()
            // 拍照后的处理建议转为 File 后再调用 uploadAndRecognizeBill
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.add_utility_activity)

        // 1. 视图绑定
        initViews()

        // 2. 日期选择器逻辑
        setupDatePickers()

        // 3. 上传与识别逻辑
        findViewById<MaterialCardView>(R.id.card_scan_bill).setOnClickListener {
            showImageSourceDialog()
        }

        // 4. 保存到数据库逻辑
        findViewById<Button>(R.id.save_button).setOnClickListener {
            saveBillToDatabase()
        }
    }

    private fun initViews() {
        val toolbar: MaterialToolbar = findViewById(R.id.toolbar)
        setSupportActionBar(toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        toolbar.setNavigationOnClickListener { finish() }

        etStartDate = findViewById(R.id.et_start_date) // 请确保 XML 中 ID 对应
        etEndDate = findViewById(R.id.et_end_date)     // 请确保 XML 中 ID 对应
        etElectricity = findViewById(R.id.et_electricity)
        etWater = findViewById(R.id.et_water)
        ivBillPreview = findViewById(R.id.iv_bill_preview)
        llPlaceholder = findViewById(R.id.ll_scan_placeholder)
        etNotes = findViewById(R.id.et_notes)

        // 初始化默认日期显示
        etStartDate.setText(displayFormat.format(startCalendar.time))
        etEndDate.setText(displayFormat.format(endCalendar.time))
    }

    private fun setupDatePickers() {
        etStartDate.setOnClickListener {
            showDatePicker(startCalendar) { etStartDate.setText(displayFormat.format(it.time)) }
        }
        etEndDate.setOnClickListener {
            showDatePicker(endCalendar) { etEndDate.setText(displayFormat.format(it.time)) }
        }
    }

    private fun showDatePicker(calendar: Calendar, onDateSet: (Calendar) -> Unit) {
        DatePickerDialog(this, { _, year, month, day ->
            calendar.set(year, month, day)
            onDateSet(calendar)
        }, calendar.get(Calendar.YEAR), calendar.get(Calendar.MONTH), calendar.get(Calendar.DAY_OF_MONTH)).show()
    }

    /**
     * 🌟 调用后端 /api/UtilityBill/upload 接口识别图片
     */
    private fun uploadAndRecognizeBill(uri: Uri) {
        lifecycleScope.launch {
            try {
                val file = uriToFile(uri) ?: return@launch
                val requestFile = file.asRequestBody("image/*".toMediaTypeOrNull())
                val body = MultipartBody.Part.createFormData("file", file.name, requestFile)

                val token = getAuthToken()
                val response = NetworkClient.apiService.uploadUtilityBill(token, body)

                if (response.isSuccessful && response.body() != null) {
                    val bill = response.body()!!
                    // 自动填充识别到的数据
                    etElectricity.setText(bill.electricityUsage.toString())
                    etWater.setText(bill.waterUsage.toString())

                    // 解析后端 ISO 日期并设置到本地变量和 UI
                    parseApiDateToUI(bill.billPeriodStart, startCalendar, etStartDate)
                    parseApiDateToUI(bill.billPeriodEnd, endCalendar, etEndDate)

                    Toast.makeText(this@AddUtilityActivity, "Bill recognized!", Toast.LENGTH_SHORT).show()
                } else {
                    Toast.makeText(this@AddUtilityActivity, "Recognition failed", Toast.LENGTH_SHORT).show()
                }
            } catch (e: Exception) {
                Log.e("OCR_ERROR", e.message ?: "Error")
            }
        }
    }

    /**
     * 🌟 调用后端 /api/UtilityBill/manual 接口保存到数据库
     */
    private fun saveBillToDatabase() {
        // 🌟 从输入框获取文本
        val noteContent = etNotes.text.toString()

        val request = ManualUtilityRequest(
            billType = 0,
            billPeriodStart = apiDateFormat.format(startCalendar.time),
            billPeriodEnd = apiDateFormat.format(endCalendar.time),
            electricityUsage = etElectricity.text.toString().toDoubleOrNull() ?: 0.0,
            waterUsage = etWater.text.toString().toDoubleOrNull() ?: 0.0,
            notes = noteContent // 🌟 将获取的内容放入请求对象
        )

        lifecycleScope.launch {
            try {
                val token = getAuthToken()
                val response = NetworkClient.apiService.saveUtilityManual(token, request)

                if (response.isSuccessful) {
                    Toast.makeText(this@AddUtilityActivity, "Record saved!", Toast.LENGTH_SHORT).show()
                    finish()
                } else {
                    Toast.makeText(this@AddUtilityActivity, "Error: ${response.code()}", Toast.LENGTH_SHORT).show()
                }
            } catch (e: Exception) {
                Toast.makeText(this@AddUtilityActivity, "Network Error", Toast.LENGTH_SHORT).show()
            }
        }
    }

    // --- 辅助工具函数 ---

    private fun getAuthToken(): String {
        val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
        return "Bearer ${prefs.getString("access_token", "")}"
    }

    private fun parseApiDateToUI(dateStr: String, calendar: Calendar, editText: EditText) {
        try {
            val date = apiDateFormat.parse(dateStr)
            if (date != null) {
                calendar.time = date
                editText.setText(displayFormat.format(date))
            }
        } catch (e: Exception) { e.printStackTrace() }
    }

    private fun uriToFile(uri: Uri): File? {
        val inputStream = contentResolver.openInputStream(uri) ?: return null
        val tempFile = File(cacheDir, "upload_bill.jpg")
        val outputStream = FileOutputStream(tempFile)
        inputStream.use { input -> outputStream.use { output -> input.copyTo(output) } }
        return tempFile
    }

    private fun showImagePreviewUI() {
        ivBillPreview.visibility = View.VISIBLE
        llPlaceholder.visibility = View.GONE
    }

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

    private val requestPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { if (it) takePictureLauncher.launch(null) }
}