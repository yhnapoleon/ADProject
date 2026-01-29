package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.content.Intent
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
import com.google.android.material.button.MaterialButton
import iss.nus.edu.sg.sharedprefs.admobile.R
import iss.nus.edu.sg.sharedprefs.admobile.utils.NavigationUtils
import java.io.File
import java.text.SimpleDateFormat
import java.util.*

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
        // 后续逻辑：将本地图片上传至 Azure 后端
        Toast.makeText(this, "Avatar locally updated!", Toast.LENGTH_SHORT).show()
    }
}