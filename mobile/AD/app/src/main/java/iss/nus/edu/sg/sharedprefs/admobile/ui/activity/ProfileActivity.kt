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

        // 🌟 修改：分步修改密码
        findViewById<MaterialButton>(R.id.btn_change_password).setOnClickListener { showStep1OldPasswordDialog() }

        findViewById<MaterialButton>(R.id.btn_logout).setOnClickListener { logout() }
        findViewById<View>(R.id.card_history).setOnClickListener {
            startActivity(Intent(this, EmissionRecordsActivity::class.java))
        }

        NavigationUtils.setupBottomNavigation(this, R.id.nav_person)
    }

    // --- 修改密码分步逻辑 ---

    /**
     * 第一步：输入旧密码弹窗
     */
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
                if (oldPwd.isNotEmpty()) {
                    showStep2NewPasswordDialog(oldPwd)
                } else {
                    Toast.makeText(this, "Old password is required", Toast.LENGTH_SHORT).show()
                }
            }
            .setNegativeButton("Cancel", null)
            .show()
    }

    /**
     * 第二步：输入新密码弹窗（包含强度和一致性校验）
     */
    private fun showStep2NewPasswordDialog(oldPwd: String) {
        val dialogView = layoutInflater.inflate(R.layout.dialog_change_password_step2, null)
        val etNew = dialogView.findViewById<EditText>(R.id.et_new_password)
        val etConfirm = dialogView.findViewById<EditText>(R.id.et_confirm_password)
        val tvStrength = dialogView.findViewById<TextView>(R.id.tv_password_strength)

        val dialog = AlertDialog.Builder(this)
            .setTitle("Set New Password")
            .setView(dialogView)
            .setPositiveButton("Confirm", null) // 后面重写点击事件
            .setNegativeButton("Back", null)
            .create()

        // 1. 实时密码强度检测
        etNew.addTextChangedListener(object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {
                val pwd = s.toString()
                when {
                    pwd.isEmpty() -> tvStrength.text = ""
                    pwd.length < 6 -> {
                        tvStrength.text = "Strength: Weak"
                        tvStrength.setTextColor(Color.RED)
                    }
                    pwd.length < 10 -> {
                        tvStrength.text = "Strength: Medium"
                        tvStrength.setTextColor(Color.parseColor("#FFA500"))
                    }
                    else -> {
                        tvStrength.text = "Strength: Strong"
                        tvStrength.setTextColor(Color.parseColor("#2E7D32"))
                    }
                }
            }
            override fun afterTextChanged(s: Editable?) {}
        })

        // 2. 实时一致性校验
        etConfirm.addTextChangedListener(object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {
                if (s.toString() != etNew.text.toString() && s!!.isNotEmpty()) {
                    etConfirm.error = "Passwords do not match!"
                } else {
                    etConfirm.error = null
                }
            }
            override fun afterTextChanged(s: Editable?) {}
        })

        dialog.show()

        // 3. 重写 Confirm 按钮点击事件，防止不合规时弹窗关闭
        dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener {
            val newPwd = etNew.text.toString()
            val confirmPwd = etConfirm.text.toString()

            if (newPwd.length < 6) {
                etNew.error = "At least 6 characters"
                return@setOnClickListener
            }
            if (newPwd != confirmPwd) {
                etConfirm.error = "Passwords do not match!"
                return@setOnClickListener
            }

            // 全部通过，执行 API 调用
            performActualPasswordUpdate(oldPwd, newPwd, dialog)
        }
    }

    /**
     * 调用后端接口执行修改
     */
    private fun performActualPasswordUpdate(old: String, new: String, dialog: AlertDialog) {
        lifecycleScope.launch {
            val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
            val token = "Bearer ${prefs.getString("access_token", "")}"
            val request = ChangePasswordRequest(oldPassword = old, newPassword = new)

            try {
                val response = NetworkClient.apiService.changePassword(token, request)
                if (response.isSuccessful) {
                    Toast.makeText(this@ProfileActivity, "Password successfully updated!", Toast.LENGTH_SHORT).show()
                    dialog.dismiss()
                } else {
                    val errorBody = response.errorBody()?.string()
                    val msg = if (response.code() == 401) "Old password is incorrect" else "Error: ${response.code()}"
                    Toast.makeText(this@ProfileActivity, msg, Toast.LENGTH_LONG).show()
                }
            } catch (e: Exception) {
                Toast.makeText(this@ProfileActivity, "Network Error: ${e.message}", Toast.LENGTH_SHORT).show()
            }
        }
    }

    // --- 以下是原有的 Profile 逻辑 (保持不变) ---

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
                Toast.makeText(this@ProfileActivity, "Load failed", Toast.LENGTH_SHORT).show()
            }
        }
    }

    private fun updateUI(p: UserProfileResponse) {
        findViewById<TextView>(R.id.profile_name).text = p.nickname ?: p.name
        findViewById<TextView>(R.id.profile_email).text = p.email

        setInfo(R.id.item_username, "Username", p.name)
        setInfo(R.id.item_nickname, "Nickname", p.nickname ?: "Not set")
        setInfo(R.id.item_email, "Email", p.email ?: "Not set")
        setInfo(R.id.item_birth, "Birth Date", p.birthDate ?: "Not set")
        setInfo(R.id.item_location, "Location", p.location ?: "Not set")
        setInfo(R.id.item_join_date, "Join Days", "${p.joinDays} Days")
        setInfo(R.id.item_password, "Password", "••••••••")

        p.avatar?.let { base64Str ->
            if (base64Str.isNotEmpty()) {
                try {
                    val imageBytes = Base64.decode(base64Str, Base64.DEFAULT)
                    val bitmap = BitmapFactory.decodeByteArray(imageBytes, 0, imageBytes.size)
                    profileAvatar.setImageBitmap(bitmap)
                } catch (e: Exception) {
                    profileAvatar.setImageResource(R.drawable.ic_avatar_placeholder)
                }
            }
        } ?: profileAvatar.setImageResource(R.drawable.ic_avatar_placeholder)
    }

    private fun toggleEditMode() {
        if (isEditing) {
            saveProfileChanges()
        } else {
            enterEditMode()
            isEditing = true
            editBtn.text = "Save Changes"
        }
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
            } catch (e: Exception) {
                Toast.makeText(this@ProfileActivity, "Network Error", Toast.LENGTH_SHORT).show()
            }
        }
    }

    private fun processPrepareAndUploadAvatar(uri: Uri) {
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val inputStream = contentResolver.openInputStream(uri)
                val originalBitmap = BitmapFactory.decodeStream(inputStream)
                inputStream?.close()

                originalBitmap?.let {
                    val outputStream = ByteArrayOutputStream()
                    var quality = 80
                    it.compress(Bitmap.CompressFormat.JPEG, quality, outputStream)

                    while (outputStream.toByteArray().size > 500 * 1024 && quality > 10) {
                        outputStream.reset()
                        quality -= 10
                        it.compress(Bitmap.CompressFormat.JPEG, quality, outputStream)
                    }

                    val base64Avatar = Base64.encodeToString(outputStream.toByteArray(), Base64.NO_WRAP)

                    withContext(Dispatchers.Main) {
                        val request = UpdateProfileRequest(
                            nickname = getRealTimeValue(R.id.item_nickname),
                            email = getRealTimeValue(R.id.item_email),
                            location = getRealTimeValue(R.id.item_location),
                            birthDate = getRealTimeValue(R.id.item_birth),
                            avatar = base64Avatar
                        )
                        val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
                        val token = "Bearer ${prefs.getString("access_token", "")}"
                        val response = NetworkClient.apiService.updateUserProfile(token, request)
                        if (response.isSuccessful) {
                            Toast.makeText(this@ProfileActivity, "Avatar updated!", Toast.LENGTH_SHORT).show()
                            response.body()?.let { updateUI(it) }
                        }
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) { Toast.makeText(this@ProfileActivity, "Error", Toast.LENGTH_SHORT).show() }
            }
        }
    }

    private fun getRealTimeValue(viewId: Int): String {
        val root = findViewById<View>(viewId)
        val value = if (isEditing) {
            root.findViewById<EditText>(R.id.info_edit).text.toString()
        } else {
            root.findViewById<TextView>(R.id.info_value).text.toString()
        }
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