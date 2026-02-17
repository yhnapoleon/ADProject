package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.app.DatePickerDialog
import android.content.Intent
import android.graphics.Color
import android.os.Bundle
import android.text.Editable
import android.text.InputFilter
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

        val editName = findViewById<EditText>(R.id.editName)
        val editEmail = findViewById<EditText>(R.id.editEmail)
        val editBirthDate = findViewById<EditText>(R.id.editBirthDate)
        val editPassword = findViewById<EditText>(R.id.editPassword)
        val editConfirmPassword = findViewById<EditText>(R.id.editConfirmPassword)
        val spinnerRegion = findViewById<Spinner>(R.id.spinnerRegion)
        val btnCreateAccount = findViewById<Button>(R.id.btnCreateAccount)
        val textLogin = findViewById<TextView>(R.id.textLogin)
        val tvPasswordStrength = findViewById<TextView>(R.id.tvPasswordStrength)

        //  1. 限制密码最大长度为 20 位
        val filterArray = arrayOf<InputFilter>(InputFilter.LengthFilter(20))
        editPassword.filters = filterArray
        editConfirmPassword.filters = filterArray

        ArrayAdapter.createFromResource(
            this,
            R.array.regions_array,
            android.R.layout.simple_spinner_item
        ).also { adapter ->
            adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
            spinnerRegion.adapter = adapter
        }

        editBirthDate.isFocusable = false
        editBirthDate.setOnClickListener {
            val calendar = Calendar.getInstance()
            DatePickerDialog(this, { _, year, month, day ->
                selectedBirthDate = String.format("%04d-%02d-%02d", year, month + 1, day)
                editBirthDate.setText(selectedBirthDate)
            }, calendar.get(Calendar.YEAR) - 20, calendar.get(Calendar.MONTH), calendar.get(Calendar.DAY_OF_MONTH)).show()
        }

        // 2. 实时监听密码强度
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
                // 当主密码变动时，也要检查确认密码是否还匹配
                if (editConfirmPassword.text.isNotEmpty()) {
                    checkPasswordMatch(editPassword, editConfirmPassword)
                }
            }
        })

        // 3. 实时监听确认密码：不一致立即提示
        editConfirmPassword.addTextChangedListener(object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {}
            override fun afterTextChanged(s: Editable?) {
                checkPasswordMatch(editPassword, editConfirmPassword)
            }
        })

        btnCreateAccount.setOnClickListener {
            val name = editName.text.toString().trim()
            val email = editEmail.text.toString().trim()
            val password = editPassword.text.toString()
            val confirmPassword = editConfirmPassword.text.toString()
            val regionValue = spinnerRegion.selectedItem.toString()

            if (name.isEmpty() || email.isEmpty() || selectedBirthDate.isEmpty() || password.isEmpty() || regionValue.isEmpty()) {
                Toast.makeText(this, "Please fill in all fields", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            if (!Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
                Toast.makeText(this, "Invalid email format", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            if (password.length < 8) {
                editPassword.error = "Min 8 characters"
                return@setOnClickListener
            }

            if (password != confirmPassword) {
                editConfirmPassword.error = "Passwords do not match"
                return@setOnClickListener
            }

            val request = RegisterRequestDto(
                username = name,
                email = email,
                password = password,
                birthDate = selectedBirthDate,
                region = regionValue
            )

            lifecycleScope.launch {
                try {
                    val result = authRepository.register(request)
                    result.onSuccess {
                        Toast.makeText(this@RegisterActivity, "Account created!", Toast.LENGTH_SHORT).show()
                        val intent = Intent(this@RegisterActivity, LoginActivity::class.java)
                        intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
                        startActivity(intent)
                        finish()
                    }.onFailure { exception ->
                        val errorMsg = exception.message ?: ""
                        val displayMsg = when {
                            errorMsg.contains("Region") -> "Please select a valid region."
                            errorMsg.contains("409") || errorMsg.contains("Duplicate") -> "Email already exists."
                            else -> "Registration failed. Please try again."
                        }
                        Toast.makeText(this@RegisterActivity, displayMsg, Toast.LENGTH_LONG).show()
                    }
                } catch (e: Exception) {
                    Log.e("API_CHECK", "Error: ${e.localizedMessage}")
                }
            }
        }

        textLogin.setOnClickListener { finish() }
    }

    // 抽取出的密码匹配检查函数
    private fun checkPasswordMatch(p1: EditText, p2: EditText) {
        if (p1.text.toString() != p2.text.toString()) {
            p2.error = "Passwords do not match"
        } else {
            p2.error = null
        }
    }

    private fun updatePasswordStrengthUI(password: String, textView: TextView) {
        var score = 0
        if (password.length in 8..20) score++
        if (password.length > 12) score++
        if (password.any { it.isDigit() }) score++
        if (password.any { it.isUpperCase() }) score++
        if (password.any { !it.isLetterOrDigit() }) score++

        when {
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