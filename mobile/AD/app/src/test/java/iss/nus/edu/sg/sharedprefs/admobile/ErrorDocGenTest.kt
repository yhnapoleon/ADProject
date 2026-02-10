package iss.nus.edu.sg.sharedprefs.admobile

import iss.nus.edu.sg.sharedprefs.admobile.utils.UserFriendlyErrorHandler
import org.junit.Test
import java.io.File

class ErrorDocGenTest {
    @Test
    fun generateAndroidErrorMappingMarkdown() {
        val mappings = UserFriendlyErrorHandler.allMappings()
        val sb = StringBuilder()
        sb.appendLine("| Exception | User Friendly Message |")
        sb.appendLine("|---|---|")
        mappings.forEach { (k, v) ->
            sb.appendLine("| $k | $v |")
        }
        // 写入到模块根目录
        File("Android_Error_Mapping.md").writeText(sb.toString())
    }
}

