package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.content.Intent
import android.content.SharedPreferences
import android.graphics.Color
import android.net.Uri
import android.os.Bundle
import android.os.Environment
import android.view.View
import android.widget.EditText
import android.widget.ImageView
import android.widget.TextView
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.FileProvider
import com.bumptech.glide.Glide
import com.google.android.material.button.MaterialButton
import iss.nus.edu.sg.sharedprefs.admobile.R
import iss.nus.edu.sg.sharedprefs.admobile.utils.NavigationUtils
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import okhttp3.*
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.RequestBody.Companion.asRequestBody
import org.json.JSONObject
import java.io.File
import java.io.FileOutputStream
import java.text.SimpleDateFormat
import java.util.*
import java.util.concurrent.TimeUnit

class ProfileActivity : AppCompatActivity() {

    private var isEditing = false
    private lateinit var editBtn: MaterialButton
    private lateinit var profileAvatar: ImageView

    // 用于存储相机拍照的临时 Uri
    private var photoUri: Uri? = null

    private val editableIds = listOf(
        R.id.item_username, R.id.item_nickname, R.id.item_email,
        R.id.item_password, R.id.item_birth, R.id.item_location
    )

    // 🌟 注册相册选择回调
    private val pickImageLauncher = registerForActivityResult(ActivityResultContracts.GetContent()) { uri: Uri? ->
        uri?.let {
            profileAvatar.setImageURI(it)
            // 这里通常需要将图片上传到后端 Azure API
            uploadAvatar(it)
        }
    }

    // 🌟 注册相机拍照回调
    private val takePhotoLauncher = registerForActivityResult(ActivityResultContracts.TakePicture()) { success: Boolean ->
        if (success) {
            photoUri?.let {
                profileAvatar.setImageURI(it)
                uploadAvatar(it)
            }
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_profile)

        window.statusBarColor = Color.parseColor("#674fa3")

        // 1. 初始化视图与展示数据
        profileAvatar = findViewById(R.id.profile_avatar)
        setupProfileData()

        // 2. 头像点击逻辑
        profileAvatar.setOnClickListener {
            showImageSourceDialog()
        }

        // 3. 绑定编辑/保存按钮逻辑
        editBtn = findViewById(R.id.btn_edit_profile)
        editBtn.setOnClickListener {
            toggleEditMode()
        }

        // 4. 跳转记录页
        findViewById<View>(R.id.card_history).setOnClickListener {
            startActivity(Intent(this, EmissionRecordsActivity::class.java))
        }

        // 5. 初始化导航栏
        NavigationUtils.setupBottomNavigation(this, R.id.nav_person)
    }

    /**
     * 弹出选择框：拍照或相册
     */
    private fun showImageSourceDialog() {
        val options = arrayOf("Take Photo", "Choose from Gallery", "Cancel")
        AlertDialog.Builder(this)
            .setTitle("Update Profile Picture")
            .setItems(options) { dialog, which ->
                when (which) {
                    0 -> openCamera()
                    1 -> pickImageLauncher.launch("image/*")
                    else -> dialog.dismiss()
                }
            }
            .show()
    }

    private fun openCamera() {
        val photoFile = createImageFile()
        photoUri = FileProvider.getUriForFile(
            this,
            "${packageName}.fileprovider",
            photoFile
        )
        // 🌟 修改这里：只有非空才 launch
        photoUri?.let { uri ->
            takePhotoLauncher.launch(uri)
        }
    }

    private fun createImageFile(): File {
        val timeStamp = SimpleDateFormat("yyyyMMdd_HHmmss", Locale.getDefault()).format(Date())
        val storageDir = getExternalFilesDir(Environment.DIRECTORY_PICTURES)
        return File.createTempFile("AVATAR_${timeStamp}_", ".jpg", storageDir)
    }

    private fun toggleEditMode() {
        isEditing = !isEditing

        for (id in editableIds) {
            val itemView = findViewById<View>(id)
            val tvValue = itemView.findViewById<TextView>(R.id.info_value)
            val etEdit = itemView.findViewById<EditText>(R.id.info_edit)

            if (isEditing) {
                etEdit.setText(tvValue.text)
                etEdit.visibility = View.VISIBLE
                tvValue.visibility = View.GONE
            } else {
                tvValue.text = etEdit.text.toString()
                etEdit.visibility = View.GONE
                tvValue.visibility = View.VISIBLE

                if (id == R.id.item_username) findViewById<TextView>(R.id.profile_name).text = tvValue.text
                if (id == R.id.item_email) findViewById<TextView>(R.id.profile_email).text = tvValue.text
            }
        }
        editBtn.text = if (isEditing) "Save Changes" else "Edit Profile"
    }

    private fun setupProfileData() {
        setInfo(R.id.item_username, "Username", "Melody")
        setInfo(R.id.item_nickname, "Nickname", "EcoRanger")
        setInfo(R.id.item_email, "Email", "melody@example.com")
        setInfo(R.id.item_password, "Password", "••••••••")
        setInfo(R.id.item_birth, "Birth Date", "March 15, 1995")
        setInfo(R.id.item_location, "Location", "West Region")
        setInfo(R.id.item_join_date, "Join Date", "September 20, 2025")
    }

    private fun setInfo(viewId: Int, label: String, value: String) {
        val root = findViewById<View>(viewId)
        root.findViewById<TextView>(R.id.info_label).text = label
        root.findViewById<TextView>(R.id.info_value).text = value
    }

    private fun uploadAvatar(uri: Uri) {
        val token = getToken()
        if (token == null) {
            Toast.makeText(this, "Please login first", Toast.LENGTH_SHORT).show()
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                // 将 Uri 转换为 File
                val inputStream = contentResolver.openInputStream(uri)
                val tempFile = File(cacheDir, "avatar_${System.currentTimeMillis()}.jpg")
                val outputStream = FileOutputStream(tempFile)
                
                inputStream?.use { input ->
                    outputStream.use { output ->
                        input.copyTo(output)
                    }
                }

                // 创建 OkHttpClient（使用长超时）
                val client = OkHttpClient.Builder()
                    .connectTimeout(30, TimeUnit.SECONDS)
                    .readTimeout(60, TimeUnit.SECONDS)
                    .writeTimeout(60, TimeUnit.SECONDS)
                    .build()

                // 创建请求体
                val requestBody = MultipartBody.Builder()
                    .setType(MultipartBody.FORM)
                    .addFormDataPart(
                        "file",
                        tempFile.name,
                        tempFile.asRequestBody("image/jpeg".toMediaType())
                    )
                    .build()

                // 创建请求
                val request = Request.Builder()
                    .url("https://ecolens-api-daa7a0e4a3d4d7e8.southeastasia-01.azurewebsites.net/api/user/avatar")
                    .put(requestBody)
                    .addHeader("Authorization", "Bearer $token")
                    .build()

                // 执行请求
                val response = client.newCall(request).execute()
                val responseBody = response.body?.string()

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && responseBody != null) {
                        try {
                            val jsonResponse = JSONObject(responseBody)
                            val avatarUrl = jsonResponse.optString("avatarUrl", "")
                                .ifEmpty { jsonResponse.optString("avatar", "") }
                            
                            if (avatarUrl.isNotEmpty()) {
                                // 使用 Glide 加载头像（支持 Base64 字符串）
                                if (!isDestroyed && !isFinishing) {
                                    Glide.with(this@ProfileActivity)
                                        .load(avatarUrl)
                                        .circleCrop()
                                        .into(profileAvatar)
                                }
                                Toast.makeText(this@ProfileActivity, "Avatar uploaded successfully!", Toast.LENGTH_SHORT).show()
                            } else {
                                Toast.makeText(this@ProfileActivity, "Failed to get avatar URL", Toast.LENGTH_SHORT).show()
                            }
                        } catch (e: Exception) {
                            android.util.Log.e("ProfileActivity", "Failed to parse avatar response", e)
                            Toast.makeText(this@ProfileActivity, "Failed to parse response", Toast.LENGTH_SHORT).show()
                        }
                    } else {
                        val errorMsg = responseBody?.let {
                            try {
                                JSONObject(it).optString("message", "Failed to upload avatar")
                            } catch (_: Exception) {
                                "Failed to upload avatar: ${response.code}"
                            }
                        } ?: "Failed to upload avatar: ${response.code}"
                        Toast.makeText(this@ProfileActivity, errorMsg, Toast.LENGTH_LONG).show()
                        android.util.Log.e("ProfileActivity", "Upload failed: $errorMsg (Code: ${response.code})")
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@ProfileActivity, "Error: ${e.message}", Toast.LENGTH_SHORT).show()
                    android.util.Log.e("ProfileActivity", "Upload error", e)
                }
            }
        }
    }

    private fun getToken(): String? {
        val prefs: SharedPreferences = getSharedPreferences("EcoLensPrefs", MODE_PRIVATE)
        return prefs.getString("token", null)
    }
}