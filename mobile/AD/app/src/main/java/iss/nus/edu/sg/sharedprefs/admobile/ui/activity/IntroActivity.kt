package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.content.Intent
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Button
import android.widget.ImageView
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.RecyclerView
import androidx.viewpager2.widget.ViewPager2
import iss.nus.edu.sg.sharedprefs.admobile.R

data class IntroItem(val title: String, val description: String, val imageResId: Int)

class IntroActivity : AppCompatActivity() {

    private lateinit var viewPager: ViewPager2
    private lateinit var btnNext: Button
    private lateinit var tvSkip: TextView

    private val introItems = listOf(
        IntroItem("Track Your Carbon Footprint", "Understand the environmental impact of your daily activities.", R.drawable.intro1),
        IntroItem("Set Emission Goals", "Set and track your personal goals to reduce emissions and contribute to the planet.", R.drawable.intro2),
        IntroItem("Build Green Habits", "Develop a sustainable lifestyle through small, impactful changes.", R.drawable.intro3)
    )

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.intro_activity)

        viewPager = findViewById(R.id.viewPager)
        btnNext = findViewById(R.id.btnNext)
        tvSkip = findViewById(R.id.tvSkip)

        viewPager.adapter = IntroAdapter(introItems)

        // Skip 按钮点击事件：直接跳转到登录页
        tvSkip.setOnClickListener {
            navigateToLogin()
        }

        viewPager.registerOnPageChangeCallback(object : ViewPager2.OnPageChangeCallback() {
            override fun onPageSelected(position: Int) {
                super.onPageSelected(position)
                if (position == introItems.size - 1) {
                    btnNext.text = "Finish"
                    tvSkip.visibility = View.GONE // 最后一页隐藏 Skip 按钮
                } else {
                    btnNext.text = "Next"
                    tvSkip.visibility = View.VISIBLE // 非最后一页显示 Skip 按钮
                }
            }
        })

        btnNext.setOnClickListener {
            if (viewPager.currentItem < introItems.size - 1) {
                viewPager.currentItem += 1
            } else {
                navigateToLogin()
            }
        }
    }

    // 🌟 封装跳转方法
    private fun navigateToLogin() {
        startActivity(Intent(this, LoginActivity::class.java))
        finish()
    }

    inner class IntroAdapter(private val items: List<IntroItem>) : RecyclerView.Adapter<IntroAdapter.IntroViewHolder>() {

        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): IntroViewHolder {
            val view = LayoutInflater.from(parent.context).inflate(R.layout.intro_page, parent, false)
            return IntroViewHolder(view)
        }

        override fun onBindViewHolder(holder: IntroViewHolder, position: Int) {
            holder.bind(items[position])
        }

        override fun getItemCount(): Int = items.size

        inner class IntroViewHolder(itemView: View) : RecyclerView.ViewHolder(itemView) {
            private val image = itemView.findViewById<ImageView>(R.id.imageView)
            private val title = itemView.findViewById<TextView>(R.id.textTitle)
            private val description = itemView.findViewById<TextView>(R.id.textDescription)

            fun bind(item: IntroItem) {
                image.setImageResource(item.imageResId)
                title.text = item.title
                description.text = item.description
            }
        }
    }
}