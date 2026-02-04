package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.Manifest
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.graphics.Color
import android.net.Uri
import android.os.Bundle
import android.os.Environment
import android.text.Editable
import android.text.InputType
import android.text.TextWatcher
import android.util.Base64
import android.view.View
import android.view.ViewGroup
import android.widget.*
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.core.content.FileProvider
import androidx.lifecycle.lifecycleScope
import com.google.android.material.button.MaterialButton
import iss.nus.edu.sg.sharedprefs.admobile.R
import iss.nus.edu.sg.sharedprefs.admobile.data.model.ChangePasswordRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.model.UpdateProfileRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.model.UserProfileResponse
import iss.nus.edu.sg.sharedprefs.admobile.data.network.NetworkClient
import iss.nus.edu.sg.sharedprefs.admobile.utils.NavigationUtils
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import okhttp3.MediaType.Companion.toMediaTypeOrNull
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.toRequestBody
import java.io.ByteArrayOutputStream
import java.io.File

class ProfileActivity : AppCompatActivity() {

    private var isEditing = false
    private lateinit var editBtn: MaterialButton
    private lateinit var profileAvatar: ImageView
    private var photoUri: Uri? = null

    private val editableIds = listOf(
        R.id.item_nickname, R.id.item_email,
        R.id.item_birth, R.id.item_location
    )

    private val pickImageLauncher = registerForActivityResult(ActivityResultContracts.GetContent()) { uri: Uri? ->
        uri?.let {
            profileAvatar.setImageURI(it)
            processPrepareAndUploadAvatar(it)
        }
    }

    private val requestCameraPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { isGranted: Boolean ->
        if (isGranted) openCamera() else Toast.makeText(this, "Permission denied", Toast.LENGTH_SHORT).show()
    }

    private val takePhotoLauncher = registerForActivityResult(ActivityResultContracts.TakePicture()) { success: Boolean ->
        if (success) {
            photoUri?.let {
                profileAvatar.setImageURI(it)
                processPrepareAndUploadAvatar(it)
            }
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_profile)

        window.statusBarColor = Color.parseColor("#674fa3")

        profileAvatar = findViewById(R.id.profile_avatar)
        editBtn = findViewById(R.id.btn_edit_profile)

        fetchUserProfile()

        profileAvatar.setOnClickListener { showImageSourceDialog() }
        editBtn.setOnClickListener { toggleEditMode() }

        findViewById<MaterialButton>(R.id.btn_change_password).setOnClickListener { showStep1OldPasswordDialog() }
        findViewById<MaterialButton>(R.id.btn_logout).setOnClickListener { logout() }
        findViewById<View>(R.id.card_history).setOnClickListener {
            startActivity(Intent(this, EmissionRecordsActivity::class.java))
        }

        NavigationUtils.setupBottomNavigation(this, R.id.nav_person)
    }

    // --- 修改密码逻辑 (保持不变) ---
    private fun showStep1OldPasswordDialog() {
        val etOld = EditText(this).apply {
            hint = "Enter Old Password"
            inputType = InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_PASSWORD
        }
        val container = FrameLayout(this).apply {
            val params = FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT)
            params.setMargins(60, 40, 60, 0)
            addView(etOld, params)
        }
        AlertDialog.Builder(this)
            .setTitle("Identity Verification")
            .setMessage("Please enter your current password to continue.")
            .setView(container)
            .setPositiveButton("Next") { _, _ ->
                val oldPwd = etOld.text.toString()
                if (oldPwd.isNotEmpty()) showStep2NewPasswordDialog(oldPwd)
                else Toast.makeText(this, "Old password is required", Toast.LENGTH_SHORT).show()
            }
            .setNegativeButton("Cancel", null)
            .show()
    }

    private fun showStep2NewPasswordDialog(oldPwd: String) {
        val dialogView = layoutInflater.inflate(R.layout.dialog_change_password_step2, null)
        val etNew = dialogView.findViewById<EditText>(R.id.et_new_password)
        val etConfirm = dialogView.findViewById<EditText>(R.id.et_confirm_password)
        val tvStrength = dialogView.findViewById<TextView>(R.id.tv_password_strength)

        val dialog = AlertDialog.Builder(this)
            .setTitle("Set New Password")
            .setView(dialogView)
            .setPositiveButton("Confirm", null)
            .setNegativeButton("Back", null)
            .create()

        etNew.addTextChangedListener(object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {
                val pwd = s.toString()
                when {
                    pwd.isEmpty() -> tvStrength.text = ""
                    pwd.length < 6 -> { tvStrength.text = "Strength: Weak"; tvStrength.setTextColor(Color.RED) }
                    pwd.length < 10 -> { tvStrength.text = "Strength: Medium"; tvStrength.setTextColor(Color.parseColor("#FFA500")) }
                    else -> { tvStrength.text = "Strength: Strong"; tvStrength.setTextColor(Color.parseColor("#2E7D32")) }
                }
            }
            override fun afterTextChanged(s: Editable?) {}
        })

        etConfirm.addTextChangedListener(object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {
                etConfirm.error = if (s.toString() != etNew.text.toString() && s!!.isNotEmpty()) "Passwords do not match!" else null
            }
            override fun afterTextChanged(s: Editable?) {}
        })

        dialog.show()
        dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener {
            val newPwd = etNew.text.toString()
            if (newPwd.length < 6) { etNew.error = "At least 6 characters"; return@setOnClickListener }
            if (newPwd != etConfirm.text.toString()) { etConfirm.error = "Passwords do not match!"; return@setOnClickListener }
            performActualPasswordUpdate(oldPwd, newPwd, dialog)
        }
    }

    private fun performActualPasswordUpdate(old: String, new: String, dialog: AlertDialog) {
        lifecycleScope.launch {
            val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
            val token = "Bearer ${prefs.getString("access_token", "")}"
            try {
                val response = NetworkClient.apiService.changePassword(token, ChangePasswordRequest(old, new))
                if (response.isSuccessful) {
                    Toast.makeText(this@ProfileActivity, "Password successfully updated!", Toast.LENGTH_SHORT).show()
                    dialog.dismiss()
                } else {
                    val msg = if (response.code() == 401) "Old password is incorrect" else "Error: ${response.code()}"
                    Toast.makeText(this@ProfileActivity, msg, Toast.LENGTH_LONG).show()
                }
            } catch (e: Exception) {
                Toast.makeText(this@ProfileActivity, "Network Error: ${e.message}", Toast.LENGTH_SHORT).show()
            }
        }
    }

    // --- Profile 数据获取与展示 ---
    private fun fetchUserProfile() {
        lifecycleScope.launch {
            val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
            val token = "Bearer ${prefs.getString("access_token", "")}"
            try {
                val response = NetworkClient.apiService.getUserProfile(token)
                if (response.isSuccessful && response.body() != null) updateUI(response.body()!!)
            } catch (e: Exception) {
                Toast.makeText(this@ProfileActivity, "Load failed", Toast.LENGTH_SHORT).show()
            }
        }
    }

    private fun updateUI(p: UserProfileResponse) {
        // 1. 基础文字信息绑定
        findViewById<TextView>(R.id.profile_name).text = p.nickname ?: p.name
        findViewById<TextView>(R.id.profile_email).text = p.email

        setInfo(R.id.item_username, "Username", p.name)
        setInfo(R.id.item_nickname, "Nickname", p.nickname ?: "Not set")
        setInfo(R.id.item_email, "Email", p.email ?: "Not set")
        setInfo(R.id.item_birth, "Birth Date", p.birthDate ?: "Not set")
        setInfo(R.id.item_location, "Location", p.location ?: "Not set")
        setInfo(R.id.item_join_date, "Join Days", "${p.joinDays} Days")

        // 2. 🌟 核心修改：头像显示逻辑（增加禁用缓存配置）
        val avatarUrl = p.avatar
        android.util.Log.d("AVATAR_DEBUG", "Current Avatar URL from Server: $avatarUrl")

        if (!avatarUrl.isNullOrEmpty()) {
            com.bumptech.glide.Glide.with(this)
                .load(avatarUrl)
                // 🌟 关键：跳过内存和磁盘缓存，强制从服务器拉取最新图片内容
                .skipMemoryCache(true)
                .diskCacheStrategy(com.bumptech.glide.load.engine.DiskCacheStrategy.NONE)
                .placeholder(R.drawable.ic_avatar_placeholder)
                .error(R.drawable.ic_avatar_placeholder)
                .circleCrop()
                .into(profileAvatar)
        } else {
            profileAvatar.setImageResource(R.drawable.ic_avatar_placeholder)
        }
    }

    // --- 🌟 核心修改：头像上传逻辑 🌟 ---
    private fun processPrepareAndUploadAvatar(uri: Uri) {
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val inputStream = contentResolver.openInputStream(uri)
                val bitmap = BitmapFactory.decodeStream(inputStream)
                inputStream?.close()

                bitmap?.let {
                    val outputStream = ByteArrayOutputStream()
                    var quality = 80
                    it.compress(Bitmap.CompressFormat.JPEG, quality, outputStream)

                    // 压缩逻辑（保持在 500KB 以内）
                    while (outputStream.toByteArray().size > 500 * 1024 && quality > 10) {
                        outputStream.reset()
                        quality -= 10
                        it.compress(Bitmap.CompressFormat.JPEG, quality, outputStream)
                    }

                    val imageBytes = outputStream.toByteArray()
                    val requestFile = imageBytes.toRequestBody("image/jpeg".toMediaTypeOrNull())

                    // 网页端字段名为 "file"
                    val body = MultipartBody.Part.createFormData("file", "avatar.jpg", requestFile)

                    withContext(Dispatchers.Main) {
                        val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
                        val token = prefs.getString("access_token", "") ?: ""
                        val fullToken = if (token.startsWith("Bearer ")) token else "Bearer $token"

                        android.util.Log.d("AVATAR_DEBUG", "Starting Multipart PUT...")

                        val response = NetworkClient.apiService.uploadAvatar(fullToken, body)

                        if (response.isSuccessful) {
                            android.util.Log.d("AVATAR_DEBUG", "Upload Success! Response Body: ${response.body()}")

                            // 🌟 核心改进：手动清理 Glide 内存缓存
                            com.bumptech.glide.Glide.get(this@ProfileActivity).clearMemory()

                            Toast.makeText(this@ProfileActivity, "Avatar updated successfully!", Toast.LENGTH_SHORT).show()

                            // 🌟 重新获取用户信息
                            // 由于 updateUI 已经设置了 skipMemoryCache，fetchUserProfile 后会看到新头像
                            fetchUserProfile()
                        } else {
                            val errorDetail = response.errorBody()?.string()
                            android.util.Log.e("AVATAR_ERROR", "Code: ${response.code()} Detail: $errorDetail")
                            Toast.makeText(this@ProfileActivity, "Upload failed: ${response.code()}", Toast.LENGTH_SHORT).show()
                        }
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    android.util.Log.e("AVATAR_ERROR", "Exception: ${e.message}")
                    Toast.makeText(this@ProfileActivity, "Error: ${e.message}", Toast.LENGTH_SHORT).show()
                }
            }
        }
    }

    // --- 其他 Profile 功能 (保持不变) ---
    private fun toggleEditMode() {
        if (isEditing) saveProfileChanges()
        else { enterEditMode(); isEditing = true; editBtn.text = "Save Changes" }
    }

    private fun saveProfileChanges() {
        val request = UpdateProfileRequest(
            nickname = getRealTimeValue(R.id.item_nickname),
            email = getRealTimeValue(R.id.item_email),
            location = getRealTimeValue(R.id.item_location),
            birthDate = getRealTimeValue(R.id.item_birth),
            avatar = null
        )
        lifecycleScope.launch {
            val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
            val token = "Bearer ${prefs.getString("access_token", "")}"
            try {
                val response = NetworkClient.apiService.updateUserProfile(token, request)
                if (response.isSuccessful) {
                    Toast.makeText(this@ProfileActivity, "Profile Updated!", Toast.LENGTH_SHORT).show()
                    isEditing = false
                    editBtn.text = "Edit Profile"
                    response.body()?.let { updateUI(it) }
                    exitEditMode()
                }
            } catch (e: Exception) { Toast.makeText(this@ProfileActivity, "Network Error", Toast.LENGTH_SHORT).show() }
        }
    }

    private fun getRealTimeValue(viewId: Int): String {
        val root = findViewById<View>(viewId)
        val value = if (isEditing) root.findViewById<EditText>(R.id.info_edit).text.toString() else root.findViewById<TextView>(R.id.info_value).text.toString()
        return if (value == "Not set") "" else value
    }

    private fun enterEditMode() {
        for (id in editableIds) {
            val root = findViewById<View>(id)
            root.findViewById<TextView>(R.id.info_value).visibility = View.GONE
            root.findViewById<EditText>(R.id.info_edit).apply {
                visibility = View.VISIBLE
                setText(root.findViewById<TextView>(R.id.info_value).text)
            }
        }
    }

    private fun exitEditMode() {
        for (id in editableIds) {
            val root = findViewById<View>(id)
            root.findViewById<TextView>(R.id.info_value).visibility = View.VISIBLE
            root.findViewById<EditText>(R.id.info_edit).visibility = View.GONE
        }
    }

    private fun setInfo(viewId: Int, label: String, value: String) {
        val root = findViewById<View>(viewId)
        root.findViewById<TextView>(R.id.info_label).text = label
        root.findViewById<TextView>(R.id.info_value).text = value
    }

    private fun showImageSourceDialog() {
        val options = arrayOf("Take Photo", "Choose from Gallery", "Cancel")
        AlertDialog.Builder(this).setTitle("Update Avatar")
            .setItems(options) { dialog, which ->
                when (which) {
                    0 -> openCamera()
                    1 -> pickImageLauncher.launch("image/*")
                    else -> dialog.dismiss()
                }
            }.show()
    }

    private fun openCamera() {
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.CAMERA) != PackageManager.PERMISSION_GRANTED) {
            requestCameraPermissionLauncher.launch(Manifest.permission.CAMERA)
        } else {
            val photoFile = File.createTempFile("AVATAR_", ".jpg", getExternalFilesDir(Environment.DIRECTORY_PICTURES))
            photoUri = FileProvider.getUriForFile(this, "$packageName.fileprovider", photoFile)
            takePhotoLauncher.launch(photoUri!!)
        }
    }

    private fun logout() {
        getSharedPreferences("auth_prefs", Context.MODE_PRIVATE).edit().clear().apply()
        startActivity(Intent(this, LoginActivity::class.java).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK))
        finish()
    }
}