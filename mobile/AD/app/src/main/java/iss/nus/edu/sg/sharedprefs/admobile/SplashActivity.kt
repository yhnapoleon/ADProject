package iss.nus.edu.sg.sharedprefs.admobile

import android.content.Intent
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import androidx.appcompat.app.AppCompatActivity

class SplashActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.splash_activity)

        Handler(Looper.getMainLooper()).postDelayed({
            // Start the IntroActivity
            startActivity(Intent(this, IntroActivity::class.java))
            // Close this activity
            finish()
        }, 2000) // 2 seconds delay
    }
}
