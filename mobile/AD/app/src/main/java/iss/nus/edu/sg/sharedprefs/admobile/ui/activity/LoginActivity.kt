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
import retrofit2.HttpException
import java.net.SocketTimeoutException
import java.net.UnknownHostException

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

            // 进入加载状态
            setLoading(true, btnLogin, progressBar)

            lifecycleScope.launch {
                val loginRequest = LoginRequestDto(email, password)
                val result = authRepository.login(loginRequest)

                result.onSuccess { authResponse ->
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
                    // 登录失败
                    setLoading(false, btnLogin, progressBar)

                    val friendlyMessage = getFriendlyMessage(exception)
                    Toast.makeText(this@LoginActivity, friendlyMessage, Toast.LENGTH_LONG).show()
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
     * 将异常转换为用户友好的文案
     */
    private fun getFriendlyMessage(throwable: Throwable): String {
        return when (throwable) {
            is SocketTimeoutException -> "The server is taking too long to respond. Please try again later."
            is UnknownHostException -> "Cannot connect to server. Please check your internet connection."
            is HttpException -> {
                when (throwable.code()) {
                    401 -> "Invalid email or password. Please try again."
                    404 -> "Server not found. Please try again later."
                    500 -> "Server internal error. Our team is working on it."
                    else -> "Login failed. Please check your credentials."
                }
            }
            else -> {
                // 如果错误信息里包含某些关键词，手动匹配
                val msg = throwable.message ?: ""
                when {
                    msg.contains("401") -> "Invalid email or password."
                    msg.contains("403") -> "This account is currently locked."
                    msg.contains("timeout") -> "Connection timed out."
                    else -> "An unexpected error occurred. Please try again."
                }
            }
        }
    }

    private fun setLoading(isLoading: Boolean, button: Button, progressBar: ProgressBar) {
        if (isLoading) {
            button.isEnabled = false
            button.text = ""
            progressBar.visibility = View.VISIBLE
        } else {
            button.isEnabled = true
            button.text = "Login"
            progressBar.visibility = View.GONE
        }
    }
}