package iss.nus.edu.sg.sharedprefs.admobile.utils

object UserFriendlyErrorHandler {
    private val mappings: Map<Class<out Throwable>, String> = mapOf(
        java.net.SocketTimeoutException::class.java to "网络连接超时，请检查网络",
        retrofit2.HttpException::class.java to "服务器开小差了，请稍后重试",
        java.io.IOException::class.java to "网络异常，请检查网络",
        IllegalArgumentException::class.java to "输入无效，请检查填写内容",
        IllegalStateException::class.java to "操作不被允许，请稍后重试"
    )

    fun toUserMessage(throwable: Throwable): String {
        val entry = mappings.entries.firstOrNull { it.key.isInstance(throwable) }
        return entry?.value ?: "发生未知错误，请稍后重试"
    }

    fun allMappings(): Map<String, String> {
        return mappings.mapKeys { it.key.simpleName }
    }
}

