package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.view.View
import android.widget.*
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
        val progressBar = findViewById<ProgressBar>(R.id.loginProgressBar)
        val textSignUp = findViewById<TextView>(R.id.textSignUp)
        val textForgotPassword = findViewById<TextView>(R.id.textForgotPassword)

        btnLogin.setOnClickListener {
            val email = editEmail.text.toString().trim()
            val password = editPassword.text.toString().trim()

            if (email.isEmpty() || password.isEmpty()) {
                Toast.makeText(this, "Please enter your email and password", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            // 🌟 1. 进入加载状态：禁用按钮，隐藏文字，显示进度条
            setLoading(true, btnLogin, progressBar)

            lifecycleScope.launch {
                val loginRequest = LoginRequestDto(email, password)
                val result = authRepository.login(loginRequest)

                result.onSuccess { authResponse ->
                    // 保存 Token
                    val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
                    prefs.edit().apply {
                        putString("access_token", authResponse.token)
                        apply()
                    }

                    Toast.makeText(this@LoginActivity, "Welcome back, ${authResponse.user.username}!", Toast.LENGTH_SHORT).show()

                    val intent = Intent(this@LoginActivity, MainActivity::class.java)
                    intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
                    startActivity(intent)
                    finish()
                }.onFailure { exception ->
                    // 🌟 2. 登录失败：恢复 UI 状态，允许用户再次尝试
                    setLoading(false, btnLogin, progressBar)
                    Toast.makeText(this@LoginActivity, "Login failed: ${exception.message}", Toast.LENGTH_LONG).show()
                }
            }
        }

        textSignUp.setOnClickListener {
            val intent = Intent(this, RegisterActivity::class.java)
            startActivity(intent)
        }

        textForgotPassword.setOnClickListener {
            Toast.makeText(this, "Forgot password function coming soon.", Toast.LENGTH_SHORT).show()
        }
    }

    /**
     * 🌟 切换 UI 的加载状态
     * @param isLoading 是否正在加载
     * @param button 登录按钮
     * @param progressBar 进度条
     */
    private fun setLoading(isLoading: Boolean, button: Button, progressBar: ProgressBar) {
        if (isLoading) {
            button.isEnabled = false
            button.text = "" // 清空文字，给进度条留出位置
            progressBar.visibility = View.VISIBLE
        } else {
            button.isEnabled = true
            button.text = "Login"
            progressBar.visibility = View.GONE
        }
    }
}