package iss.nus.edu.sg.sharedprefs.admobile

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.graphics.Bitmap
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.provider.MediaStore
import android.view.View
import android.widget.Button
import android.widget.EditText
import android.widget.ImageView
import android.widget.ProgressBar
import android.widget.Spinner
import android.widget.TextView
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import com.google.android.material.appbar.MaterialToolbar
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONObject
import java.io.File
import java.io.FileOutputStream

class AddFoodActivity : AppCompatActivity() {
    private lateinit var imagePreview: ImageView
    private lateinit var addPhotoPlaceholder: View
    private lateinit var choosePhotoButton: Button
    private lateinit var etFoodName: EditText
    private lateinit var etAmount: EditText
    private lateinit var spinnerUnit: Spinner
    private lateinit var etEmissionFactor: EditText
    private lateinit var etEmissions: EditText
    private lateinit var etNote: EditText
    private lateinit var saveButton: Button
    private lateinit var progressBar: ProgressBar
    
    private var selectedImageUri: Uri? = null
    private var selectedImageFile: File? = null
    private var recognizedLabel: String? = null

    // 图片选择器
    private val imagePickerLauncher = registerForActivityResult(ActivityResultContracts.GetContent()) { uri: Uri? ->
        uri?.let { 
            selectedImageUri = it
            displayImage(it)
            recognizeFood(it)
        }
    }
    
    // 相机权限请求
    private val cameraPermissionLauncher = registerForActivityResult(ActivityResultContracts.RequestPermission()) { isGranted ->
        if (isGranted) {
            takePhoto()
        } else {
            Toast.makeText(this, "Camera permission is required to take photos", Toast.LENGTH_SHORT).show()
        }
    }
    
    // 相机拍照
    private val cameraLauncher = registerForActivityResult(ActivityResultContracts.TakePicture()) { success ->
        if (success && cameraImageUri != null) {
            selectedImageUri = cameraImageUri
            displayImage(cameraImageUri!!)
            recognizeFood(cameraImageUri!!)
        }
    }
    
    private var cameraImageUri: Uri? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.add_food_activity)

        val toolbar: MaterialToolbar = findViewById(R.id.toolbar)
        setSupportActionBar(toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        toolbar.setNavigationOnClickListener {
            onBackPressedDispatcher.onBackPressed()
        }

        // 绑定视图
        imagePreview = findViewById(R.id.image_preview)
        addPhotoPlaceholder = findViewById(R.id.add_photo_placeholder)
        choosePhotoButton = findViewById(R.id.choose_photo_button)
        etFoodName = findViewById(R.id.et_food_name)
        etAmount = findViewById(R.id.et_amount)
        spinnerUnit = findViewById(R.id.spinner_unit)
        etEmissionFactor = findViewById(R.id.et_emission_factor)
        etEmissions = findViewById(R.id.et_emissions)
        etNote = findViewById(R.id.et_note)
        saveButton = findViewById(R.id.save_button)

        // 设置单位Spinner
        val units = arrayOf("kg", "g", "lb", "oz", "piece")
        val adapter = android.widget.ArrayAdapter(this, android.R.layout.simple_spinner_item, units)
        adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
        spinnerUnit.adapter = adapter

        // 选择图片
        choosePhotoButton.setOnClickListener {
            imagePickerLauncher.launch("image/*")
        }
        
        // 点击图片区域也可以选择
        findViewById<View>(R.id.photo_container)?.setOnClickListener {
            imagePickerLauncher.launch("image/*")
        }

        // 保存按钮
        saveButton.setOnClickListener {
            saveFoodRecord()
        }

        // 实时计算排放量
        etAmount.setOnFocusChangeListener { _, hasFocus ->
            if (!hasFocus) calculateEmissions()
        }
        etEmissionFactor.setOnFocusChangeListener { _, hasFocus ->
            if (!hasFocus) calculateEmissions()
        }
    }

    private fun displayImage(uri: Uri) {
        imagePreview.setImageURI(uri)
        imagePreview.visibility = View.VISIBLE
        addPhotoPlaceholder.visibility = View.GONE
    }
    
    private fun showImageSourceDialog() {
        val options = arrayOf("Camera", "Gallery")
        android.app.AlertDialog.Builder(this)
            .setTitle("Select Image Source")
            .setItems(options) { _, which ->
                when (which) {
                    0 -> {
                        // 检查相机权限
                        if (ContextCompat.checkSelfPermission(this, Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED) {
                            takePhoto()
                        } else {
                            cameraPermissionLauncher.launch(Manifest.permission.CAMERA)
                        }
                    }
                    1 -> imagePickerLauncher.launch("image/*")
                }
            }
            .show()
    }
    
    private fun takePhoto() {
        val photoFile = File(cacheDir, "food_photo_${System.currentTimeMillis()}.jpg")
        val uri = androidx.core.content.FileProvider.getUriForFile(
            this,
            "${packageName}.fileprovider",
            photoFile
        )
        cameraImageUri = uri
        cameraLauncher.launch(uri)
    }

    private fun recognizeFood(uri: Uri) {
        val token = ApiHelper.getToken(this)
        if (token == null) {
            Toast.makeText(this, "Please login first", Toast.LENGTH_SHORT).show()
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                // 将 Uri 转换为 File
                val inputStream = contentResolver.openInputStream(uri)
                val tempFile = File(cacheDir, "food_${System.currentTimeMillis()}.jpg")
                val outputStream = FileOutputStream(tempFile)
                
                inputStream?.use { input ->
                    outputStream.use { output ->
                        input.copyTo(output)
                    }
                }
                
                selectedImageFile = tempFile

                // 调用食物识别API（使用长超时）
                val response = ApiHelper.executeUploadFile(this@AddFoodActivity, "/api/vision/analyze", tempFile, "file", useLongTimeout = true)
                val responseBody = response.body?.string()

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && responseBody != null) {
                        try {
                            val jsonResponse = JSONObject(responseBody)
                            recognizedLabel = jsonResponse.optString("label", "")
                            val confidence = jsonResponse.optDouble("confidence", 0.0)
                            
                            val label = recognizedLabel
                            if (!label.isNullOrEmpty()) {
                                etFoodName.setText(label)
                                // 减少 Toast 频率，只在成功时显示一次
                                android.util.Log.d("AddFoodActivity", "Recognized: $label (${String.format("%.1f%%", confidence * 100)})")
                                // 自动获取排放因子
                                fetchEmissionFactor(label)
                            } else {
                                android.util.Log.w("AddFoodActivity", "Recognition failed, please enter manually")
                            }
                        } catch (e: Exception) {
                            Toast.makeText(this@AddFoodActivity, "Failed to parse recognition result", Toast.LENGTH_SHORT).show()
                            android.util.Log.e("AddFoodActivity", "Parse error", e)
                        }
                    } else {
                        val errorMsg = if (responseBody != null) {
                            try {
                                JSONObject(responseBody).optString("message", "Recognition failed")
                            } catch (e: Exception) {
                                "Recognition failed: ${response.code}"
                            }
                        } else {
                            "Recognition failed: ${response.code}"
                        }
                        Toast.makeText(this@AddFoodActivity, errorMsg, Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@AddFoodActivity, "Error: ${e.message}", Toast.LENGTH_SHORT).show()
                    android.util.Log.e("AddFoodActivity", "Recognition error", e)
                }
            }
        }
    }

    private fun calculateEmissions() {
        val amount = etAmount.text.toString().toDoubleOrNull() ?: 0.0
        val factor = etEmissionFactor.text.toString().toDoubleOrNull() ?: 0.0
        val emissions = amount * factor
        etEmissions.setText(String.format("%.2f", emissions))
    }
    
    private fun fetchEmissionFactor(foodName: String) {
        CoroutineScope(Dispatchers.IO).launch {
            try {
                val response = ApiHelper.executeGet(this@AddFoodActivity, "/api/carbon/factors?category=Food")
                val responseBody = response.body?.string()
                
                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && responseBody != null) {
                        try {
                            val jsonArray = org.json.JSONArray(responseBody)
                            // 查找匹配的食物因子（不区分大小写，部分匹配）
                            for (i in 0 until jsonArray.length()) {
                                val item = jsonArray.getJSONObject(i)
                                val label = item.optString("labelName", "")
                                if (label.contains(foodName, ignoreCase = true) || foodName.contains(label, ignoreCase = true)) {
                                    val factor = item.optDouble("factor", 7.5)
                                    etEmissionFactor.setText(factor.toString())
                                    calculateEmissions()
                                    return@withContext
                                }
                            }
                            // 如果没有找到，使用默认值
                            etEmissionFactor.setText("7.5")
                            calculateEmissions()
                        } catch (e: Exception) {
                            android.util.Log.e("AddFoodActivity", "Failed to parse carbon factors", e)
                            etEmissionFactor.setText("7.5")
                        }
                    } else {
                        // 使用默认值
                        etEmissionFactor.setText("7.5")
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    android.util.Log.e("AddFoodActivity", "Failed to fetch emission factor", e)
                    etEmissionFactor.setText("7.5")
                }
            }
        }
    }

    private fun saveFoodRecord() {
        val foodName = etFoodName.text.toString().trim()
        val amount = etAmount.text.toString().toDoubleOrNull()
        val unit = spinnerUnit.selectedItem?.toString() ?: "kg"
        val factor = etEmissionFactor.text.toString().toDoubleOrNull()
        val note = etNote.text.toString().trim()

        if (foodName.isEmpty()) {
            Toast.makeText(this, "Please enter food name", Toast.LENGTH_SHORT).show()
            return
        }

        if (amount == null || amount <= 0) {
            Toast.makeText(this, "Please enter valid amount", Toast.LENGTH_SHORT).show()
            return
        }

        val token = ApiHelper.getToken(this)
        if (token == null) {
            Toast.makeText(this, "Please login first", Toast.LENGTH_SHORT).show()
            return
        }

        // 图片现在是可选的
        // if (selectedImageFile == null || !selectedImageFile!!.exists()) {
        //     Toast.makeText(this, "Please select an image first", Toast.LENGTH_SHORT).show()
        //     return
        // }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                // 使用multipart上传，包含图片和JSON字段
                // 如果图片存在，使用图片；否则创建一个临时文件
                val imageFile = selectedImageFile ?: File(cacheDir, "temp_food.jpg").apply {
                    createNewFile()
                }
                
                val response = ApiHelper.executeUploadActivity(
                    this@AddFoodActivity,
                    "/api/activity/upload",
                    imageFile,
                    foodName,
                    amount.toBigDecimal(),
                    unit
                )
                val responseBody = response.body?.string()

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful) {
                        Toast.makeText(this@AddFoodActivity, "Food record saved successfully!", Toast.LENGTH_SHORT).show()
                        finish()
                    } else {
                        val errorMsg = responseBody?.let {
                            try {
                                JSONObject(it).optString("message", "Failed to save")
                            } catch (e: Exception) {
                                "Failed to save: ${response.code}"
                            }
                        } ?: "Failed to save: ${response.code}"
                        Toast.makeText(this@AddFoodActivity, errorMsg, Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@AddFoodActivity, "Error: ${e.message}", Toast.LENGTH_SHORT).show()
                    android.util.Log.e("AddFoodActivity", "Save error", e)
                }
            }
        }
    }
}
