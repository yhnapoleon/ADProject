package iss.nus.edu.sg.sharedprefs.admobile.ui.adapter

import android.graphics.Color
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageView
import android.widget.LinearLayout
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.card.MaterialCardView
import iss.nus.edu.sg.sharedprefs.admobile.R

// 数据类
data class EmissionRecord(val id: Int = 0, val date: String, val type: String, val amount: String, val desc: String)

class RecordAdapter(private var records: MutableList<EmissionRecord>) : RecyclerView.Adapter<RecordAdapter.ViewHolder>() {

    // 🌟 定义一个回调，方便 Activity 处理真实的删除逻辑（如调用 API）
    var onDeleteClickListener: ((Int) -> Unit)? = null

    class ViewHolder(view: View) : RecyclerView.ViewHolder(view) {
        // 🌟 新增：层级视图引用
        val cardView: MaterialCardView = view.findViewById(R.id.card_view)
        val deleteMenu: LinearLayout = view.findViewById(R.id.delete_menu)

        // 原有引用
        val tvDate: TextView = view.findViewById(R.id.tv_record_date)
        val tvType: TextView = view.findViewById(R.id.tv_record_type)
        val tvDesc: TextView = view.findViewById(R.id.tv_record_desc)
        val tvAmount: TextView = view.findViewById(R.id.tv_record_amount)
        val badgeContainer: LinearLayout = view.findViewById(R.id.badge_container)
        val ivType: ImageView = view.findViewById(R.id.iv_record_type)
    }

    // 🌟 删除逻辑
    fun removeItem(position: Int) {
        if (position >= 0 && position < records.size) {
            records.removeAt(position)
            notifyItemRemoved(position)
            notifyItemRangeChanged(position, records.size)
        }
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        // 🌟 注意：这里 inflate 的是包含 FrameLayout 层级的那个 item 布局
        val view = LayoutInflater.from(parent.context).inflate(R.layout.item_emission_record, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val record = records[position]

        // 🌟 每次绑定重置位移，防止复用导致错位
        holder.cardView.translationX = 0f

        holder.tvDate.text = record.date
        holder.tvType.text = record.type
        holder.tvDesc.text = record.desc
        holder.tvAmount.text = "${record.amount} kg CO₂e"

        // 🌟 设置底座删除按钮的点击监听
        holder.deleteMenu.setOnClickListener {
            onDeleteClickListener?.invoke(holder.adapterPosition)
        }

        // 标签样式逻辑
        when (record.type) {
            "Food" -> {
                holder.badgeContainer.setBackgroundResource(R.drawable.shape_badge_food)
                holder.tvType.setTextColor(Color.parseColor("#674fa3"))
                holder.ivType.setImageResource(R.drawable.main_eat_purple)
            }
            "Transport" -> {
                holder.badgeContainer.setBackgroundColor(Color.parseColor("#E3F2FD"))
                holder.tvType.setTextColor(Color.parseColor("#1976D2"))
                holder.ivType.setImageResource(R.drawable.main_travel_purple)
            }
            "Utilities" -> {
                holder.badgeContainer.setBackgroundColor(Color.parseColor("#FFFDE7"))
                holder.tvType.setTextColor(Color.parseColor("#FBC02D"))
                holder.ivType.setImageResource(R.drawable.main_water_purple)
            }
        }
    }

    override fun getItemCount() = records.size
}