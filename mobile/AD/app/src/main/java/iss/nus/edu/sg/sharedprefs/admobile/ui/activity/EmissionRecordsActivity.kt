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

class EmissionRecordsActivity : AppCompatActivity() {

    private lateinit var adapter: RecordAdapter
    private val displayList = mutableListOf<EmissionRecord>()
    private val allRecords = mutableListOf<EmissionRecord>()

    private var selectedType = "All Types"

    // 分拆日期范围为开始和结束
    private var startDate: Long? = null
    private var endDate: Long? = null
    private val sdfFilter = SimpleDateFormat("MMM dd", Locale.US)

    private var openedViewHolder: RecordAdapter.ViewHolder? = null
    private var isEditMode = false

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

    override fun onCreateOptionsMenu(menu: Menu?): Boolean {
        menuInflater.inflate(R.menu.menu_records, menu)
        return true
    }

    override fun onOptionsItemSelected(item: MenuItem): Boolean {
        return when (item.itemId) {
            R.id.action_edit -> {
                if (isEditMode) performBatchDelete() else enterEditMode()
                true
            }
            else -> super.onOptionsItemSelected(item)
        }
    }

    private fun enterEditMode() {
        isEditMode = true
        adapter.setEditMode(true)
        supportActionBar?.title = "Select Items"
        invalidateOptionsMenu()
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
            .setPositiveButton("Delete") { _, _ -> executeBatchDeleteApi(selected) }
            .setNegativeButton("Cancel", null)
            .show()
    }

    private fun executeBatchDeleteApi(selected: List<EmissionRecord>) {
        lifecycleScope.launch {
            try {
                val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
                val token = "Bearer ${prefs.getString("access_token", "")}"

                val requestBody = selected.map { record ->
                    val typeInt = when (record.type) {
                        "Food" -> 1
                        "Transport" -> 2
                        "Utilities" -> 3
                        else -> 0
                    }
                    TypedDeleteEntry(type = typeInt, id = record.id)
                }

                val response = NetworkClient.apiService.batchDeleteTypedRecords(token, requestBody)

                if (response.isSuccessful) {
                    val count = response.body()?.totalDeleted ?: 0
                    Toast.makeText(this@EmissionRecordsActivity, "Deleted $count records", Toast.LENGTH_SHORT).show()
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

    private fun setupFilterButtons() {
        val btnType = findViewById<MaterialButton>(R.id.btn_filter_type)
        val btnStart = findViewById<MaterialButton>(R.id.btn_start_date)
        val btnEnd = findViewById<MaterialButton>(R.id.btn_end_date)

        btnType.setOnClickListener { showTypePopupMenu(it) }

        // 设置开始日期
        btnStart.setOnClickListener {
            showDatePicker("Select Start Date", startDate) { date ->
                startDate = date
                btnStart.text = sdfFilter.format(Date(date))
                applyFilters()
            }
        }

        // 设置结束日期
        btnEnd.setOnClickListener {
            showDatePicker("Select End Date", endDate) { date ->
                endDate = date
                btnEnd.text = sdfFilter.format(Date(date))
                applyFilters()
            }
        }

        // 长按重置日期
        btnStart.setOnLongClickListener {
            startDate = null
            btnStart.text = "Start"
            applyFilters()
            true
        }
        btnEnd.setOnLongClickListener {
            endDate = null
            btnEnd.text = "End"
            applyFilters()
            true
        }
    }

    private fun showDatePicker(title: String, currentSelection: Long?, onSelected: (Long) -> Unit) {
        val builder = MaterialDatePicker.Builder.datePicker()
        builder.setTitleText(title)
        currentSelection?.let { builder.setSelection(it) }
        val picker = builder.build()
        picker.addOnPositiveButtonClickListener { onSelected(it) }
        picker.show(supportFragmentManager, "DATE_PICKER")
    }

    private fun applyFilters() {
        var filtered = allRecords.toList()

        // 类型过滤
        if (selectedType != "All Types") {
            filtered = filtered.filter { it.type == selectedType }
        }

        // 日期范围过滤
        val isoParser = SimpleDateFormat("yyyy-MM-dd", Locale.US).apply {
            timeZone = TimeZone.getTimeZone("UTC")
        }

        filtered = filtered.filter { record ->
            try {
                val datePart = if (record.date.contains("T")) record.date.substring(0, 10) else record.date
                val recordTime = isoParser.parse(datePart)?.time ?: 0L

                // 如果设置了开始日期，记录必须 >= 开始日期
                val afterStart = startDate?.let { recordTime >= it } ?: true
                // 如果设置了结束日期，记录必须 <= 结束日期 (加一天以包含当天)
                val beforeEnd = endDate?.let { recordTime <= (it + 86400000) } ?: true

                afterStart && beforeEnd
            } catch (e: Exception) { true }
        }

        displayList.clear()
        displayList.addAll(filtered.map { it.copy(date = formatDate(it.date)) })
        adapter.notifyDataSetChanged()
    }

    // --- 以下方法保持不变 ---

    private fun formatDate(rawDate: String): String {
        return try {
            val datePart = if (rawDate.contains("T")) rawDate.substring(0, 10) else rawDate
            val parser = SimpleDateFormat("yyyy-MM-dd", Locale.US)
            val formatter = SimpleDateFormat("MMM dd, yyyy", Locale.US)
            formatter.format(parser.parse(datePart)!!)
        } catch (e: Exception) { rawDate }
    }

    @SuppressLint("ClickableViewAccessibility")
    private fun setupRecyclerView() {
        val recyclerView = findViewById<RecyclerView>(R.id.rv_records)
        recyclerView.layoutManager = LinearLayoutManager(this)
        adapter = RecordAdapter(displayList)

        adapter.onItemClickListener = { record ->
            if (isEditMode) adapter.toggleSelection(record)
            else if (openedViewHolder != null) {
                openedViewHolder?.cardView?.translationX = 0f
                openedViewHolder = null
            } else showDetailDialog(record)
        }

        adapter.onDeleteClickListener = { position ->
            if (!isEditMode) showDeleteDialog(position, adapter)
            openedViewHolder = null
        }
        recyclerView.adapter = adapter

        val swipeHandler = object : ItemTouchHelper.SimpleCallback(0, ItemTouchHelper.LEFT) {
            override fun getSwipeThreshold(v: RecyclerView.ViewHolder) = 1.0f
            override fun getSwipeEscapeVelocity(d: Float) = Float.MAX_VALUE
            override fun onMove(r: RecyclerView, v: RecyclerView.ViewHolder, t: RecyclerView.ViewHolder) = false
            override fun onSwiped(vh: RecyclerView.ViewHolder, dir: Int) {
                adapter.notifyItemChanged(vh.bindingAdapterPosition)
            }
            override fun onChildDraw(c: Canvas, rv: RecyclerView, vh: RecyclerView.ViewHolder, dX: Float, dY: Float, actionState: Int, isCurrentlyActive: Boolean) {
                if (isEditMode) return
                val holder = vh as RecordAdapter.ViewHolder
                val buttonWidth = 100 * rv.context.resources.displayMetrics.density
                if (isCurrentlyActive && openedViewHolder != null && openedViewHolder != holder) {
                    openedViewHolder?.cardView?.animate()?.translationX(0f)?.setDuration(100)?.start()
                    openedViewHolder = null
                }
                if (actionState == ItemTouchHelper.ACTION_STATE_SWIPE) {
                    val limitX = -buttonWidth
                    if (isCurrentlyActive) holder.cardView.translationX = if (dX < limitX) limitX else dX
                    else {
                        if (holder.cardView.translationX < -40 * rv.context.resources.displayMetrics.density) {
                            holder.cardView.translationX = limitX
                            openedViewHolder = holder
                        } else holder.cardView.translationX = 0f
                    }
                }
            }
        }
        ItemTouchHelper(swipeHandler).attachToRecyclerView(recyclerView)
        recyclerView.setOnTouchListener { _, _ ->
            openedViewHolder?.cardView?.animate()?.translationX(0f)?.setDuration(200)?.start()
            openedViewHolder = null
            false
        }
    }

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
        addRow("Recorded Date", record.date)
        val obj = record.originalObject
        when (obj) {
            is TravelHistoryItem -> {
                addRow("Transport Mode", obj.transportModeName)
                addRow("Origin", obj.originAddress)
                addRow("Destination", obj.destinationAddress)
                addRow("Notes", obj.notes)
            }
            is FoodHistoryItem -> {
                addRow("Food Name", obj.name)
                addRow("Amount/Quantity", "${obj.amount}")
                addRow("Notes", obj.notes)
            }
            is UtilityHistoryItem -> {
                addRow("Electricity", "${obj.electricityUsage} kWh")
                addRow("Water", "${obj.waterUsage} m³")
                addRow("Notes", obj.notes)
            }
        }
        val dialog = AlertDialog.Builder(this).setView(dialogView).create()
        dialogView.findViewById<MaterialButton>(R.id.btn_close_detail).setOnClickListener { dialog.dismiss() }
        dialog.show()
    }

    private fun fetchDataFromBackend() {
        lifecycleScope.launch {
            try {
                val prefs = getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
                val token = "Bearer ${prefs.getString("access_token", "")}"
                val travelDeferred = async { NetworkClient.apiService.getTravelHistory(token, 100) }
                val foodDeferred = async { NetworkClient.apiService.getFoodHistory(token, 100) }
                val utilityDeferred = async { NetworkClient.apiService.getUtilityHistory(token, 100) }
                val newList = mutableListOf<EmissionRecord>()
                travelDeferred.await().body()?.items?.forEach { newList.add(EmissionRecord(it.id, it.createdAt, "Transport", String.format("%.2f", it.carbonEmission), it.transportModeName, it)) }
                foodDeferred.await().body()?.items?.forEach { newList.add(EmissionRecord(it.id, it.createdAt, "Food", String.format("%.2f", it.emission), it.name, it)) }
                utilityDeferred.await().body()?.items?.forEach { newList.add(EmissionRecord(it.id, it.createdAt, "Utilities", String.format("%.2f", it.totalCarbonEmission), "Utility Bill", it)) }
                allRecords.clear()
                allRecords.addAll(newList.sortedByDescending { it.date })
                applyFilters()
            } catch (e: Exception) { Toast.makeText(this@EmissionRecordsActivity, "Network error", Toast.LENGTH_SHORT).show() }
        }
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

    private fun showDeleteDialog(position: Int, adapter: RecordAdapter) {
        val recordToDelete = displayList[position]
        AlertDialog.Builder(this).setTitle("Delete Record").setMessage("Are you sure?").setPositiveButton("Delete") { _, _ ->
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
                    if (response?.isSuccessful == true) {
                        allRecords.removeAll { it.id == recordToDelete.id && it.type == recordToDelete.type }
                        adapter.removeItem(position)
                        applyFilters()
                    }
                } catch (e: Exception) { Toast.makeText(this@EmissionRecordsActivity, "Error", Toast.LENGTH_SHORT).show() }
            }
        }.setNegativeButton("Cancel", null).show()
    }
}