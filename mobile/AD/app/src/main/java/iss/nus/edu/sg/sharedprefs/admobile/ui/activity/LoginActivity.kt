package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.widget.Button
import android.widget.EditText
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import iss.nus.edu.sg.sharedprefs.admobile.R
import iss.nus.edu.sg.sharedprefs.admobile.data.model.LoginRequestDto
import iss.nus.edu.sg.sharedprefs.admobile.data.repository.AuthRepository
import kotlinx.coroutines.launch

class LoginActivity : AppCompatActivity() {

    private val authRepository by lazy { AuthRepository(this) }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.login_activity)

        val editEmail = findViewById<EditText>(R.id.editEmail)
        val editPassword = findViewById<EditText>(R.id.editPassword)
        val btnLogin = findViewById<Button>(R.id.btnLogin)
        val textSignUp = findViewById<TextView>(R.id.textSignUp)
        val textForgotPassword = findViewById<TextView>(R.id.textForgotPassword)
/*
        // ============================================================
        // 🚀 当前激活：直接登录跳转逻辑 (Offline / Debug Mode)
        // ============================================================
        btnLogin.setOnClickListener {
            // 模拟保存本地登录状态，以便后续页面读取
            val prefs = getSharedPreferences("EcoLensPrefs", MODE_PRIVATE)
            prefs.edit().apply {
                putString("token", "debug_token_123")
                putString("username", "DebugUser")
                apply()
            }

            Toast.makeText(this, "Debug Mode: Skipping Authentication...", Toast.LENGTH_SHORT).show()

            // 执行跳转
            val intent = Intent(this, MainActivity::class.java)
            intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            startActivity(intent)
            finish()
        }
        // ============================================================
*/

        // ============================================================
        // 🔒 已注释：原始后端 API 登录逻辑 (Production Mode)
        // ============================================================
        btnLogin.setOnClickListener {
            val email = editEmail.text.toString().trim()
            val password = editPassword.text.toString().trim()

            if (email.isEmpty() || password.isEmpty()) {
                Toast.makeText(this, "Please enter your email and password", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            lifecycleScope.launch {
                val loginRequest = LoginRequestDto(email, password)
                val result = authRepository.login(loginRequest)

                result.onSuccess { authResponse ->
                    // 🌟 1. 必须添加：保存 Token，否则 MainActivity 会把你踢出来
                    val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
                    prefs.edit().apply {
                        putString("access_token", authResponse.token) // 确保字段名是 access_token
                        apply()
                    }

                    Toast.makeText(this@LoginActivity, "Welcome back, ${authResponse.user.username}!", Toast.LENGTH_SHORT).show()

                    val intent = Intent(this@LoginActivity, MainActivity::class.java)
                    intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
                    startActivity(intent)
                    finish()
                }.onFailure { exception ->
                    Toast.makeText(this@LoginActivity, "Login failed: ${exception.message}", Toast.LENGTH_LONG).show()
                }
            }
        }
        // ============================================================



        // 注册跳转保持可用，方便你查看注册页 UI
        textSignUp.setOnClickListener {
            val intent = Intent(this, RegisterActivity::class.java)
            startActivity(intent)
        }

        textForgotPassword.setOnClickListener {
            Toast.makeText(this, "Forgot password is disabled in Debug Mode.", Toast.LENGTH_SHORT).show()
        }
    }
}