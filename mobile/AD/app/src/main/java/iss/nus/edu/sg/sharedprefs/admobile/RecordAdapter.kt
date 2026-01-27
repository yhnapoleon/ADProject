package iss.nus.edu.sg.sharedprefs.admobile

import android.graphics.Color
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageView
import android.widget.LinearLayout
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView

// 数据类
data class EmissionRecord(val date: String, val type: String, val amount: String, val desc: String)

class RecordAdapter(private val records: List<EmissionRecord>) : RecyclerView.Adapter<RecordAdapter.ViewHolder>() {

    class ViewHolder(view: View) : RecyclerView.ViewHolder(view) {
        val tvDate: TextView = view.findViewById(R.id.tv_record_date)
        val tvType: TextView = view.findViewById(R.id.tv_record_type)
        val tvDesc: TextView = view.findViewById(R.id.tv_record_desc)
        val tvAmount: TextView = view.findViewById(R.id.tv_record_amount)
        val badgeContainer: LinearLayout = view.findViewById(R.id.badge_container)
        val ivType: ImageView = view.findViewById(R.id.iv_record_type)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context).inflate(R.layout.item_emission_record, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val record = records[position]
        holder.tvDate.text = record.date
        holder.tvType.text = record.type
        holder.tvDesc.text = record.desc
        holder.tvAmount.text = "${record.amount} kg CO₂e"

        // 🌟 匹配 Web 端样式的彩色标签逻辑
        when (record.type) {
            "Food" -> {
                holder.badgeContainer.setBackgroundResource(R.drawable.shape_badge_food) // 浅紫色背景
                holder.tvType.setTextColor(Color.parseColor("#674fa3"))
                holder.ivType.setImageResource(R.drawable.main_eat_purple)
            }
            "Transport" -> {
                holder.badgeContainer.setBackgroundColor(Color.parseColor("#E3F2FD")) // 浅蓝色背景
                holder.tvType.setTextColor(Color.parseColor("#1976D2"))
                holder.ivType.setImageResource(R.drawable.main_travel_purple)
            }
            "Utilities" -> {
                holder.badgeContainer.setBackgroundColor(Color.parseColor("#FFFDE7")) // 浅黄色背景
                holder.tvType.setTextColor(Color.parseColor("#FBC02D"))
                holder.ivType.setImageResource(R.drawable.main_water_purple)
            }
        }
    }

    override fun getItemCount() = records.size
}