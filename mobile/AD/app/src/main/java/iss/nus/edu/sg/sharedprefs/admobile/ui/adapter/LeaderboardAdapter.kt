package iss.nus.edu.sg.sharedprefs.admobile.ui.adapter

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageView
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.bumptech.glide.Glide
import com.bumptech.glide.load.engine.DiskCacheStrategy
import com.bumptech.glide.request.RequestOptions
import iss.nus.edu.sg.sharedprefs.admobile.R
import iss.nus.edu.sg.sharedprefs.admobile.data.model.LeaderboardItem

class LeaderboardAdapter(private var items: List<LeaderboardItem>) :
    RecyclerView.Adapter<LeaderboardAdapter.ViewHolder>() {

    private val BASE_URL = "https://ecolens-api-daa7a0e4a3d4d7e8.southeastasia-01.azurewebsites.net"

    fun updateData(newItems: List<LeaderboardItem>) {
        this.items = newItems
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_ranking_row, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val item = items[position]
        holder.tvRank.text = item.rank.toString()
        holder.tvName.text = item.nickname ?: item.username
        holder.tvValue.text = String.format("%.2f kg", item.emissionsTotal)

        // 🌟 统一头像处理逻辑
        var avatarPath = item.avatarUrl ?: ""
        val fullAvatarUrl = if (avatarPath.isNotEmpty()) {
            if (avatarPath.startsWith("http")) avatarPath
            else "$BASE_URL${avatarPath.replace("\\", "/")}"
        } else null

        // 🌟 强制禁用缓存加载
        Glide.with(holder.itemView.context)
            .load(fullAvatarUrl)
            .apply(RequestOptions.circleCropTransform())
            .skipMemoryCache(true) // 🌟 跳过内存
            .diskCacheStrategy(DiskCacheStrategy.NONE) // 🌟 跳过磁盘
            .placeholder(R.drawable.ic_avatar_placeholder)
            .error(R.drawable.ic_avatar_placeholder)
            .into(holder.ivAvatar)
    }

    override fun getItemCount() = items.size

    class ViewHolder(view: View) : RecyclerView.ViewHolder(view) {
        val tvRank: TextView = view.findViewById(R.id.tv_rank_num)
        val tvName: TextView = view.findViewById(R.id.tv_user_name)
        val tvValue: TextView = view.findViewById(R.id.tv_carbon_value)
        val ivAvatar: ImageView = view.findViewById(R.id.iv_user_avatar)
    }
}