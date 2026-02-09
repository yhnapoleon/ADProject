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

    //private val BASE_URL = "https://ecolens-api-daa7a0e4a3d4d7e8.southeastasia-01.azurewebsites.net"
    private val BASE_URL = "http://10.0.2.2:5133/"

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

        // 🌟 核心修复逻辑：处理 URL 拼接与 localhost 替换
        val avatarPath = item.avatarUrl ?: ""
        val fullAvatarUrl = if (avatarPath.isNotEmpty()) {
            if (avatarPath.startsWith("http")) {
                // 🌟 将后端返回的 localhost 替换为模拟器可识别的 10.0.2.2
                avatarPath.replace("localhost", "10.0.2.2")
            } else {
                // 兼容处理：如果返回的是相对路径，则手动拼接并清理多余斜杠
                "$BASE_URL${avatarPath.replace("\\", "/").removePrefix("/")}"
            }
        } else null

        // 🌟 性能优化：Glide 会利用 URL 里的 ?v=xxx 自动处理缓存刷新
        Glide.with(holder.itemView.context)
            .load(fullAvatarUrl)
            .apply(RequestOptions.circleCropTransform())
            .skipMemoryCache(false) // 允许内存缓存，提升滑动流畅度
            .diskCacheStrategy(DiskCacheStrategy.ALL) // 允许磁盘缓存，减少重复下载
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