package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.graphics.Canvas
import android.graphics.Color
import android.os.Bundle
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.ItemTouchHelper
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.appbar.MaterialToolbar
import iss.nus.edu.sg.sharedprefs.admobile.R
import iss.nus.edu.sg.sharedprefs.admobile.ui.adapter.EmissionRecord
import iss.nus.edu.sg.sharedprefs.admobile.ui.adapter.RecordAdapter

class EmissionRecordsActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_records)

        window.statusBarColor = Color.parseColor("#674fa3")

        val toolbar = findViewById<MaterialToolbar>(R.id.toolbar)
        setSupportActionBar(toolbar)
        toolbar.setNavigationOnClickListener {
            onBackPressedDispatcher.onBackPressed()
        }

        setupRecyclerView()
    }

    private fun setupRecyclerView() {
        val recyclerView = findViewById<RecyclerView>(R.id.rv_records)
        recyclerView.layoutManager = LinearLayoutManager(this)

        val mockData = mutableListOf(
            EmissionRecord(1, "Jan 23, 2026", "Food", "2.5", "Beef meal at restaurant"),
            EmissionRecord(2, "Jan 22, 2026", "Transport", "1.8", "Drive car to office (25 km)"),
            EmissionRecord(3, "Jan 21, 2026", "Utilities", "0.5", "Electricity usage"),
            EmissionRecord(4, "Jan 20, 2026", "Food", "1.2", "Chicken pasta"),
            EmissionRecord(5, "Jan 19, 2026", "Transport", "0.9", "Public bus ride")
        )

        val adapter = RecordAdapter(mockData)

        // 🌟 1. 绑定适配器内部按钮的点击事件
        adapter.onDeleteClickListener = { position ->
            showDeleteDialog(position, adapter)
        }

        recyclerView.adapter = adapter

        // 🌟 2. 核心修改：重新实现滑动控制
        val swipeHandler = object : ItemTouchHelper.SimpleCallback(0, ItemTouchHelper.LEFT) {
            override fun onMove(rv: RecyclerView, vh: RecyclerView.ViewHolder, target: RecyclerView.ViewHolder) = false

            override fun onSwiped(viewHolder: RecyclerView.ViewHolder, direction: Int) {
                // 🌟 这里必须通知刷新，让滑开的卡片留在原地或恢复，而不是消失
                adapter.notifyItemChanged(viewHolder.adapterPosition)
            }

            // 🌟 🌟 重点：通过重写此方法限制滑动距离
            override fun onChildDraw(
                c: Canvas,
                recyclerView: RecyclerView,
                viewHolder: RecyclerView.ViewHolder,
                dX: Float,
                dY: Float,
                actionState: Int,
                isCurrentlyActive: Boolean
            ) {
                if (actionState == ItemTouchHelper.ACTION_STATE_SWIPE) {
                    val holder = viewHolder as RecordAdapter.ViewHolder

                    // 将 100dp 转换为像素（对应你 XML 里的删除按钮宽度）
                    val buttonWidth = 100 * recyclerView.context.resources.displayMetrics.density

                    // 限制最大位移：dX 是负数（向左滑），我们限制它最小不能超过 -buttonWidth
                    val translationX = if (Math.abs(dX) > buttonWidth) -buttonWidth else dX

                    // 🌟 关键：只移动卡片部分（cardView），底层的删除按钮层不动
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
                adapter.removeItem(position)
                Toast.makeText(this, "Record deleted", Toast.LENGTH_SHORT).show()
            }
            .setNegativeButton("Cancel") { _, _ ->
                // 用户取消后，确保卡片归位
                adapter.notifyItemChanged(position)
            }
            .setCancelable(false)
            .show()
    }
}