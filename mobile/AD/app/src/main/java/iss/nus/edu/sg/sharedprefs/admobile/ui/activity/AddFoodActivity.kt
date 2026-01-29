package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.Manifest
import android.content.pm.PackageManager
import android.graphics.Bitmap
import android.net.Uri
import android.os.Bundle
import android.view.View
import android.widget.Button
import android.widget.ImageView
import android.widget.LinearLayout
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AlertDialog // 🌟 确保导包正确
import androidx.appcompat.app.AppCompatActivity
import com.google.android.material.appbar.MaterialToolbar
import iss.nus.edu.sg.sharedprefs.admobile.R

class AddFoodActivity : AppCompatActivity() {

    private lateinit var imagePreview: ImageView
    private lateinit var placeholder: LinearLayout

    // 1. 定义图片选择器 (从相册)
    private val pickImageLauncher = registerForActivityResult(ActivityResultContracts.GetContent()) { uri: Uri? ->
        uri?.let { showImage(uri) }
    }

    // 2. 定义相机启动器 (获取缩略图)
    private val takePictureLauncher = registerForActivityResult(ActivityResultContracts.TakePicturePreview()) { bitmap: Bitmap? ->
        bitmap?.let { showImageBitmap(it) }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.add_food_activity)

        imagePreview = findViewById(R.id.image_preview)
        placeholder = findViewById(R.id.add_photo_placeholder)
        val btnChoose = findViewById<Button>(R.id.choose_photo_button)

        // 设置 Toolbar
        val toolbar: MaterialToolbar = findViewById(R.id.toolbar)
        setSupportActionBar(toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        toolbar.setNavigationOnClickListener { onBackPressedDispatcher.onBackPressed() }

        // 点击按钮弹出对话框
        btnChoose.setOnClickListener { showImageSourceDialog() }

        // 🌟 新增：点击已经显示的图片也可以重新更换
        imagePreview.setOnClickListener { showImageSourceDialog() }
    }

    private fun showImageSourceDialog() {
        val options = arrayOf("Take Photo", "Choose from Gallery")
        AlertDialog.Builder(this) // 使用 androidx.appcompat.app.AlertDialog
            .setTitle("Add Food Photo")
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
            // 请求权限
            requestPermissionLauncher.launch(Manifest.permission.CAMERA)
        }
    }

    // 🌟 权限请求回调处理
    private val requestPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { isGranted: Boolean ->
        if (isGranted) {
            takePictureLauncher.launch(null)
        } else {
            Toast.makeText(this, "Camera permission is required to take photos", Toast.LENGTH_SHORT).show()
        }
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