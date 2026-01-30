package iss.nus.edu.sg.sharedprefs.admobile

import android.content.Intent
import android.graphics.Bitmap
import android.net.Uri
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

                // 调用食物识别API
                val response = ApiHelper.executeUploadFile(this@AddFoodActivity, "/api/vision/meal-detect", tempFile)
                val responseBody = response.body?.string()

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && responseBody != null) {
                        try {
                            val jsonResponse = JSONObject(responseBody)
                            recognizedLabel = jsonResponse.optString("label", "")
                            val confidence = jsonResponse.optDouble("confidence", 0.0)
                            
                            if (recognizedLabel?.isNotEmpty() == true) {
                                etFoodName.setText(recognizedLabel)
                                Toast.makeText(this@AddFoodActivity, "Recognized: $recognizedLabel (${String.format("%.1f%%", confidence * 100)})", Toast.LENGTH_LONG).show()
                            } else {
                                Toast.makeText(this@AddFoodActivity, "Recognition failed, please enter manually", Toast.LENGTH_SHORT).show()
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

        if (selectedImageFile == null || !selectedImageFile!!.exists()) {
            Toast.makeText(this, "Please select an image first", Toast.LENGTH_SHORT).show()
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                // 使用multipart上传，包含图片和JSON字段
                val response = ApiHelper.executeUploadActivity(
                    this@AddFoodActivity,
                    "/api/activity/upload",
                    selectedImageFile!!,
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
