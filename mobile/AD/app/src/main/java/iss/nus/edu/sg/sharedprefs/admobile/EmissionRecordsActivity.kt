package iss.nus.edu.sg.sharedprefs.admobile

import android.graphics.Color
import android.os.Bundle
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.appbar.MaterialToolbar
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONArray
import org.json.JSONObject
import java.text.SimpleDateFormat
import java.util.*

class EmissionRecordsActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_records)

        window.statusBarColor = Color.parseColor("#674fa3")

        // 🌟 返回逻辑
        findViewById<MaterialToolbar>(R.id.toolbar).setNavigationOnClickListener {
            onBackPressedDispatcher.onBackPressed()
        }

        loadRecords()
    }

    private fun loadRecords() {
        val token = ApiHelper.getToken(this)
        if (token == null) {
            Toast.makeText(this, "Please login first", Toast.LENGTH_SHORT).show()
            finish()
            return
        }

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val response = ApiHelper.executeGet(this@EmissionRecordsActivity, "/api/records")
                val responseBody = response.body?.string()

                withContext(Dispatchers.Main) {
                    if (response.isSuccessful && responseBody != null) {
                        val jsonArray = JSONArray(responseBody)
                        val records = parseRecords(jsonArray)
                        setupRecyclerView(records)
                    } else {
                        val errorMsg = if (responseBody != null) {
                            try {
                                JSONObject(responseBody).optString("message", "Failed to load records")
                            } catch (e: Exception) {
                                "Failed to load records: ${response.code}"
                            }
                        } else {
                            "Failed to load records: ${response.code}"
                        }
                        Toast.makeText(this@EmissionRecordsActivity, errorMsg, Toast.LENGTH_SHORT).show()
                        // 显示空列表
                        setupRecyclerView(emptyList())
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@EmissionRecordsActivity, "Error: ${e.message}", Toast.LENGTH_SHORT).show()
                    android.util.Log.e("EmissionRecordsActivity", "Load records error", e)
                    setupRecyclerView(emptyList())
                }
            }
        }
    }

    private fun parseRecords(jsonArray: JSONArray): List<EmissionRecord> {
        val records = mutableListOf<EmissionRecord>()
        val inputFormat = SimpleDateFormat("yyyy-MM-dd", Locale.ENGLISH)
        val outputFormat = SimpleDateFormat("MMM dd, yyyy", Locale.ENGLISH)

        for (i in 0 until jsonArray.length()) {
            val item = jsonArray.getJSONObject(i)
            val dateStr = item.optString("date", "")
            val type = item.optString("type", "")
            val amount = item.optDouble("amount", 0.0)
            val description = item.optString("description", "")

            // 格式化日期
            val formattedDate = try {
                if (dateStr.isNotEmpty()) {
                    val date = inputFormat.parse(dateStr)
                    date?.let { outputFormat.format(it) } ?: dateStr
                } else dateStr
            } catch (e: Exception) {
                dateStr
            }

            // 格式化类型显示
            val typeDisplay = when (type.lowercase()) {
                "food" -> "Food"
                "transport" -> "Transport"
                "utility" -> "Utilities"
                else -> type
            }

            records.add(EmissionRecord(formattedDate, typeDisplay, String.format("%.2f", amount), description))
        }

        return records
    }

    private fun setupRecyclerView(records: List<EmissionRecord>) {
        val recyclerView = findViewById<RecyclerView>(R.id.rv_records)
        recyclerView.layoutManager = LinearLayoutManager(this)
        recyclerView.adapter = RecordAdapter(records)
    }
}