package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.annotation.SuppressLint
import android.content.Context
import android.graphics.Canvas
import android.graphics.Color
import android.os.Bundle
import android.view.LayoutInflater
import android.view.Menu
import android.view.MenuItem
import android.view.View
import android.widget.LinearLayout
import android.widget.PopupMenu
import android.widget.TextView
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

    private var openedViewHolder: RecordAdapter.ViewHolder? = null
    private var isEditMode = false // 🌟 控制是否处于批量编辑模式

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_records)

        window.statusBarColor = Color.parseColor("#674fa3")

        val toolbar = findViewById<MaterialToolbar>(R.id.toolbar)
        setSupportActionBar(toolbar)
        toolbar.setNavigationOnClickListener {
            if (isEditMode) exitEditMode() else onBackPressedDispatcher.onBackPressed()
        }

        setupRecyclerView()
        setupFilterButtons()
        fetchDataFromBackend()
    }

    // 🌟 注入菜单
    override fun onCreateOptionsMenu(menu: Menu?): Boolean {
        menuInflater.inflate(R.menu.menu_records, menu)
        return true
    }

    // 🌟 处理菜单点击
    override fun onOptionsItemSelected(item: MenuItem): Boolean {
        return when (item.itemId) {
            R.id.action_edit -> {
                if (isEditMode) {
                    performBatchDelete() // 如果已经在编辑，点击按钮执行删除
                } else {
                    enterEditMode()
                }
                true
            }
            else -> super.onOptionsItemSelected(item)
        }
    }

    private fun enterEditMode() {
        isEditMode = true
        adapter.setEditMode(true)
        supportActionBar?.title = "Select Items"
        invalidateOptionsMenu() // 刷新菜单文字
    }

    private fun exitEditMode() {
        isEditMode = false
        adapter.setEditMode(false)
        supportActionBar?.title = "Emission Records"
        invalidateOptionsMenu()
    }

    override fun onPrepareOptionsMenu(menu: Menu?): Boolean {
        val editItem = menu?.findItem(R.id.action_edit)
        editItem?.title = if (isEditMode) "Delete" else "Edit"
        return super.onPrepareOptionsMenu(menu)
    }

    private fun performBatchDelete() {
        val selected = adapter.getSelectedItems()
        if (selected.isEmpty()) {
            Toast.makeText(this, "No items selected", Toast.LENGTH_SHORT).show()
            exitEditMode()
            return
        }

        AlertDialog.Builder(this)
            .setTitle("Batch Delete")
            .setMessage("Delete ${selected.size} records? This cannot be undone.")
            .setPositiveButton("Delete") { _, _ ->
                executeBatchDeleteApi(selected)
            }
            .setNegativeButton("Cancel", null)
            .show()
    }

    private fun executeBatchDeleteApi(selected: List<EmissionRecord>) {
        lifecycleScope.launch {
            try {
                val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
                val token = "Bearer ${prefs.getString("access_token", "")}"

                // 构建批量删除请求体
                val request = BatchDeleteRequest(
                    activityLogIds = selected.filter { it.type == "Food" }.map { it.id },
                    travelLogIds = selected.filter { it.type == "Transport" }.map { it.id },
                    utilityBillIds = selected.filter { it.type == "Utilities" }.map { it.id }
                )

                val response = NetworkClient.apiService.batchDeleteRecords(token, request)

                if (response.isSuccessful) {
                    val count = response.body()?.totalDeleted ?: 0
                    Toast.makeText(this@EmissionRecordsActivity, "Deleted $count records", Toast.LENGTH_SHORT).show()

                    // UI 同步更新
                    allRecords.removeAll { rec -> selected.any { it.id == rec.id && it.type == rec.type } }
                    exitEditMode()
                    applyFilters()
                } else {
                    Toast.makeText(this@EmissionRecordsActivity, "Error: ${response.code()}", Toast.LENGTH_SHORT).show()
                }
            } catch (e: Exception) {
                Toast.makeText(this@EmissionRecordsActivity, "Network Error", Toast.LENGTH_SHORT).show()
            }
        }
    }

    @SuppressLint("ClickableViewAccessibility")
    private fun setupRecyclerView() {
        val recyclerView = findViewById<RecyclerView>(R.id.rv_records)
        recyclerView.layoutManager = LinearLayoutManager(this)
        adapter = RecordAdapter(displayList)

        adapter.onItemClickListener = { record ->
            if (isEditMode) {
                // 编辑模式下点击卡片等于勾选
                adapter.toggleSelection(record)
            } else if (openedViewHolder != null) {
                openedViewHolder?.cardView?.translationX = 0f
                openedViewHolder = null
            } else {
                showDetailDialog(record)
            }
        }

        adapter.onDeleteClickListener = { position ->
            if (!isEditMode) showDeleteDialog(position, adapter)
            openedViewHolder = null
        }
        recyclerView.adapter = adapter

        // 设置 Swipe 处理器，仅在非编辑模式下启用
        val swipeHandler = object : ItemTouchHelper.SimpleCallback(0, ItemTouchHelper.LEFT) {
            override fun getSwipeThreshold(viewHolder: RecyclerView.ViewHolder): Float = 1.0f
            override fun getSwipeEscapeVelocity(defaultValue: Float): Float = Float.MAX_VALUE
            override fun onMove(r: RecyclerView, v: RecyclerView.ViewHolder, t: RecyclerView.ViewHolder) = false
            override fun onSwiped(vh: RecyclerView.ViewHolder, dir: Int) {
                adapter.notifyItemChanged(vh.bindingAdapterPosition)
            }

            override fun onChildDraw(c: Canvas, rv: RecyclerView, vh: RecyclerView.ViewHolder, dX: Float, dY: Float, actionState: Int, isCurrentlyActive: Boolean) {
                // 编辑模式禁止侧滑
                if (isEditMode) return

                val holder = vh as RecordAdapter.ViewHolder
                val buttonWidth = 100 * rv.context.resources.displayMetrics.density

                if (isCurrentlyActive && openedViewHolder != null && openedViewHolder != holder) {
                    openedViewHolder?.cardView?.animate()?.translationX(0f)?.setDuration(100)?.start()
                    openedViewHolder = null
                }

                if (actionState == ItemTouchHelper.ACTION_STATE_SWIPE) {
                    val limitX = -buttonWidth
                    if (isCurrentlyActive) {
                        holder.cardView.translationX = if (dX < limitX) limitX else dX
                    } else {
                        if (holder.cardView.translationX < -40 * rv.context.resources.displayMetrics.density) {
                            holder.cardView.translationX = limitX
                            openedViewHolder = holder
                        } else {
                            holder.cardView.translationX = 0f
                        }
                    }
                }
            }
        }

        ItemTouchHelper(swipeHandler).attachToRecyclerView(recyclerView)

        recyclerView.setOnTouchListener { _, _ ->
            if (openedViewHolder != null) {
                openedViewHolder?.cardView?.animate()?.translationX(0f)?.setDuration(200)?.start()
                openedViewHolder = null
            }
            false
        }
    }

    // ... showDetailDialog, fetchDataFromBackend, setupFilterButtons, applyFilters, formatDate (保持不变) ...

    private fun showDetailDialog(record: EmissionRecord) {
        val dialogView = LayoutInflater.from(this).inflate(R.layout.dialog_record_detail, null)
        val container = dialogView.findViewById<LinearLayout>(R.id.ll_detail_container)
        val tvTitle = dialogView.findViewById<TextView>(R.id.tv_detail_title)

        tvTitle.text = "${record.type} Details"

        fun addRow(label: String, value: String?) {
            if (value.isNullOrBlank() || value == "null") return
            val row = LayoutInflater.from(this).inflate(R.layout.item_detail_row, container, false)
            row.findViewById<TextView>(R.id.tv_label).text = label
            row.findViewById<TextView>(R.id.tv_value).text = value
            container.addView(row)
        }

        addRow("Emission Amount", "${record.amount} kg CO₂e")
        addRow("Recorded Date", formatDate(record.date))

        val obj = record.originalObject
        when (obj) {
            is TravelHistoryItem -> {
                addRow("Transport Mode", obj.transportModeName)
                addRow("Origin", obj.originAddress)
                addRow("Destination", obj.destinationAddress)
                addRow("Passenger Count", obj.passengerCount.toString())
                addRow("Notes", obj.notes)
            }
            is FoodHistoryItem -> {
                addRow("Food Name", obj.name)
                addRow("Amount/Quantity", "${obj.amount}")
                addRow("Emission Factor", "${obj.emissionFactor}")
                addRow("Notes", obj.notes)
            }
            is UtilityHistoryItem -> {
                addRow("Electricity Usage", "${obj.electricityUsage} kWh")
                addRow("Water Usage", "${obj.waterUsage} m³")
                addRow("Billing Period", "${obj.billPeriodStart.substringBefore("T")} to ${obj.billPeriodEnd.substringBefore("T")}")
                addRow("Notes", obj.notes)
            }
        }

        val dialog = AlertDialog.Builder(this)
            .setView(dialogView)
            .create()

        dialogView.findViewById<MaterialButton>(R.id.btn_close_detail).setOnClickListener {
            dialog.dismiss()
        }
        dialog.show()
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

                val travelRes = travelDeferred.await()
                if (travelRes.isSuccessful) {
                    travelRes.body()?.items?.forEach { item ->
                        newList.add(EmissionRecord(item.id, item.createdAt, "Transport",
                            String.format("%.2f", item.carbonEmission), item.transportModeName, item))
                    }
                }

                val foodRes = foodDeferred.await()
                if (foodRes.isSuccessful) {
                    foodRes.body()?.items?.forEach { item ->
                        newList.add(EmissionRecord(item.id, item.createdAt, "Food",
                            String.format("%.2f", item.emission), item.name, item))
                    }
                }

                val utilityRes = utilityDeferred.await()
                if (utilityRes.isSuccessful) {
                    utilityRes.body()?.items?.forEach { item ->
                        newList.add(EmissionRecord(item.id, item.createdAt, "Utilities",
                            String.format("%.2f", item.totalCarbonEmission), "Utility Bill", item))
                    }
                }

                allRecords.clear()
                allRecords.addAll(newList.sortedByDescending { it.date })
                applyFilters()

            } catch (e: Exception) {
                Toast.makeText(this@EmissionRecordsActivity, "Network error", Toast.LENGTH_SHORT).show()
            }
        }
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
        types.forEachIndexed { index, title -> popup.menu.add(0, index, index, title) }
        popup.setOnMenuItemClickListener { menuItem ->
            selectedType = types[menuItem.itemId]
            btnType.text = selectedType
            applyFilters()
            true
        }
        popup.show()
    }

    private fun showDateRangePicker() {
        val builder = MaterialDatePicker.Builder.dateRangePicker()
        builder.setTitleText("Select Date Range")
        builder.setInputMode(MaterialDatePicker.INPUT_MODE_CALENDAR)
        selectedDateRange?.let { builder.setSelection(it) }
        val picker = builder.build()
        picker.addOnPositiveButtonClickListener { selection ->
            selectedDateRange = selection
            val sdf = SimpleDateFormat("MMM dd, yyyy", Locale.US)
            val rangeText = "${sdf.format(Date(selection.first))} - ${sdf.format(Date(selection.second))}"
            findViewById<MaterialButton>(R.id.btn_filter_month).text = rangeText
            applyFilters()
        }
        picker.show(supportFragmentManager, "DATE_RANGE_PICKER")
    }

    private fun applyFilters() {
        var filtered = allRecords.toList()
        if (selectedType != "All Types") {
            filtered = filtered.filter { it.type == selectedType }
        }
        selectedDateRange?.let { range ->
            val startTime = range.first
            val endTime = range.second + 86400000
            val isoParser = SimpleDateFormat("yyyy-MM-dd", Locale.US).apply {
                timeZone = TimeZone.getTimeZone("UTC")
            }
            filtered = filtered.filter { record ->
                try {
                    val datePart = if (record.date.contains("T")) record.date.substring(0, 10) else record.date
                    val recordTime = isoParser.parse(datePart)?.time ?: 0L
                    recordTime in startTime until endTime
                } catch (e: Exception) { true }
            }
        }
        displayList.clear()
        displayList.addAll(filtered.map { it.copy(date = formatDate(it.date)) })
        adapter.notifyDataSetChanged()
    }

    private fun formatDate(rawDate: String): String {
        return try {
            val datePart = if (rawDate.contains("T")) rawDate.substring(0, 10) else rawDate
            val parser = SimpleDateFormat("yyyy-MM-dd", Locale.US)
            val formatter = SimpleDateFormat("MMM dd, yyyy", Locale.US)
            formatter.format(parser.parse(datePart)!!)
        } catch (e: Exception) { rawDate }
    }

    private fun showDeleteDialog(position: Int, adapter: RecordAdapter) {
        val recordToDelete = displayList[position]

        AlertDialog.Builder(this)
            .setTitle("Delete Record")
            .setMessage("Are you sure you want to delete this ${recordToDelete.type} record? This action cannot be undone.")
            .setPositiveButton("Delete") { _, _ ->
                lifecycleScope.launch {
                    try {
                        val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
                        val token = "Bearer ${prefs.getString("access_token", "")}"

                        val response = when (recordToDelete.type) {
                            "Transport" -> NetworkClient.apiService.deleteTravel(token, recordToDelete.id)
                            "Food" -> NetworkClient.apiService.deleteFood(token, recordToDelete.id)
                            "Utilities" -> NetworkClient.apiService.deleteUtility(token, recordToDelete.id)
                            else -> null
                        }

                        if (response != null && response.isSuccessful) {
                            allRecords.removeAll { it.id == recordToDelete.id && it.type == recordToDelete.type }
                            adapter.removeItem(position)
                            openedViewHolder = null
                            Toast.makeText(this@EmissionRecordsActivity, "Deleted successfully", Toast.LENGTH_SHORT).show()
                        } else {
                            Toast.makeText(this@EmissionRecordsActivity, "Delete failed: ${response?.code()}", Toast.LENGTH_SHORT).show()
                            adapter.notifyItemChanged(position)
                        }
                    } catch (e: Exception) {
                        Toast.makeText(this@EmissionRecordsActivity, "Network error", Toast.LENGTH_SHORT).show()
                        adapter.notifyItemChanged(position)
                    }
                }
            }
            .setNegativeButton("Cancel") { _, _ ->
                openedViewHolder?.cardView?.animate()?.translationX(0f)?.setDuration(200)?.start()
                openedViewHolder = null
                adapter.notifyItemChanged(position)
            }
            .show()
    }
}