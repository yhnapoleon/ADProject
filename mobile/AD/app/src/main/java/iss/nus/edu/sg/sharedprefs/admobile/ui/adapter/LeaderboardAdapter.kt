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
import iss.nus.edu.sg.sharedprefs.admobile.data.model.RankingType // 🌟 引入共享枚举

class LeaderboardAdapter(private var items: List<LeaderboardItem>) :
    RecyclerView.Adapter<LeaderboardAdapter.ViewHolder>() {

    private var currentType: RankingType = RankingType.DAILY

    private val BASE_URL = "http://10.0.2.2:5133/"

    fun updateData(newItems: List<LeaderboardItem>, type: RankingType) {
        this.items = newItems
        this.currentType = type
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_ranking_row, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val item = items[position]
        holder.tvRank.text = (position + 4).toString()
        holder.tvName.text = item.nickname ?: item.username

        // 根据共享枚举展示积分
        val displayPoints = when (currentType) {
            RankingType.DAILY -> item.pointsToday
            RankingType.MONTHLY -> item.pointsMonth
            RankingType.TOTAL -> item.pointsTotal
        }

        holder.tvValue.text = String.format("%.1f kg | %d pts", item.emissionsTotal, displayPoints)

        val avatarPath = item.avatarUrl ?: ""
        val fullAvatarUrl = if (avatarPath.isNotEmpty()) {
            if (avatarPath.startsWith("http")) {
                avatarPath.replace("localhost", "10.0.2.2")
            } else {
                "$BASE_URL${avatarPath.replace("\\", "/").removePrefix("/")}"
            }
        } else null

        Glide.with(holder.itemView.context)
            .load(fullAvatarUrl)
            .apply(RequestOptions.circleCropTransform())
            .skipMemoryCache(false)
            .diskCacheStrategy(DiskCacheStrategy.ALL)
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