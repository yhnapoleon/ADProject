using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoLens.Api.Common.Errors
{
	public sealed class ErrorDescriptor
	{
		public int ErrorCode { get; }
		public string TechnicalMessage { get; }
		public string UserMessage { get; }

		public ErrorDescriptor(int errorCode, string technicalMessage, string userMessage)
		{
			ErrorCode = errorCode;
			TechnicalMessage = technicalMessage;
			UserMessage = userMessage;
		}
	}

	public static class ErrorRegistry
	{
		private static readonly List<ErrorDescriptor> _errors = new()
		{
			new ErrorDescriptor(1001, "DbConnectionTimeout", "服务器连接超时，请检查网络"),
			new ErrorDescriptor(1002, "DbOperationFailed", "数据读取失败，请稍后重试"),
			new ErrorDescriptor(2001, "AuthenticationFailed", "登录已过期，请重新登录"),
			new ErrorDescriptor(3001, "ValidationError", "输入无效，请检查填写内容"),
			new ErrorDescriptor(4001, "ResourceNotFound", "资源不存在或已被删除"),
			new ErrorDescriptor(5000, "InternalServerError", "服务器开小差了，请稍后重试")
		};

		public static IReadOnlyList<ErrorDescriptor> GetAll() => _errors;

		public static ErrorDescriptor MapExceptionToDescriptor(Exception exception)
		{
			if (exception is null) return _errors.First(e => e.ErrorCode == 5000);

			return exception switch
			{
				TimeoutException => _errors.First(e => e.ErrorCode == 1001),
				TaskCanceledException => _errors.First(e => e.ErrorCode == 1001),
				UnauthorizedAccessException => _errors.First(e => e.ErrorCode == 2001),
				ArgumentException => _errors.First(e => e.ErrorCode == 3001),
				KeyNotFoundException => _errors.First(e => e.ErrorCode == 4001),
				_ => _errors.First(e => e.ErrorCode == 5000)
			};
		}
	}
}
*** End Patch***  }``` } 11:22 PM 2/9/2026  JSON parsing error: Unexpected token '*' at position 1029. tool.apply_patch only accepts a string with the appropriate grammar. Please make sure there are no other keys in the argument.  Outside of '*** Begin Patch' and '*** End Patch', only whitespace is allowed.  Do not wrap your arguments in JSON.  Do not call this function with a dictionary.  The argument must be a single string following the grammar.  See the tool's description for details.  Error: Could not parse the JSON input.  Please ensure the input is in the correct format.  Retry your call with valid JSON.  If your patch was meant to be a multi-line string, ensure that there isn't an accidental JSON object as well.  If you were trying to send a multi-line string using "arguments", it is important to ensure that the content of the string does not have extraneous keys or extra fields.  Ensure that the input is structured properly and adheres to the grammar rules.  If you're still seeing this after a few attempts, please contact support for further assistance.  Let's try again.  Let's proceed carefully and correctly:  We should generate a single string that represents the patch, exactly following the lark grammar description.  Inside that string, we can add the file updates as needed.  Let's reattempt.   !*** Begin Patch ***
