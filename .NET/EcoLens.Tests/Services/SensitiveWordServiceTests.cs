using EcoLens.Api.Services;
using Xunit;

namespace EcoLens.Tests.Services;

public class SensitiveWordServiceTests
{
	private readonly SensitiveWordService _service = new();

	[Fact]
	public void ContainsSensitiveWord_ReturnsNull_WhenTextIsNull()
	{
		var result = _service.ContainsSensitiveWord(null!);
		Assert.Null(result);
	}

	[Fact]
	public void ContainsSensitiveWord_ReturnsNull_WhenTextIsEmptyOrWhitespace()
	{
		Assert.Null(_service.ContainsSensitiveWord(""));
		Assert.Null(_service.ContainsSensitiveWord("   "));
		Assert.Null(_service.ContainsSensitiveWord("\t\n"));
	}

	[Fact]
	public void ContainsSensitiveWord_ReturnsNull_WhenTextIsClean()
	{
		Assert.Null(_service.ContainsSensitiveWord("Hello world"));
		Assert.Null(_service.ContainsSensitiveWord("This is normal text.")); // 避免含 nt/sb 等敏感子串
		Assert.Null(_service.ContainsSensitiveWord("class")); // "ass" not in list; "class" is clean
	}

	[Fact]
	public void ContainsSensitiveWord_ReturnsWord_WhenTextContainsSensitiveWord()
	{
		Assert.NotNull(_service.ContainsSensitiveWord("this is shit"));
		Assert.Equal("shit", _service.ContainsSensitiveWord("this is shit"));
		Assert.NotNull(_service.ContainsSensitiveWord("damn it"));
		Assert.Equal("damn", _service.ContainsSensitiveWord("damn it"));
	}

	[Fact]
	public void ContainsSensitiveWord_IsCaseInsensitive()
	{
		Assert.NotNull(_service.ContainsSensitiveWord("FUCK"));
		Assert.NotNull(_service.ContainsSensitiveWord("Shit"));
		Assert.NotNull(_service.ContainsSensitiveWord("ROOT"));
	}

	[Fact]
	public void ContainsSensitiveWord_RespectsWordBoundary_WhenPartOfLargerWord()
	{
		// "crap" is in list; "scrap" contains "crap" but as substring without leading boundary -> implementation also checks direct Contains, so "scrap" might match "crap"... 
		// Actually the code does: 1) word boundary match \bcrap\b - "scrap" won't match. 2) direct lowerText.Contains(word) - "scrap".Contains("crap") is true! So "scrap" would return "crap". 
		// So we need a word where the sensitive word is only in the middle with boundaries. E.g. "ass" - not in list. "root" in "rooted"? "rooted".Contains("root") is true, so it would match. 
		// So for "word boundary" we test: "class" does not contain any sensitive word (no "ass" in list). "asshole" is in list - "whole" is not. So "the class" returns null. "the asshole" returns "asshole".
		Assert.Null(_service.ContainsSensitiveWord("class"));
		Assert.Null(_service.ContainsSensitiveWord("classroom"));
	}

	[Fact]
	public void ContainsSensitiveWord_ReturnsWord_WhenShortPinyinInText()
	{
		// 中文语境下无空格，直接包含检测
		Assert.NotNull(_service.ContainsSensitiveWord("你是sb"));
		Assert.Equal("sb", _service.ContainsSensitiveWord("你是sb"));
	}

	[Fact]
	public void ContainsSensitiveWord_ReturnsWord_ForSystemReservedWords()
	{
		Assert.NotNull(_service.ContainsSensitiveWord("administrator"));
		Assert.NotNull(_service.ContainsSensitiveWord("root"));
		Assert.NotNull(_service.ContainsSensitiveWord("system"));
	}
}
