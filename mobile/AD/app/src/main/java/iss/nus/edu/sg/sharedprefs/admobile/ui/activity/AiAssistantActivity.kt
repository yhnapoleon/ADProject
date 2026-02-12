package iss.nus.edu.sg.sharedprefs.admobile.ui.activity

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.EditText
import android.widget.ImageButton
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import iss.nus.edu.sg.sharedprefs.admobile.R
import iss.nus.edu.sg.sharedprefs.admobile.data.model.ChatRequest
import iss.nus.edu.sg.sharedprefs.admobile.data.network.NetworkClient
import iss.nus.edu.sg.sharedprefs.admobile.utils.NavigationUtils
import kotlinx.coroutines.launch

data class ChatMessage(val content: String, val isBot: Boolean)

class AiAssistantActivity : AppCompatActivity() {

    private val messages = mutableListOf<ChatMessage>()
    private lateinit var adapter: ChatAdapter
    private lateinit var chatRecycler: RecyclerView
    private lateinit var etMessage: EditText

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_ai_assistant)

        chatRecycler = findViewById(R.id.chat_recycler)
        etMessage = findViewById(R.id.et_message)
        val btnSend: ImageButton = findViewById(R.id.btn_send)

        adapter = ChatAdapter(messages)
        chatRecycler.layoutManager = LinearLayoutManager(this)
        chatRecycler.adapter = adapter

        // 初始化快捷 Tips 点击事件
        setupQuickTips()

        addMessage("Hello! I'm your AI Carbon Guide. How can I help you reduce your footprint today?", true)

        btnSend.setOnClickListener {
            val text = etMessage.text.toString().trim()
            if (text.isNotEmpty()) {
                sendMessageToAi(text)
                etMessage.text.clear()
            }
        }

        NavigationUtils.setupBottomNavigation(this, R.id.nav_chat)
    }

    private fun setupQuickTips() {
        findViewById<TextView>(R.id.tip_food).setOnClickListener {
            sendMessageToAi("Give me some low-carbon food tips.")
        }
        findViewById<TextView>(R.id.tip_transport).setOnClickListener {
            sendMessageToAi("How can I reduce carbon in transport?")
        }
        findViewById<TextView>(R.id.tip_utilities).setOnClickListener {
            sendMessageToAi("Tell me about utility-saving habits.")
        }
    }

    private fun addMessage(text: String, isBot: Boolean) {
        messages.add(ChatMessage(text, isBot))
        adapter.notifyItemInserted(messages.size - 1)
        chatRecycler.scrollToPosition(messages.size - 1)
    }

    private fun sendMessageToAi(userText: String) {
        addMessage(userText, false)

        lifecycleScope.launch {
            try {
                val request = ChatRequest(userText)
                val response = NetworkClient.apiService.postChatMessage(request)

                if (response.isSuccessful && response.body() != null) {
                    val botReply = response.body()!!.reply
                    addMessage(botReply, true)
                } else {
                    addMessage("Sorry, the server is having trouble. Please try again later.", true)
                }
            } catch (e: Exception) {
                addMessage("Network error. Please check your connection.", true)
            }
        }
    }

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