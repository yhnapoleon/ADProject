package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.Manifest
import android.content.Context
import android.content.pm.PackageManager
import android.graphics.Bitmap
import android.graphics.BitmapFactory
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
import com.google.zxing.*
import com.google.zxing.common.HybridBinarizer
import iss.nus.edu.sg.sharedprefs.admobile.R
import iss.nus.edu.sg.sharedprefs.admobile.data.model.AddFoodRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.model.CalculateFoodRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.network.NetworkClient
import kotlinx.coroutines.launch
import okhttp3.MediaType.Companion.toMediaTypeOrNull
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.asRequestBody
import java.io.File
import java.io.FileOutputStream
import java.util.*

class AddFoodActivity : AppCompatActivity() {

    private lateinit var imagePreview: ImageView
    private lateinit var placeholder: LinearLayout
    private lateinit var editFoodName: EditText
    private lateinit var editAmount: AutoCompleteTextView
    private lateinit var editEmissionFactor: EditText
    private lateinit var editEmissions: EditText
    private lateinit var editNote: EditText
    private lateinit var analyzeProgress: ProgressBar
    private lateinit var btnCalculate: Button
    private lateinit var btnSave: Button
    private val TAG = "ECO_DEBUG"

    // 🌟 修改：Launcher 不再直接调用识别，而是走统一处理入口
    private val pickImageLauncher = registerForActivityResult(ActivityResultContracts.GetContent()) { uri: Uri? ->
        uri?.let { handleSelectedImage(it) }
    }

    private val takePictureLauncher = registerForActivityResult(ActivityResultContracts.TakePicturePreview()) { bitmap: Bitmap? ->
        bitmap?.let {
            val uri = saveBitmapToTempFile(it)
            handleSelectedImage(uri)
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.add_food_activity)
        initViews()
        setupAmountDropdown()
    }

    private fun initViews() {
        imagePreview = findViewById(R.id.image_preview)
        placeholder = findViewById(R.id.add_photo_placeholder)
        editFoodName = findViewById(R.id.editFoodName)
        editAmount = findViewById(R.id.editAmount)
        editEmissionFactor = findViewById(R.id.editEmissionFactor)
        editEmissions = findViewById(R.id.editEmissions)
        editNote = findViewById(R.id.editNote)
        analyzeProgress = findViewById(R.id.analyze_progress)
        btnCalculate = findViewById(R.id.btn_calculate)
        btnSave = findViewById(R.id.save_button)

        val btnChoose = findViewById<Button>(R.id.choose_photo_button)
        val toolbar: MaterialToolbar = findViewById(R.id.toolbar)
        setSupportActionBar(toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        toolbar.setNavigationOnClickListener { onBackPressedDispatcher.onBackPressed() }

        btnChoose.setOnClickListener { showImageSourceDialog() }
        imagePreview.setOnClickListener { showImageSourceDialog() }
        btnCalculate.setOnClickListener { calculateEmissions() }
        btnSave.setOnClickListener { submitFinalData() }
    }

    /**
     * 🌟 核心处理入口：区分条形码和普通图片
     */
    private fun handleSelectedImage(uri: Uri) {
        Log.d(TAG, "handleSelectedImage: Start processing URI: $uri")
        showImage(uri)
        lifecycleScope.launch {
            try {
                analyzeProgress.visibility = View.VISIBLE

                // 1. 尝试本地扫描条形码
                val bitmap = contentResolver.openInputStream(uri)?.use {
                    Log.d(TAG, "handleSelectedImage: Image opened successfully")
                    BitmapFactory.decodeStream(it)
                }

                val barcode = if (bitmap != null) {
                    val result = scanBarcode(bitmap)
                    Log.d(TAG, "handleSelectedImage: Local scan result: ${result ?: "No barcode found"}")
                    result
                } else null

                if (barcode != null) {
                    queryBarcodeData(barcode, uri)
                } else {
                    Log.d(TAG, "handleSelectedImage: No barcode detected, falling back to AI Analysis")
                    uploadAndAnalyze(uri)
                }
            } catch (e: Exception) {
                Log.e(TAG, "handleSelectedImage Error: ${e.message}", e)
                analyzeProgress.visibility = View.GONE
                Toast.makeText(this@AddFoodActivity, "Error processing image", Toast.LENGTH_SHORT).show()
            }
        }
    }

    /**
     * 🌟 ZXing 本地解析逻辑
     */
    private fun scanBarcode(bitmap: Bitmap): String? {
        val width = bitmap.width
        val height = bitmap.height
        val pixels = IntArray(width * height)
        bitmap.getPixels(pixels, 0, width, 0, 0, width, height)

        val source = RGBLuminanceSource(width, height, pixels)
        val binarizer = HybridBinarizer(source)
        val binaryBitmap = BinaryBitmap(binarizer)

        return try {
            val reader = MultiFormatReader()
            val hints = EnumMap<DecodeHintType, Any>(DecodeHintType::class.java)
            hints[DecodeHintType.POSSIBLE_FORMATS] = listOf(
                BarcodeFormat.EAN_13, BarcodeFormat.EAN_8,
                BarcodeFormat.UPC_A, BarcodeFormat.UPC_E
            )
            val result = reader.decode(binaryBitmap, hints)
            result.text
        } catch (e: Exception) {
            null
        }
    }

    /**
     * 🌟 调用后端条形码接口
     */
    private fun queryBarcodeData(barcode: String, originalUri: Uri) {
        lifecycleScope.launch {
            try {
                Log.d(TAG, "queryBarcodeData: Fetching data for barcode: $barcode")
                val token = getAuthToken()
                val response = NetworkClient.apiService.getProductByBarcode(token, barcode, false, false)

                if (response.isSuccessful && response.body() != null) {
                    val product = response.body()!!
                    Log.d(TAG, "queryBarcodeData: Success! Product: ${product.productName}")
                    editFoodName.setText(product.productName)
                    editEmissionFactor.setText(String.format("%.4f", product.co2Factor))

                    if (!product.brand.isNullOrEmpty()) {
                        editNote.setText("Brand: ${product.brand}")
                    }

                    Toast.makeText(this@AddFoodActivity, "Barcode matched: ${product.productName}", Toast.LENGTH_SHORT).show()
                    analyzeProgress.visibility = View.GONE
                } else {
                    Log.w(TAG, "queryBarcodeData: Failed. Code: ${response.code()}, Msg: ${response.message()}")
                    Log.d(TAG, "queryBarcodeData: Falling back to AI Analysis")
                    uploadAndAnalyze(originalUri)
                }
            } catch (e: Exception) {
                Log.e(TAG, "queryBarcodeData Network Error: ${e.message}", e)
                uploadAndAnalyze(originalUri)
            }
        }
    }

    /**
     * 图片分析逻辑 (原有逻辑)
     */
    private fun uploadAndAnalyze(uri: Uri) {
        lifecycleScope.launch {
            try {
                Log.d(TAG, "uploadAndAnalyze: Starting AI Analysis for URI: $uri")
                analyzeProgress.visibility = View.VISIBLE
                imagePreview.alpha = 0.5f

                val token = getAuthToken()
                val file = getFileFromUri(uri)

                // 打印文件信息，看是否过大导致超时
                Log.d(TAG, "uploadAndAnalyze: Prepared file: ${file.path}, Size: ${file.length() / 1024} KB")

                val requestFile = file.asRequestBody("image/*".toMediaTypeOrNull())
                val body = MultipartBody.Part.createFormData("file", file.name, requestFile)

                Log.d(TAG, "uploadAndAnalyze: Sending request to server...")
                val response = NetworkClient.apiService.analyzeFoodImage(token, body)

                if (response.isSuccessful && response.body() != null) {
                    val label = response.body()!!.label
                    Log.d(TAG, "uploadAndAnalyze: AI Analysis Success. Label: $label")
                    editFoodName.setText(label)
                    Toast.makeText(this@AddFoodActivity, "AI Detected: $label", Toast.LENGTH_SHORT).show()
                } else {
                    Log.e(TAG, "uploadAndAnalyze: API Error. Code: ${response.code()}, Body: ${response.errorBody()?.string()}")
                    Toast.makeText(this@AddFoodActivity, "AI Analysis failed (Code ${response.code()})", Toast.LENGTH_SHORT).show()
                }
            } catch (e: Exception) {
                Log.e(TAG, "uploadAndAnalyze Exception: ${e.message}", e)
                Toast.makeText(this@AddFoodActivity, "Network error: ${e.localizedMessage}", Toast.LENGTH_SHORT).show()
            } finally {
                analyzeProgress.visibility = View.GONE
                imagePreview.alpha = 1.0f
            }
        }
    }

    private fun calculateEmissions() {
        val name = editFoodName.text.toString().trim()
        val rawAmount = editAmount.text.toString().trim()

        if (name.isEmpty() || rawAmount.isEmpty()) {
            Toast.makeText(this, "Please enter food name and amount", Toast.LENGTH_SHORT).show()
            return
        }

        val amountValue = parseAmount(rawAmount)
        if (amountValue <= 0) {
            Toast.makeText(this, "Please enter a valid amount", Toast.LENGTH_SHORT).show()
            return
        }

        lifecycleScope.launch {
            try {
                btnCalculate.isEnabled = false
                btnCalculate.text = "Calculating..."
                val token = getAuthToken()
                val request = CalculateFoodRequest(name, amountValue)
                val response = NetworkClient.apiService.calculateFoodEmission(token, request)

                if (response.isSuccessful && response.body() != null) {
                    val result = response.body()!!
                    editEmissionFactor.setText(String.format("%.4f", result.emission_factor))
                    editEmissions.setText(String.format("%.4f", result.emission))
                    Toast.makeText(this@AddFoodActivity, "Calculated!", Toast.LENGTH_SHORT).show()
                }
            } catch (e: Exception) {
                Toast.makeText(this@AddFoodActivity, "Network error", Toast.LENGTH_SHORT).show()
            } finally {
                btnCalculate.isEnabled = true
                btnCalculate.text = "Confirm and Calculate"
            }
        }
    }

    private fun submitFinalData() {
        val name = editFoodName.text.toString().trim()
        val rawAmount = editAmount.text.toString().trim()
        val factor = editEmissionFactor.text.toString().toDoubleOrNull() ?: 0.0
        val totalEmission = editEmissions.text.toString().toDoubleOrNull() ?: 0.0
        val note = editNote.text.toString().trim()

        if (name.isEmpty() || totalEmission <= 0.0) {
            Toast.makeText(this, "Please complete the calculation first", Toast.LENGTH_SHORT).show()
            return
        }

        lifecycleScope.launch {
            try {
                btnSave.isEnabled = false
                btnSave.text = "Saving..."
                val token = getAuthToken()
                val request = AddFoodRequest(name, parseAmount(rawAmount), factor, totalEmission, note)
                val response = NetworkClient.apiService.addFoodRecord(token, request)

                if (response.isSuccessful && response.body()?.success == true) {
                    Toast.makeText(this@AddFoodActivity, "Record saved successfully!", Toast.LENGTH_SHORT).show()
                    finish()
                }
            } catch (e: Exception) {
                Toast.makeText(this@AddFoodActivity, "Network error during save", Toast.LENGTH_SHORT).show()
            } finally {
                btnSave.isEnabled = true
                btnSave.text = "Save"
            }
        }
    }

    // --- 工具方法 ---

    private fun getAuthToken(): String {
        val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
        return "Bearer ${prefs.getString("access_token", "")}"
    }

    private fun parseAmount(raw: String): Double {
        return if (raw.contains("-")) {
            try {
                val parts = raw.replace("g", "").split("-")
                (parts[0].toDouble() + parts[1].toDouble()) / 2.0
            } catch (e: Exception) { 0.0 }
        } else {
            raw.toDoubleOrNull() ?: 0.0
        }
    }

    private fun setupAmountDropdown() {
        val ranges = mutableListOf<String>()
        for (i in 0 until 1000 step 50) { ranges.add("${i}-${i + 50}") }
        val adapter = ArrayAdapter(this, android.R.layout.simple_dropdown_item_1line, ranges)
        editAmount.setAdapter(adapter)
        editAmount.setOnItemClickListener { parent, _, position, _ ->
            val selected = parent.getItemAtPosition(position).toString()
            editAmount.setText(selected)
            editAmount.setSelection(editAmount.text.length)
        }
    }

    private fun getFileFromUri(uri: Uri): File {
        val file = File(cacheDir, "temp_food_${System.currentTimeMillis()}.jpg")
        contentResolver.openInputStream(uri)?.use { input ->
            FileOutputStream(file).use { output -> input.copyTo(output) }
        }
        return file
    }

    private fun saveBitmapToTempFile(bitmap: Bitmap): Uri {
        val file = File(cacheDir, "camera_temp_${System.currentTimeMillis()}.jpg")
        FileOutputStream(file).use { out -> bitmap.compress(Bitmap.CompressFormat.JPEG, 95, out) }
        return Uri.fromFile(file)
    }

    private fun showImageSourceDialog() {
        val options = arrayOf("Take Photo", "Choose from Gallery")
        AlertDialog.Builder(this)
            .setTitle("Add Food Photo")
            .setItems(options) { _, which ->
                when (which) {
                    0 -> checkCameraPermissionAndLaunch()
                    1 -> pickImageLauncher.launch("image/*")
                }
            }.show()
    }

    private fun checkCameraPermissionAndLaunch() {
        if (checkSelfPermission(Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED) {
            takePictureLauncher.launch(null)
        } else {
            requestPermissionLauncher.launch(Manifest.permission.CAMERA)
        }
    }

    private val requestPermissionLauncher = registerForActivityResult(ActivityResultContracts.RequestPermission()) { isGranted ->
        if (isGranted) takePictureLauncher.launch(null)
    }

    private fun showImage(uri: Uri) {
        imagePreview.setImageURI(uri)
        imagePreview.visibility = View.VISIBLE
        placeholder.visibility = View.GONE
    }

    private fun showImageBitmap(bitmap: Bitmap) {
        imagePreview.setImageBitmap(bitmap)
        imagePreview.visibility = View.VISIBLE
        placeholder.visibility = View.GONE
    }
}