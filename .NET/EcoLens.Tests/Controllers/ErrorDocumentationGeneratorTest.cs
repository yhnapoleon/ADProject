using System.Text;
using EcoLens.Api.Common.Errors;

namespace EcoLens.Tests;

public class ErrorDocumentationGeneratorTest
{
	[Fact]
	public void Generate_Backend_Error_Codes_Markdown()
	{
		var sb = new StringBuilder();
		sb.AppendLine("| Error Code | Technical Reason | User Friendly Message |");
		sb.AppendLine("|---|---|---|");

		foreach (var e in ErrorRegistry.GetAll())
		{
			sb.AppendLine($"| {e.ErrorCode} | {e.TechnicalMessage} | {e.UserMessage} |");
		}

		var repoRoot = GetWorkspaceRoot();
		var target = Path.Combine(repoRoot, "Backend_Error_Codes.md");
		File.WriteAllText(target, sb.ToString(), Encoding.UTF8);
	}

	private static string GetWorkspaceRoot()
	{
		// 1) 优先使用 GitHub Actions 的工作区环境变量
		var workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
		if (!string.IsNullOrWhiteSpace(workspace) && Directory.Exists(workspace))
		{
			return workspace;
		}

		// 2) 回退：向上查找包含 .github 或 .git 的目录
		var dir = AppContext.BaseDirectory;
		for (var i = 0; i < 15; i++)
		{
			if (Directory.Exists(Path.Combine(dir, ".github")) || Directory.Exists(Path.Combine(dir, ".git")))
			{
				return dir;
			}
			var parent = Directory.GetParent(dir);
			if (parent == null) break;
			dir = parent.FullName;
		}
		// 3) 最后回退到当前工作目录
		return Directory.GetCurrentDirectory();
	}
}

