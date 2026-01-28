package iss.nus.edu.sg.sharedprefs.admobile

import android.content.Intent
import android.graphics.Color
import android.os.Bundle
import android.view.View
import android.widget.EditText
import android.widget.ImageView
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import com.google.android.material.button.MaterialButton

class ProfileActivity : AppCompatActivity() {

    private var isEditing = false
    private lateinit var editBtn: MaterialButton

    // 定义参与编辑的条目 ID (手动排除 item_join_date)
    private val editableIds = listOf(
        R.id.item_username, R.id.item_nickname, R.id.item_email,
        R.id.item_password, R.id.item_birth, R.id.item_location
    )

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_profile)

        // 设置状态栏颜色
        window.statusBarColor = Color.parseColor("#674fa3")

        // 1. 初始化展示数据
        setupProfileData()

        // 2. 绑定编辑/保存按钮逻辑
        editBtn = findViewById(R.id.btn_edit_profile)
        editBtn.setOnClickListener {
            toggleEditMode()
        }

        val historyCard = findViewById<View>(R.id.card_history)
        historyCard.setOnClickListener {
            // 创建跳转意图，从当前页面跳往 EmissionRecordsActivity
            val intent = Intent(this, EmissionRecordsActivity::class.java)
            startActivity(intent)
        }

        // 3. 初始化统一导航栏
        NavigationUtils.setupBottomNavigation(this, R.id.nav_person)
    }

    /**
     * 核心逻辑：切换编辑与展示模式
     */
    private fun toggleEditMode() {
        isEditing = !isEditing

        for (id in editableIds) {
            val itemView = findViewById<View>(id)
            val tvValue = itemView.findViewById<TextView>(R.id.info_value)
            val etEdit = itemView.findViewById<EditText>(R.id.info_edit)

            if (isEditing) {
                // 进入编辑状态：显示输入框，填入当前值，隐藏文本
                etEdit.setText(tvValue.text)
                etEdit.visibility = View.VISIBLE
                tvValue.visibility = View.GONE
            } else {
                // 保存状态：将输入框内容写回文本，隐藏输入框
                tvValue.text = etEdit.text.toString()
                etEdit.visibility = View.GONE
                tvValue.visibility = View.VISIBLE

                // 同步更新顶部卡片的展示名和 Email
                if (id == R.id.item_username) findViewById<TextView>(R.id.profile_name).text = tvValue.text
                if (id == R.id.item_email) findViewById<TextView>(R.id.profile_email).text = tvValue.text
            }
        }

        // 修改按钮文案反馈
        editBtn.text = if (isEditing) "Save Changes" else "Edit Profile"
    }

    private fun setupProfileData() {
        setInfo(R.id.item_username, "Username", "Melody")
        setInfo(R.id.item_nickname, "Nickname", "EcoRanger")
        setInfo(R.id.item_email, "Email", "melody@example.com")
        setInfo(R.id.item_password, "Password", "••••••••")
        setInfo(R.id.item_birth, "Birth Date", "March 15, 1995")
        setInfo(R.id.item_location, "Location", "West Region")
        setInfo(R.id.item_join_date, "Join Date", "September 20, 2025")
    }

    private fun setInfo(viewId: Int, label: String, value: String) {
        val root = findViewById<View>(viewId)
        root.findViewById<TextView>(R.id.info_label).text = label
        root.findViewById<TextView>(R.id.info_value).text = value
    }
}