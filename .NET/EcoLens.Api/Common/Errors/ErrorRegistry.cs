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
