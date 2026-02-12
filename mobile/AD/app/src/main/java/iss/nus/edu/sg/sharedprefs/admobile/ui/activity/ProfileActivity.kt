package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.Manifest
import android.app.DatePickerDialog
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
import android.util.Log
import android.view.View
import android.view.ViewGroup
import android.widget.*
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.core.content.FileProvider
import androidx.lifecycle.lifecycleScope
import com.bumptech.glide.Glide
import com.bumptech.glide.load.engine.DiskCacheStrategy
import com.bumptech.glide.signature.ObjectKey
import com.google.android.material.button.MaterialButton
import iss.nus.edu.sg.sharedprefs.admobile.R
import iss.nus.edu.sg.sharedprefs.admobile.data.model.*
import iss.nus.edu.sg.sharedprefs.admobile.data.network.NetworkClient
import iss.nus.edu.sg.sharedprefs.admobile.utils.NavigationUtils
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import okhttp3.MediaType.Companion.toMediaTypeOrNull
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.toRequestBody
import java.io.ByteArrayOutputStream
import java.io.File
import java.util.*
import android.text.InputFilter

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
            profileAvatar.setImageURI(it) // 选图后立刻本地预览
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
        setupLocationSpinner()

        profileAvatar.setOnClickListener { showImageSourceDialog() }
        editBtn.setOnClickListener { toggleEditMode() }

        findViewById<MaterialButton>(R.id.btn_change_password).setOnClickListener { showStep1OldPasswordDialog() }
        findViewById<MaterialButton>(R.id.btn_logout).setOnClickListener { logout() }
        findViewById<View>(R.id.card_history).setOnClickListener {
            startActivity(Intent(this, EmissionRecordsActivity::class.java))
        }

        NavigationUtils.setupBottomNavigation(this, R.id.nav_person)
    }

    private fun setupLocationSpinner() {
        val root = findViewById<View>(R.id.item_location)
        val spinner = root.findViewById<Spinner>(R.id.info_spinner)
        ArrayAdapter.createFromResource(
            this,
            R.array.regions_array,
            android.R.layout.simple_spinner_item
        ).also { adapter ->
            adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
            spinner.adapter = adapter
        }
    }

    private fun showDatePicker(editText: EditText) {
        val calendar = Calendar.getInstance()
        val dialog = DatePickerDialog(this, { _, year, month, day ->
            val date = String.format("%04d-%02d-%02d", year, month + 1, day)
            editText.setText(date)
        }, calendar.get(Calendar.YEAR), calendar.get(Calendar.MONTH), calendar.get(Calendar.DAY_OF_MONTH))
        dialog.datePicker.maxDate = calendar.timeInMillis
        dialog.show()
    }

    // ---Profile 数据逻辑 ---

    private fun fetchUserProfile() {
        lifecycleScope.launch {
            val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
            val token = "Bearer ${prefs.getString("access_token", "")}"
            try {
                val response = NetworkClient.apiService.getUserProfile(token)
                if (response.isSuccessful && response.body() != null) {
                    updateUI(response.body()!!)
                }
            } catch (e: Exception) {
                Log.e("PROFILE_DEBUG", "Fetch Profile Error: ${e.message}")
                Toast.makeText(this@ProfileActivity, "Load failed", Toast.LENGTH_SHORT).show()
            }
        }
    }

    private fun updateUI(p: UserProfileResponse) {
        Log.d("PROFILE_DEBUG", "Profile Data Received. Avatar: ${p.avatar}")

        findViewById<TextView>(R.id.profile_name).text = p.nickname ?: p.name
        findViewById<TextView>(R.id.profile_email).text = p.email
        findViewById<TextView>(R.id.profile_total_pts).text = "Total Points: ${p.pointsTotal}"

        setInfo(R.id.item_username, "Username", p.name)
        setInfo(R.id.item_nickname, "Nickname", p.nickname ?: "Not set")
        setInfo(R.id.item_email, "Email", p.email ?: "Not set")
        setInfo(R.id.item_birth, "Birth Date", p.birthDate ?: "Not set")
        setInfo(R.id.item_location, "Location", p.location ?: "Not set")
        setInfo(R.id.item_join_date, "Join Days", "${p.joinDays} Days")

        // 使用封装好的加载逻辑
        loadAvatarWithGlide(p.avatar)
    }

    /**
     * 封装 Glide 加载逻辑，解决 localhost、缓存和 Null 问题
     */
    private fun loadAvatarWithGlide(url: String?) {
        if (url.isNullOrEmpty() || url == "null") {
            Log.w("PROFILE_DEBUG", "Avatar URL is null or empty, setting placeholder.")
            profileAvatar.setImageResource(R.drawable.ic_avatar_placeholder)
            return
        }

        // 处理模拟器 IP 替换
        val finalUrl = url.replace("localhost", "10.0.2.2")
        Log.d("PROFILE_DEBUG", "Glide Loading Final URL: $finalUrl")

        Glide.with(this)
            .load(finalUrl)
            .signature(ObjectKey(System.currentTimeMillis().toString())) // 强制刷新，防止看到 500 报错的缓存图
            .skipMemoryCache(true)
            .diskCacheStrategy(DiskCacheStrategy.NONE)
            .placeholder(R.drawable.ic_avatar_placeholder)
            .error(R.drawable.ic_avatar_placeholder)
            .circleCrop()
            .into(profileAvatar)
    }

    private fun processPrepareAndUploadAvatar(uri: Uri) {
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val inputStream = contentResolver.openInputStream(uri)
                val bitmap = BitmapFactory.decodeStream(inputStream)
                inputStream?.close()

                bitmap?.let {
                    val outputStream = ByteArrayOutputStream()
                    it.compress(Bitmap.CompressFormat.JPEG, 80, outputStream)
                    val imageBytes = outputStream.toByteArray()
                    val requestFile = imageBytes.toRequestBody("image/jpeg".toMediaTypeOrNull())
                    val body = MultipartBody.Part.createFormData("file", "avatar.jpg", requestFile)

                    withContext(Dispatchers.Main) {
                        val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
                        val token = prefs.getString("access_token", "") ?: ""
                        val fullToken = if (token.startsWith("Bearer ")) token else "Bearer $token"

                        val response = NetworkClient.apiService.uploadAvatar(fullToken, body)
                        if (response.isSuccessful && response.body() != null) {
                            val newUrl = response.body()?.avatarUrl ?: response.body()?.avatar
                            Log.d("UPLOAD_DEBUG", "Upload Success! New URL: $newUrl")

                            loadAvatarWithGlide(newUrl)

                            Toast.makeText(this@ProfileActivity, "Avatar updated successfully!", Toast.LENGTH_SHORT).show()

                            // 延时刷新全量数据，给 Azure 数据库同步留一点时间
                            delay(1000)
                            fetchUserProfile()
                        } else {
                            val errorBody = response.errorBody()?.string()
                            Log.e("UPLOAD_DEBUG", "Upload Failed Code: ${response.code()}, Body: $errorBody")
                            Toast.makeText(this@ProfileActivity, "Upload failed: ${response.code()}", Toast.LENGTH_SHORT).show()
                        }
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Log.e("UPLOAD_DEBUG", "Exception: ${e.message}")
                    Toast.makeText(this@ProfileActivity, "Error: ${e.message}", Toast.LENGTH_SHORT).show()
                }
            }
        }
    }

    // --- 资料编辑 & 密码修改逻辑 (保持原样) ---

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
                    isEditing = false
                    editBtn.text = "Edit Profile"
                    response.body()?.let { updateUI(it) }
                    exitEditMode()
                }
            } catch (e: Exception) { Log.e("PROFILE_DEBUG", "Update failed: ${e.message}") }
        }
    }

    private fun getRealTimeValue(viewId: Int): String {
        val root = findViewById<View>(viewId)
        val value = if (isEditing) {
            if (viewId == R.id.item_location) root.findViewById<Spinner>(R.id.info_spinner).selectedItem.toString()
            else root.findViewById<EditText>(R.id.info_edit).text.toString()
        } else {
            root.findViewById<TextView>(R.id.info_value).text.toString()
        }
        return if (value == "Not set" || value == "null") "" else value
    }

    private fun enterEditMode() {
        for (id in editableIds) {
            val root = findViewById<View>(id)
            root.findViewById<TextView>(R.id.info_value).visibility = View.GONE
            if (id == R.id.item_location) {
                root.findViewById<Spinner>(R.id.info_spinner).visibility = View.VISIBLE
            } else {
                val et = root.findViewById<EditText>(R.id.info_edit)
                et.visibility = View.VISIBLE
                et.setText(root.findViewById<TextView>(R.id.info_value).text)
                if (id == R.id.item_birth) et.setOnClickListener { showDatePicker(et) }
            }
        }
    }

    private fun exitEditMode() {
        for (id in editableIds) {
            val root = findViewById<View>(id)
            root.findViewById<TextView>(R.id.info_value).visibility = View.VISIBLE
            root.findViewById<EditText>(R.id.info_edit).visibility = View.GONE
            root.findViewById<Spinner>(R.id.info_spinner)?.visibility = View.GONE
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

    private fun showStep1OldPasswordDialog() {
        val etOld = EditText(this).apply {
            hint = "Enter Current Password"
            inputType = InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_PASSWORD
        }
        val container = FrameLayout(this).apply {
            val params = FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT)
            params.setMargins(60, 40, 60, 0)
            addView(etOld, params)
        }
        AlertDialog.Builder(this)
            .setTitle("Identity Verification")
            .setView(container)
            .setPositiveButton("Next") { _, _ ->
                val oldPwd = etOld.text.toString()
                if (oldPwd.isNotEmpty()) verifyOldPasswordAndProceed(oldPwd, null)
            }
            .setNegativeButton("Cancel", null)
            .show()
    }

    private fun verifyOldPasswordAndProceed(oldPwd: String, step1Dialog: AlertDialog?) {
        lifecycleScope.launch {
            val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
            val token = "Bearer ${prefs.getString("access_token", "")}"
            try {
                val response = NetworkClient.apiService.verifyPassword(token, VerifyPasswordRequest(oldPwd))
                if (response.isSuccessful && response.body()?.valid == true) {
                    showStep2NewPasswordDialog(oldPwd)
                } else {
                    Toast.makeText(this@ProfileActivity, "Invalid password", Toast.LENGTH_SHORT).show()
                }
            } catch (e: Exception) { Log.e("PROFILE_DEBUG", "Verify Error: ${e.message}") }
        }
    }

    private fun showStep2NewPasswordDialog(oldPwd: String) {
        val dialogView = layoutInflater.inflate(R.layout.dialog_change_password_step2, null)
        val etNew = dialogView.findViewById<EditText>(R.id.et_new_password)
        val etConfirm = dialogView.findViewById<EditText>(R.id.et_confirm_password)
        val tvStrength = dialogView.findViewById<TextView>(R.id.tv_password_strength)
        val tvMatchError = dialogView.findViewById<TextView>(R.id.tv_match_error)

        // 限制最大长度 20 位 (参考 RegisterActivity)
        val filterArray = arrayOf<InputFilter>(InputFilter.LengthFilter(20))
        etNew.filters = filterArray
        etConfirm.filters = filterArray

        val dialog = AlertDialog.Builder(this)
            .setTitle("Set New Password")
            .setView(dialogView)
            .setPositiveButton("Confirm", null)
            .setNegativeButton("Cancel", null)
            .create()

        dialog.show()

        val confirmBtn = dialog.getButton(AlertDialog.BUTTON_POSITIVE)
        confirmBtn.isEnabled = false

        val validator = object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {}
            override fun afterTextChanged(s: Editable?) {
                val newPwd = etNew.text.toString()
                val confirmPwd = etConfirm.text.toString()

                // 1. 实时强度判断 (完全同步 RegisterActivity 逻辑)
                var score = 0
                if (newPwd.length in 8..20) score++
                if (newPwd.length > 12) score++
                if (newPwd.any { it.isDigit() }) score++
                if (newPwd.any { it.isUpperCase() }) score++
                if (newPwd.any { !it.isLetterOrDigit() }) score++

                when {
                    newPwd.isEmpty() -> {
                        tvStrength.text = ""
                    }
                    newPwd.length < 8 -> {
                        tvStrength.text = "Too short (Min 8)"
                        tvStrength.setTextColor(Color.parseColor("#FF5252")) // 红色
                    }
                    score <= 2 -> {
                        tvStrength.text = "Strength: Medium"
                        tvStrength.setTextColor(Color.parseColor("#FFC107")) // 黄色
                    }
                    else -> {
                        tvStrength.text = "Strength: Strong"
                        tvStrength.setTextColor(Color.parseColor("#4CAF50")) // 绿色
                    }
                }

                // 2. 实时匹配判断
                val isMatch = newPwd == confirmPwd && confirmPwd.isNotEmpty()
                if (confirmPwd.isEmpty()) {
                    tvMatchError.text = ""
                } else if (isMatch) {
                    tvMatchError.text = "Passwords match"
                    tvMatchError.setTextColor(Color.parseColor("#4CAF50"))
                } else {
                    tvMatchError.text = "Passwords do not match"
                    tvMatchError.setTextColor(Color.parseColor("#FF5252"))
                }

                // 3. 只有长度达标 (>=8) 且 两次输入一致时才启用按钮
                confirmBtn.isEnabled = newPwd.length >= 8 && isMatch
            }
        }

        etNew.addTextChangedListener(validator)
        etConfirm.addTextChangedListener(validator)

        confirmBtn.setOnClickListener {
            performActualPasswordUpdate(oldPwd, etNew.text.toString())
            dialog.dismiss()
        }
    }

    private fun performActualPasswordUpdate(old: String, new: String) {
        lifecycleScope.launch {
            val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
            val token = "Bearer ${prefs.getString("access_token", "")}"
            try {
                val response = NetworkClient.apiService.changePassword(token, ChangePasswordRequest(old, new))
                if (response.isSuccessful) Toast.makeText(this@ProfileActivity, "Updated!", Toast.LENGTH_SHORT).show()
            } catch (e: Exception) { Log.e("PROFILE_DEBUG", "Update Pwd Error: ${e.message}") }
        }
    }

    private fun logout() {
        getSharedPreferences("auth_prefs", Context.MODE_PRIVATE).edit().clear().apply()
        startActivity(Intent(this, LoginActivity::class.java).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK))
        finish()
    }
}