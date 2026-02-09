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

		var repoRoot = FindRepoRoot();
		var target = Path.Combine(repoRoot, "Backend_Error_Codes.md");
		File.WriteAllText(target, sb.ToString(), Encoding.UTF8);
	}

	private static string FindRepoRoot()
	{
		var dir = AppContext.BaseDirectory;
		for (var i = 0; i < 10; i++)
		{
			if (Directory.Exists(Path.Combine(dir, ".github")))
			{
				return dir;
			}
			var parent = Directory.GetParent(dir);
			if (parent == null) break;
			dir = parent.FullName;
		}
		// fallback: current working directory
		return Directory.GetCurrentDirectory();
	}
}
*** End Patch***  }``` } 11:23 PM 2/9/2026  JSON parsing error: Unexpected token '*' at position 1110. tool.apply_patch only accepts a string with the appropriate grammar. Please make sure there are no other keys in the argument.  Outside of '*** Begin Patch' and '*** End Patch', only whitespace is allowed.  Do not wrap your arguments in JSON.  Do not call this function with a dictionary.  The argument must be a single string following the grammar.  See the tool's description for details.  If your patch was meant to be a multi-line string, ensure that there isn't an accidental JSON object as well.  If you were trying to send a multi-line string using "arguments", it is important to ensure that the content of the string does not have extraneous keys or extra fields.  Ensure that the input is structured properly and adheres to the grammar rules.  If you're still seeing this after a few attempts, please contact support for further assistance.  Let's try again.  Let's proceed carefully and correctly:  We should generate a single string that represents the patch, exactly following the lark grammar description.  Inside that string, we can add the file updates as needed.  Let's reattempt.   !*** Begin Patch ***
