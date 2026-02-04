package iss.nus.edu.sg.sharedprefs.admobile.ui.adapter

import android.graphics.Color
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.CheckBox
import android.widget.ImageView
import android.widget.LinearLayout
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.card.MaterialCardView
import iss.nus.edu.sg.sharedprefs.admobile.R

data class EmissionRecord(
    val id: Int = 0,
    val date: String,
    val type: String,
    val amount: String,
    val desc: String,
    val originalObject: Any? = null
)

class RecordAdapter(private var records: MutableList<EmissionRecord>) : RecyclerView.Adapter<RecordAdapter.ViewHolder>() {

    var onDeleteClickListener: ((Int) -> Unit)? = null
    var onItemClickListener: ((EmissionRecord) -> Unit)? = null

    // 🌟 核心修复：通过注解更改属性自动生成的 Setter 名称，避免与下面的函数冲突
    @get:JvmName("getEditModeState")
    @set:JvmName("setEditModeState")
    var isEditMode = false

    val selectedItems = mutableSetOf<EmissionRecord>()

    class ViewHolder(view: View) : RecyclerView.ViewHolder(view) {
        val cardView: MaterialCardView = view.findViewById(R.id.card_view)
        val deleteMenu: LinearLayout = view.findViewById(R.id.delete_menu)
        val tvDate: TextView = view.findViewById(R.id.tv_record_date)
        val tvType: TextView = view.findViewById(R.id.tv_record_type)
        val tvDesc: TextView = view.findViewById(R.id.tv_record_desc)
        val tvAmount: TextView = view.findViewById(R.id.tv_record_amount)
        val badgeContainer: LinearLayout = view.findViewById(R.id.badge_container)
        val ivType: ImageView = view.findViewById(R.id.iv_record_type)
        val checkBox: CheckBox = view.findViewById(R.id.item_checkbox)
    }

    /**
     * 🌟 状态切换方法：现在它与属性 Setter 不再冲突
     */
    fun setEditMode(enabled: Boolean) {
        this.isEditMode = enabled
        if (!enabled) {
            selectedItems.clear()
        }
        notifyDataSetChanged()
    }

    /**
     * 🌟 切换单项选中状态
     */
    fun toggleSelection(record: EmissionRecord) {
        if (selectedItems.contains(record)) {
            selectedItems.remove(record)
        } else {
            selectedItems.add(record)
        }
        val index = records.indexOf(record)
        if (index != -1) {
            notifyItemChanged(index)
        }
    }

    /**
     * 🌟 获取选中的项用于 API 请求
     */
    fun getSelectedItems(): List<EmissionRecord> = selectedItems.toList()

    fun removeItem(position: Int) {
        if (position in records.indices) {
            records.removeAt(position)
            notifyItemRemoved(position)
            notifyItemRangeChanged(position, records.size - position)
        }
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context).inflate(R.layout.item_emission_record, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val record = records[position]

        // --- 1. 编辑模式与多选逻辑 ---
        holder.checkBox.visibility = if (isEditMode) View.VISIBLE else View.GONE

        holder.checkBox.setOnCheckedChangeListener(null)
        holder.checkBox.isChecked = selectedItems.contains(record)

        holder.checkBox.setOnCheckedChangeListener { _, isChecked ->
            if (isChecked) selectedItems.add(record) else selectedItems.remove(record)
        }

        holder.cardView.setOnClickListener {
            if (isEditMode) {
                toggleSelection(record)
            } else {
                onItemClickListener?.invoke(record)
            }
        }

        // --- 2. 侧滑删除按钮逻辑 ---
        holder.deleteMenu.setOnClickListener {
            if (!isEditMode) {
                val currentPos = holder.bindingAdapterPosition
                if (currentPos != RecyclerView.NO_POSITION) {
                    onDeleteClickListener?.invoke(currentPos)
                }
            }
        }

        // --- 3. 基础 UI 绑定 ---
        holder.cardView.translationX = 0f
        holder.tvDate.text = record.date
        holder.tvType.text = record.type
        holder.tvDesc.text = record.desc
        holder.tvAmount.text = "${record.amount} kg CO₂e"

        // --- 4. 样式处理 ---
        when (record.type) {
            "Food" -> {
                holder.badgeContainer.setBackgroundResource(R.drawable.shape_badge_food)
                holder.tvType.setTextColor(Color.parseColor("#674fa3"))
                holder.ivType.setImageResource(R.drawable.main_eat_purple)
            }
            "Transport" -> {
                holder.badgeContainer.setBackgroundResource(R.drawable.shape_badge_transport)
                holder.tvType.setTextColor(Color.parseColor("#1976D2"))
                holder.ivType.setImageResource(R.drawable.main_travel_purple)
            }
            "Utilities" -> {
                holder.badgeContainer.setBackgroundResource(R.drawable.shape_badge_utility)
                holder.tvType.setTextColor(Color.parseColor("#FBC02D"))
                holder.ivType.setImageResource(R.drawable.main_water_purple)
            }
        }
    }

    override fun getItemCount() = records.size
}