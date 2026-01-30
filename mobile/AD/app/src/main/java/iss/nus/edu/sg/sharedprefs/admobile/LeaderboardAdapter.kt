package iss.nus.edu.sg.sharedprefs.admobile

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageView
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.bumptech.glide.Glide

data class LeaderboardItem(
    val rank: Int,
    val username: String,
    val nickname: String,
    val avatarUrl: String?,
    val pointsTotal: Int,
    val emissionsTotal: Double
)

class LeaderboardAdapter(private val items: List<LeaderboardItem>) : RecyclerView.Adapter<LeaderboardAdapter.ViewHolder>() {

    class ViewHolder(view: View) : RecyclerView.ViewHolder(view) {
        val tvRankNum: TextView = view.findViewById(R.id.tv_rank_num)
        val ivAvatar: ImageView = view.findViewById(R.id.iv_avatar)
        val tvUsername: TextView = view.findViewById(R.id.tv_username)
        val tvPoints: TextView = view.findViewById(R.id.tv_points)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context).inflate(R.layout.item_ranking_row, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val item = items[position]
        holder.tvRankNum.text = item.rank.toString()
        holder.tvUsername.text = item.nickname.ifEmpty { item.username }
        holder.tvPoints.text = "${item.pointsTotal} Pts"
        
        // 加载头像图片
        val avatarUrl = ApiHelper.buildAvatarUrl(holder.itemView.context, item.avatarUrl)
        if (!avatarUrl.isNullOrEmpty()) {
            Glide.with(holder.itemView.context)
                .load(avatarUrl)
                .placeholder(R.drawable.ic_avatar_placeholder)
                .error(R.drawable.ic_avatar_placeholder)
                .circleCrop()
                .into(holder.ivAvatar)
        } else {
            holder.ivAvatar.setImageResource(R.drawable.ic_avatar_placeholder)
        }
    }

    override fun getItemCount() = items.size
}
