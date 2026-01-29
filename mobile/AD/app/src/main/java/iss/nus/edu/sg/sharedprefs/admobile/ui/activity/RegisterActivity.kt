package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.app.DatePickerDialog
import android.content.Intent
import android.graphics.Color
import android.os.Bundle
import android.text.Editable
import android.text.TextWatcher
import android.util.Log
import android.util.Patterns
import android.view.View
import android.widget.*
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import iss.nus.edu.sg.sharedprefs.admobile.R
import iss.nus.edu.sg.sharedprefs.admobile.data.model.RegisterRequestDto
import iss.nus.edu.sg.sharedprefs.admobile.data.repository.AuthRepository
import kotlinx.coroutines.launch
import java.util.*

class RegisterActivity : AppCompatActivity() {

    private val authRepository by lazy { AuthRepository(this) }
    private var selectedBirthDate: String = ""

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.register_activity)

        // 1. 视图绑定
        val editName = findViewById<EditText>(R.id.editName)
        val editEmail = findViewById<EditText>(R.id.editEmail)
        val editBirthDate = findViewById<EditText>(R.id.editBirthDate)
        val editPassword = findViewById<EditText>(R.id.editPassword)
        val editConfirmPassword = findViewById<EditText>(R.id.editConfirmPassword)
        val spinnerRegion = findViewById<Spinner>(R.id.spinnerRegion)
        val btnCreateAccount = findViewById<Button>(R.id.btnCreateAccount)
        val textLogin = findViewById<TextView>(R.id.textLogin)
        val tvPasswordStrength = findViewById<TextView>(R.id.tvPasswordStrength)

        // 2. 初始化地区选择器
        ArrayAdapter.createFromResource(
            this,
            R.array.regions_array,
            android.R.layout.simple_spinner_item
        ).also { adapter ->
            adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
            spinnerRegion.adapter = adapter
        }

        // 3. 设置日期选择器
        editBirthDate.isFocusable = false
        editBirthDate.setOnClickListener {
            val calendar = Calendar.getInstance()
            DatePickerDialog(this, { _, year, month, day ->
                selectedBirthDate = String.format("%04d-%02d-%02d", year, month + 1, day)
                editBirthDate.setText(selectedBirthDate)
            }, calendar.get(Calendar.YEAR) - 20, calendar.get(Calendar.MONTH), calendar.get(Calendar.DAY_OF_MONTH)).show()
        }

        // 🌟 4. 实时监听密码强度
        editPassword.addTextChangedListener(object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {}
            override fun afterTextChanged(s: Editable?) {
                val password = s.toString()
                if (password.isEmpty()) {
                    tvPasswordStrength.visibility = View.GONE
                } else {
                    tvPasswordStrength.visibility = View.VISIBLE
                    updatePasswordStrengthUI(password, tvPasswordStrength)
                }
            }
        })

        // 5. 注册逻辑
        btnCreateAccount.setOnClickListener {
            Log.d("API_CHECK", "1. Register Button Clicked")

            val name = editName.text.toString().trim()
            val email = editEmail.text.toString().trim()
            val password = editPassword.text.toString()
            val confirmPassword = editConfirmPassword.text.toString()
            // 🌟 修改变量名为 region
            val regionValue = spinnerRegion.selectedItem.toString()

            // --- 数据校验开始 ---
            if (name.isEmpty() || email.isEmpty() || selectedBirthDate.isEmpty() || password.isEmpty() || regionValue.isEmpty()) {
                Log.d("API_CHECK", "2. Validation Failed: Empty fields")
                Toast.makeText(this, "Please fill in all fields", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            if (!Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
                Log.d("API_CHECK", "2. Validation Failed: Email format invalid ($email)")
                Toast.makeText(this, "Invalid email format", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            if (password.length < 8) {
                Log.d("API_CHECK", "2. Validation Failed: Password too short")
                editPassword.error = "Min 8 characters"
                return@setOnClickListener
            }

            if (password != confirmPassword) {
                Log.d("API_CHECK", "2. Validation Failed: Password mismatch")
                Toast.makeText(this, "Passwords do not match", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }
            // --- 数据校验结束 ---

            Log.d("API_CHECK", "3. All checks passed. Region to send: $regionValue")

            // 🌟 构造 DTO，使用新的字段名
            val request = RegisterRequestDto(
                username = name,
                email = email,
                password = password,
                birthDate = selectedBirthDate,
                region = regionValue
            )

            lifecycleScope.launch {
                Log.d("API_CHECK", "4. Launching Coroutine...")
                try {
                    val result = authRepository.register(request)

                    result.onSuccess {
                        Log.d("API_CHECK", "5. API SUCCESS!")
                        Toast.makeText(this@RegisterActivity, "Account created!", Toast.LENGTH_SHORT).show()
                        val intent = Intent(this@RegisterActivity, MainActivity::class.java)
                        intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
                        startActivity(intent)
                        finish()
                    }.onFailure { exception ->
                        // 🌟 打印后端返回的 JSON 详情
                        Log.e("API_CHECK", "5. API FAILURE: ${exception.message}")

                        // 处理常见的后端校验错误提示
                        val errorMsg = exception.message ?: ""
                        val displayMsg = when {
                            errorMsg.contains("Region") -> "Please select a valid region."
                            errorMsg.contains("DuplicateEmail") -> "Email already exists."
                            else -> "Registration failed: ${exception.message}"
                        }
                        Toast.makeText(this@RegisterActivity, displayMsg, Toast.LENGTH_LONG).show()
                    }
                } catch (e: Exception) {
                    Log.e("API_CHECK", "5. CRITICAL ERROR: ${e.localizedMessage}")
                }
            }
        }

        textLogin.setOnClickListener { finish() }
    }

    /**
     * 🌟 计算密码强度并更新 UI
     */
    private fun updatePasswordStrengthUI(password: String, textView: TextView) {
        var score = 0

        // 🌟 长度作为基础分
        if (password.length in 8..20) score++
        if (password.length > 12) score++ // 超长密码额外加分

        if (password.any { it.isDigit() }) score++
        if (password.any { it.isUpperCase() }) score++
        if (password.any { !it.isLetterOrDigit() }) score++

        when {
            // 如果长度不足 8 位，强制显示为 Weak
            password.length < 8 -> {
                textView.text = "Too short (Min 8)"
                textView.setTextColor(Color.parseColor("#FF5252"))
            }
            score <= 2 -> {
                textView.text = "Strength: Medium"
                textView.setTextColor(Color.parseColor("#FFC107"))
            }
            else -> {
                textView.text = "Strength: Strong"
                textView.setTextColor(Color.parseColor("#4CAF50"))
            }
        }
    }
}