package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.content.Context
import android.graphics.Canvas
import android.graphics.Color
import android.os.Bundle
import android.view.View
import android.widget.PopupMenu
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.ItemTouchHelper
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.appbar.MaterialToolbar
import com.google.android.material.button.MaterialButton
import com.google.android.material.datepicker.MaterialDatePicker
import iss.nus.edu.sg.sharedprefs.admobile.R
import iss.nus.edu.sg.sharedprefs.admobile.data.model.*
import iss.nus.edu.sg.sharedprefs.admobile.data.network.NetworkClient
import iss.nus.edu.sg.sharedprefs.admobile.ui.adapter.EmissionRecord
import iss.nus.edu.sg.sharedprefs.admobile.ui.adapter.RecordAdapter
import kotlinx.coroutines.async
import kotlinx.coroutines.launch
import java.text.SimpleDateFormat
import java.util.*
import androidx.core.util.Pair

class EmissionRecordsActivity : AppCompatActivity() {

    private lateinit var adapter: RecordAdapter
    private val displayList = mutableListOf<EmissionRecord>()
    private val allRecords = mutableListOf<EmissionRecord>()

    private var selectedType = "All Types"
    private var selectedDateRange: Pair<Long, Long>? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_records)

        window.statusBarColor = Color.parseColor("#674fa3")

        val toolbar = findViewById<MaterialToolbar>(R.id.toolbar)
        setSupportActionBar(toolbar)
        toolbar.setNavigationOnClickListener { onBackPressedDispatcher.onBackPressed() }

        setupRecyclerView()
        setupFilterButtons()
        fetchDataFromBackend()
    }

    private fun setupFilterButtons() {
        val btnType = findViewById<MaterialButton>(R.id.btn_filter_type)
        val btnDate = findViewById<MaterialButton>(R.id.btn_filter_month)

        btnType.setOnClickListener { showTypePopupMenu(it) }
        btnDate.setOnClickListener { showDateRangePicker() }
    }

    private fun showTypePopupMenu(view: View) {
        val btnType = view as MaterialButton
        val popup = PopupMenu(this, view)

        val types = arrayOf("All Types", "Transport", "Food", "Utilities")
        types.forEachIndexed { index, title ->
            popup.menu.add(0, index, index, title)
        }

        popup.setOnMenuItemClickListener { menuItem ->
            selectedType = types[menuItem.itemId]
            btnType.text = selectedType
            applyFilters()
            true
        }
        popup.show()
    }

    /**
     * 🌟 核心修改：实现图片中的小型对话框日历 (范围选择模式)
     */
    private fun showDateRangePicker() {
        // 使用 dateRangePicker 以获得图片中的紫色 Header 对话框
        val builder = MaterialDatePicker.Builder.dateRangePicker()
        builder.setTitleText("Select Date Range")

        // 强制使用日历模式，而不是文本输入模式
        builder.setInputMode(MaterialDatePicker.INPUT_MODE_CALENDAR)

        // 恢复之前的选中状态
        selectedDateRange?.let {
            builder.setSelection(it)
        }

        val picker = builder.build()

        picker.addOnPositiveButtonClickListener { selection ->
            selectedDateRange = selection

            // 格式化日期显示，Locale.US 确保为英文月份
            val sdf = SimpleDateFormat("MMM dd, yyyy", Locale.US)
            val rangeText = "${sdf.format(Date(selection.first))} - ${sdf.format(Date(selection.second))}"
            findViewById<MaterialButton>(R.id.btn_filter_month).text = rangeText

            applyFilters()
        }

        // picker.show 将产生一个居中的小型 Dialog，效果完全匹配你的截图
        picker.show(supportFragmentManager, "DATE_RANGE_PICKER")
    }

    /**
     * 核心过滤算法：处理类别和日期段的联动
     */
    private fun applyFilters() {
        var filtered = allRecords.toList()

        // 1. 类别过滤
        if (selectedType != "All Types") {
            filtered = filtered.filter { it.type == selectedType }
        }

        // 2. 日期范围过滤
        selectedDateRange?.let { range ->
            val startTime = range.first
            // 包含结束日当天的 24 小时
            val endTime = range.second + 86400000

            // 🌟 关键：使用 UTC 时区解析 ISO 日期，以匹配 Picker 返回的时间戳
            val isoParser = SimpleDateFormat("yyyy-MM-dd", Locale.US).apply {
                timeZone = TimeZone.getTimeZone("UTC")
            }

            filtered = filtered.filter { record ->
                try {
                    val datePart = if (record.date.contains("T")) record.date.substring(0, 10) else record.date
                    val recordTime = isoParser.parse(datePart)?.time ?: 0L
                    recordTime in startTime until endTime
                } catch (e: Exception) {
                    true
                }
            }
        }

        // 3. 更新 UI 显示
        displayList.clear()
        displayList.addAll(filtered.map { it.copy(date = formatDate(it.date)) })
        adapter.notifyDataSetChanged()
    }

    private fun fetchDataFromBackend() {
        lifecycleScope.launch {
            try {
                val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
                val token = prefs.getString("access_token", "") ?: ""
                val authHeader = "Bearer $token"

                val travelDeferred = async { NetworkClient.apiService.getTravelHistory(authHeader, 100) }
                val foodDeferred = async { NetworkClient.apiService.getFoodHistory(authHeader, 100) }
                val utilityDeferred = async { NetworkClient.apiService.getUtilityHistory(authHeader, 100) }

                val newList = mutableListOf<EmissionRecord>()

                // Process Travel
                val travelRes = travelDeferred.await()
                if (travelRes.isSuccessful) {
                    travelRes.body()?.items?.forEach { item ->
                        newList.add(EmissionRecord(item.id, item.createdAt, "Transport",
                            String.format("%.2f", item.carbonEmission), item.transportModeName))
                    }
                }

                // Process Food
                val foodRes = foodDeferred.await()
                if (foodRes.isSuccessful) {
                    foodRes.body()?.items?.forEach { item ->
                        newList.add(EmissionRecord(item.id, item.createdAt, "Food",
                            String.format("%.2f", item.emission), item.name))
                    }
                }

                // Process Utility
                val utilityRes = utilityDeferred.await()
                if (utilityRes.isSuccessful) {
                    utilityRes.body()?.items?.forEach { item ->
                        newList.add(EmissionRecord(item.id, item.createdAt, "Utilities",
                            String.format("%.2f", item.totalCarbonEmission), "Utility Bill"))
                    }
                }

                allRecords.clear()
                allRecords.addAll(newList.sortedByDescending { it.date })

                // 默认加载全部
                applyFilters()

            } catch (e: Exception) {
                Toast.makeText(this@EmissionRecordsActivity, "Network error", Toast.LENGTH_SHORT).show()
            }
        }
    }

    private fun formatDate(rawDate: String): String {
        return try {
            val datePart = if (rawDate.contains("T")) rawDate.substring(0, 10) else rawDate
            val parser = SimpleDateFormat("yyyy-MM-dd", Locale.US)
            val formatter = SimpleDateFormat("MMM dd, yyyy", Locale.US)
            formatter.format(parser.parse(datePart)!!)
        } catch (e: Exception) {
            rawDate
        }
    }

    private fun setupRecyclerView() {
        val recyclerView = findViewById<RecyclerView>(R.id.rv_records)
        recyclerView.layoutManager = LinearLayoutManager(this)
        adapter = RecordAdapter(displayList)
        adapter.onDeleteClickListener = { position -> showDeleteDialog(position, adapter) }
        recyclerView.adapter = adapter

        val swipeHandler = object : ItemTouchHelper.SimpleCallback(0, ItemTouchHelper.LEFT) {
            override fun onMove(rv: RecyclerView, vh: RecyclerView.ViewHolder, target: RecyclerView.ViewHolder) = false
            override fun onSwiped(viewHolder: RecyclerView.ViewHolder, direction: Int) {
                adapter.notifyItemChanged(viewHolder.adapterPosition)
            }
            override fun onChildDraw(c: Canvas, recyclerView: RecyclerView, viewHolder: RecyclerView.ViewHolder, dX: Float, dY: Float, actionState: Int, isCurrentlyActive: Boolean) {
                if (actionState == ItemTouchHelper.ACTION_STATE_SWIPE) {
                    val holder = viewHolder as RecordAdapter.ViewHolder
                    val buttonWidth = 100 * recyclerView.context.resources.displayMetrics.density
                    val translationX = if (Math.abs(dX) > buttonWidth) -buttonWidth else dX
                    holder.cardView.translationX = translationX
                } else {
                    super.onChildDraw(c, recyclerView, viewHolder, dX, dY, actionState, isCurrentlyActive)
                }
            }
        }
        ItemTouchHelper(swipeHandler).attachToRecyclerView(recyclerView)
    }

    private fun showDeleteDialog(position: Int, adapter: RecordAdapter) {
        AlertDialog.Builder(this)
            .setTitle("Delete Record")
            .setMessage("Are you sure you want to delete this record?")
            .setPositiveButton("Delete") { _, _ ->
                val recordToDelete = displayList[position]
                allRecords.removeAll { it.id == recordToDelete.id && it.type == recordToDelete.type }
                adapter.removeItem(position)
            }
            .setNegativeButton("Cancel") { _, _ -> adapter.notifyItemChanged(position) }
            .show()
    }
}