package iss.nus.edu.sg.sharedprefs.admobile

import android.content.Intent
import android.os.Bundle
import android.widget.ArrayAdapter
import android.widget.Spinner
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity

class RegisterActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.register_activity)

        val regionSpinner: Spinner = findViewById(R.id.spinnerRegion)
        // Create an ArrayAdapter using the string array and a default spinner layout
        ArrayAdapter.createFromResource(
            this,
            R.array.regions_array,
            android.R.layout.simple_spinner_item
        ).also { adapter ->
            // Specify the layout to use when the list of choices appears
            adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
            // Apply the adapter to the spinner
            regionSpinner.adapter = adapter
        }

        val loginText = findViewById<TextView>(R.id.textLogin)
        loginText.setOnClickListener {
            // Finish current activity and go back to LoginActivity
            finish()
        }
    }
}
