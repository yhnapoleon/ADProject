package iss.nus.edu.sg.sharedprefs.admobile

import android.content.Intent
import android.graphics.Color
import android.net.Uri
import android.os.Bundle
import android.provider.MediaStore
import android.view.View
import android.widget.EditText
import android.widget.ImageView
import android.widget.ProgressBar
import android.widget.TextView
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import com.bumptech.glide.Glide
import com.google.android.material.button.MaterialButton
import com.google.android.material.imageview.ShapeableImageView
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONObject
import java.io.File
import java.io.FileOutputStream
import java.text.SimpleDateFormat
import java.util.*

class ProfileActivity : AppCompatActivity() {

    private var isEditing = false
    private lateinit var editBtn: MaterialButton
    private var currentProfileData: JSONObject? = null

    // 定义参与编辑的条目 ID (Username 和 Password 不可编辑)
    private val editableIds = listOf(
        R.id.item_nickname, R.id.item_email,
        R.id.item_birth, R.id.item_location
    )

    // 图片选择器
    private val imagePickerLauncher = registerForActivityResult(ActivityResultContracts.GetContent()) { uri: Uri? ->
        uri?.let { uploadAvatar(it) }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_profile)

        // 设置状态栏颜色
        window.statusBarColor = Color.parseColor("#674fa3")

        // 2. 绑定编辑/保存按钮逻辑
        editBtn = findViewById(R.id.btn_edit_profile)
        editBtn.setOnClickListener {
            if (isEditing) {
                saveProfileChanges()
            } else {
                toggleEditMode()
            }
        }

        val historyCard = findViewById<View>(R.id.card_history)
        historyCard.setOnClickListener {
            // 创建跳转意图，从当前页面跳往 EmissionRecordsActivity
            val intent = Intent(this, EmissionRecordsActivity::class.java)
            startActivity(intent)
        }

        // 3. 初始化统一导航栏
        NavigationUtils.setupBottomNavigation(this, R.id.nav_person)

        // 4. 设置头像点击事件（点击头像可以更换）
        val avatarImageView = findViewById<ShapeableImageView>(R.id.profile_avatar)
        avatarImageView.setOnClickListener {
            imagePickerLauncher.launch("image/*")
        }
        // 添加点击提示（可选：可以通过添加一个覆盖层图标来实现）
        avatarImageView.isClickable = true
        avatarImageView.isFocusable = true

        // 5. 从后端加载用户资料
        loadProfileData()
    }

    /**
     * 从后端加载用户资料
     */
    private fun loadProfileData() {
        val token = ApiHelper.getToken(this)
        if (token == null) {
            Toast.makeText(this, "Please login first", Toast.LENGTH_SHORT).show()
            finish()
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val response = ApiHelper.executeGet(this@ProfileActivity, "/api/user/profile")
                val responseBody = response.body?.string()

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && responseBody != null) {
                        val json = JSONObject(responseBody)
                        currentProfileData = json
                        displayProfileData(json)
                    } else {
                        val errorMsg = if (responseBody != null) {
                            try {
                                JSONObject(responseBody).optString("message", "Failed to load profile")
                            } catch (e: Exception) {
                                "Failed to load profile: ${response.code}"
                            }
                        } else {
                            "Failed to load profile: ${response.code}"
                        }
                        Toast.makeText(this@ProfileActivity, errorMsg, Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@ProfileActivity, "Error: ${e.message}", Toast.LENGTH_SHORT).show()
                    android.util.Log.e("ProfileActivity", "Load profile error", e)
                }
            }
        }
    }

    /**
     * 显示用户资料数据
     */
    private fun displayProfileData(json: JSONObject) {
        val name = json.optString("name", "")
        val nickname = json.optString("nickname", "")
        val email = json.optString("email", "")
        val location = json.optString("location", "")
        val birthDate = json.optString("birthDate", "")
        val avatar = json.optString("avatar", null)
        val joinDays = json.optInt("joinDays", 0)
        val pointsTotal = json.optInt("pointsTotal", 0)

        // 更新顶部卡片
        findViewById<TextView>(R.id.profile_name).text = nickname.ifEmpty { name }
        findViewById<TextView>(R.id.profile_email).text = email
        findViewById<TextView>(R.id.profile_total_pts).text = "Total Points: $pointsTotal"
        
        // 加载头像
        val avatarImageView = findViewById<ShapeableImageView>(R.id.profile_avatar)
        val avatarUrl = ApiHelper.buildAvatarUrl(this, avatar)
        if (!avatarUrl.isNullOrEmpty()) {
            Glide.with(this)
                .load(avatarUrl)
                .placeholder(R.drawable.ic_avatar_placeholder)
                .error(R.drawable.ic_avatar_placeholder)
                .circleCrop()
                .into(avatarImageView)
        } else {
            avatarImageView.setImageResource(R.drawable.ic_avatar_placeholder)
        }

        // 格式化日期显示
        val formattedBirthDate = formatDateForDisplay(birthDate)
        val formattedJoinDate = formatJoinDate(joinDays)

        // 设置信息项（注意：Username 不可编辑，Password 需要单独处理）
        setInfo(R.id.item_username, "Username", name)
        setInfo(R.id.item_nickname, "Nickname", nickname)
        setInfo(R.id.item_email, "Email", email)
        setInfo(R.id.item_password, "Password", "••••••••") // 密码不显示，需要单独修改密码功能
        setInfo(R.id.item_birth, "Birth Date", formattedBirthDate)
        setInfo(R.id.item_location, "Location", location)
        setInfo(R.id.item_join_date, "Join Date", formattedJoinDate)
    }

    /**
     * 格式化日期显示（yyyy-MM-dd -> 可读格式）
     */
    private fun formatDateForDisplay(dateStr: String): String {
        if (dateStr.isEmpty()) return ""
        return try {
            val inputFormat = SimpleDateFormat("yyyy-MM-dd", Locale.ENGLISH)
            val outputFormat = SimpleDateFormat("MMMM dd, yyyy", Locale.ENGLISH)
            val date = inputFormat.parse(dateStr)
            date?.let { outputFormat.format(it) } ?: dateStr
        } catch (e: Exception) {
            dateStr
        }
    }

    /**
     * 格式化加入日期（根据天数计算）
     */
    private fun formatJoinDate(joinDays: Int): String {
        val calendar = Calendar.getInstance()
        calendar.add(Calendar.DAY_OF_YEAR, -joinDays)
        val outputFormat = SimpleDateFormat("MMMM dd, yyyy", Locale.ENGLISH)
        return outputFormat.format(calendar.time)
    }

    /**
     * 核心逻辑：切换编辑与展示模式
     */
    private fun toggleEditMode() {
        isEditing = !isEditing

        for (id in editableIds) {
            val itemView = findViewById<View>(id)
            val tvValue = itemView.findViewById<TextView>(R.id.info_value)
            val etEdit = itemView.findViewById<EditText>(R.id.info_edit)

            if (isEditing) {
                // 进入编辑状态：显示输入框，填入当前值，隐藏文本
                // 对于日期，需要转换回 yyyy-MM-dd 格式
                val editValue = when (id) {
                    R.id.item_birth -> {
                        // 将显示格式转换回 yyyy-MM-dd
                        val displayText = tvValue.text.toString()
                        if (displayText.isNotEmpty()) {
                            convertDisplayDateToApiFormat(displayText)
                        } else {
                            // 如果显示文本为空，尝试从原始数据获取
                            currentProfileData?.optString("birthDate", "") ?: ""
                        }
                    }
                    else -> {
                        val displayText = tvValue.text.toString()
                        if (displayText.isNotEmpty()) {
                            displayText
                        } else {
                            // 如果显示文本为空，尝试从原始数据获取
                            when (id) {
                                R.id.item_nickname -> currentProfileData?.optString("nickname", "") ?: ""
                                R.id.item_email -> currentProfileData?.optString("email", "") ?: ""
                                R.id.item_location -> currentProfileData?.optString("location", "") ?: ""
                                else -> ""
                            }
                        }
                    }
                }
                etEdit.setText(editValue)
                etEdit.hint = tvValue.text.toString() // 设置提示文本
                etEdit.visibility = View.VISIBLE
                tvValue.visibility = View.GONE
            } else {
                // 取消编辑：隐藏输入框，显示文本
                etEdit.visibility = View.GONE
                tvValue.visibility = View.VISIBLE
            }
        }

        // 修改按钮文案反馈
        editBtn.text = if (isEditing) "Save Changes" else "Edit Profile"
    }

    /**
     * 将显示格式的日期转换回 API 格式（yyyy-MM-dd）
     */
    private fun convertDisplayDateToApiFormat(displayDate: String): String {
        if (displayDate.isEmpty()) return ""
        return try {
            val inputFormat = SimpleDateFormat("MMMM dd, yyyy", Locale.ENGLISH)
            val outputFormat = SimpleDateFormat("yyyy-MM-dd", Locale.ENGLISH)
            val date = inputFormat.parse(displayDate)
            date?.let { outputFormat.format(it) } ?: displayDate
        } catch (e: Exception) {
            // 如果解析失败，尝试其他格式或返回原值
            displayDate
        }
    }

    /**
     * 保存资料更改到后端
     */
    private fun saveProfileChanges() {
        val token = ApiHelper.getToken(this)
        if (token == null) {
            Toast.makeText(this, "Please login first", Toast.LENGTH_SHORT).show()
            return
        }

        // 收集编辑后的数据
        val nickname = getEditTextValue(R.id.item_nickname)
        val email = getEditTextValue(R.id.item_email)
        val birthDate = getEditTextValue(R.id.item_birth)
        val location = getEditTextValue(R.id.item_location)

        // 转换日期格式
        val formattedBirthDate = if (birthDate.isNotEmpty()) {
            // 如果已经是 yyyy-MM-dd 格式，直接使用；否则尝试转换
            if (birthDate.matches(Regex("\\d{4}-\\d{2}-\\d{2}"))) {
                birthDate
            } else {
                convertDisplayDateToApiFormat(birthDate)
            }
        } else null

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val json = JSONObject().apply {
                    if (nickname.isNotEmpty()) put("nickname", nickname)
                    if (email.isNotEmpty()) put("email", email)
                    if (formattedBirthDate != null) put("birthDate", formattedBirthDate)
                    if (location.isNotEmpty()) put("location", location)
                }

                val response = ApiHelper.executePut(this@ProfileActivity, "/api/user/profile", json.toString())
                val responseBody = response.body?.string()

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && responseBody != null) {
                        val jsonResponse = JSONObject(responseBody)
                        currentProfileData = jsonResponse
                        displayProfileData(jsonResponse)
                        toggleEditMode() // 退出编辑模式
                        Toast.makeText(this@ProfileActivity, "Profile updated successfully!", Toast.LENGTH_SHORT).show()
                    } else {
                        val errorMsg = if (responseBody != null) {
                            try {
                                JSONObject(responseBody).optString("message", "Failed to update profile")
                            } catch (e: Exception) {
                                "Failed to update profile: ${response.code}"
                            }
                        } else {
                            "Failed to update profile: ${response.code}"
                        }
                        Toast.makeText(this@ProfileActivity, errorMsg, Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@ProfileActivity, "Error: ${e.message}", Toast.LENGTH_SHORT).show()
                    android.util.Log.e("ProfileActivity", "Update profile error", e)
                }
            }
        }
    }

    /**
     * 获取编辑框的值
     */
    private fun getEditTextValue(itemId: Int): String {
        val itemView = findViewById<View>(itemId)
        val etEdit = itemView.findViewById<EditText>(R.id.info_edit)
        return etEdit.text.toString().trim()
    }

    private fun setInfo(viewId: Int, label: String, value: String) {
        val root = findViewById<View>(viewId)
        root.findViewById<TextView>(R.id.info_label).text = label
        root.findViewById<TextView>(R.id.info_value).text = value
    }

    /**
     * 上传头像
     */
    private fun uploadAvatar(uri: Uri) {
        val token = ApiHelper.getToken(this)
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
                
                // 上传文件
                val response = ApiHelper.executeUploadFile(this@ProfileActivity, "/api/user/avatar", tempFile)
                val responseBody = response.body?.string()

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && responseBody != null) {
                        try {
                            val jsonResponse = JSONObject(responseBody)
                            val avatarUrl = jsonResponse.optString("avatarUrl", null) 
                                ?: jsonResponse.optString("avatar", null)
                            
                            // 重新加载用户资料以更新头像
                            loadProfileData()
                            Toast.makeText(this@ProfileActivity, "Avatar updated successfully!", Toast.LENGTH_SHORT).show()
                        } catch (e: Exception) {
                            Toast.makeText(this@ProfileActivity, "Avatar updated, but failed to parse response", Toast.LENGTH_SHORT).show()
                            loadProfileData() // 仍然重新加载资料
                        }
                    } else {
                        val errorMsg = if (responseBody != null) {
                            try {
                                JSONObject(responseBody).optString("message", "Failed to upload avatar")
                            } catch (e: Exception) {
                                "Failed to upload avatar: ${response.code}"
                            }
                        } else {
                            "Failed to upload avatar: ${response.code}"
                        }
                        Toast.makeText(this@ProfileActivity, errorMsg, Toast.LENGTH_SHORT).show()
                    }
                }
                
                // 清理临时文件
                tempFile.delete()
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@ProfileActivity, "Error: ${e.message}", Toast.LENGTH_SHORT).show()
                    android.util.Log.e("ProfileActivity", "Upload avatar error", e)
                }
            }
        }
    }
}