package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.EditText
import android.widget.ImageButton
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import iss.nus.edu.sg.sharedprefs.admobile.utils.NavigationUtils
import iss.nus.edu.sg.sharedprefs.admobile.R

// 数据模型
data class ChatMessage(val content: String, val isBot: Boolean)

class AiAssistantActivity : AppCompatActivity() {

    private val messages = mutableListOf<ChatMessage>()
    private lateinit var adapter: ChatAdapter
    private lateinit var chatRecycler: RecyclerView

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_ai_assistant)

        chatRecycler = findViewById(R.id.chat_recycler)
        val etMessage: EditText = findViewById(R.id.et_message)
        val btnSend: ImageButton = findViewById(R.id.btn_send)

        // 配置列表
        adapter = ChatAdapter(messages)
        chatRecycler.layoutManager = LinearLayoutManager(this)
        chatRecycler.adapter = adapter

        // 初始欢迎语
        addMessage("Hello! I'm your AI Carbon Guide. How can I help you reduce your footprint today?", true)

        btnSend.setOnClickListener {
            val text = etMessage.text.toString().trim()
            if (text.isNotEmpty()) {
                addMessage(text, false)
                etMessage.text.clear()
                simulateBotResponse(text)
            }
        }

        NavigationUtils.setupBottomNavigation(this, R.id.nav_chat)
    }

    private fun addMessage(text: String, isBot: Boolean) {
        messages.add(ChatMessage(text, isBot))
        adapter.notifyItemInserted(messages.size - 1)
        chatRecycler.scrollToPosition(messages.size - 1)
    }

    private fun simulateBotResponse(query: String) {
        Handler(Looper.getMainLooper()).postDelayed({
            val response = when {
                query.contains("food", true) -> "Choosing local produce and reducing red meat can cut your food carbon footprint significantly!"
                query.contains("transport", true) -> "Cycling or taking the subway in Singapore is much cleaner than driving a private car."
                else -> "That's interesting! Small changes in daily habits lead to big environmental impacts."
            }
            addMessage(response, true)
        }, 1000)
    }

    // --- 内部适配器 ---
    inner class ChatAdapter(private val list: List<ChatMessage>) : RecyclerView.Adapter<RecyclerView.ViewHolder>() {

        override fun getItemViewType(position: Int) = if (list[position].isBot) 0 else 1

        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): RecyclerView.ViewHolder {
            val layout = if (viewType == 0) R.layout.item_chat_bot else R.layout.item_chat_user
            val view = LayoutInflater.from(parent.context).inflate(layout, parent, false)
            return ChatViewHolder(view)
        }

        override fun onBindViewHolder(holder: RecyclerView.ViewHolder, position: Int) {
            (holder as ChatViewHolder).tvContent.text = list[position].content
        }

        override fun getItemCount() = list.size

        inner class ChatViewHolder(v: View) : RecyclerView.ViewHolder(v) {
            val tvContent: TextView = v.findViewById(R.id.tv_message)
        }
    }
}