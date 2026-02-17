using EcoLens.Api.Common.Errors;
using Xunit;

namespace EcoLens.Tests.Common;

public class ErrorRegistryTests
{
	[Fact]
	public void GetAll_ReturnsNonEmptyList()
	{
		var all = ErrorRegistry.GetAll();
		Assert.NotNull(all);
		Assert.NotEmpty(all);
		Assert.All(all, e =>
		{
			Assert.True(e.ErrorCode > 0);
			Assert.NotNull(e.TechnicalMessage);
			Assert.NotNull(e.UserMessage);
		});
	}

	[Fact]
	public void MapExceptionToDescriptor_Returns5000_WhenExceptionIsNull()
	{
		var d = ErrorRegistry.MapExceptionToDescriptor(null!);
		Assert.Equal(5000, d.ErrorCode);
	}

	[Fact]
	public void MapExceptionToDescriptor_Returns1001_WhenTimeoutException()
	{
		var d = ErrorRegistry.MapExceptionToDescriptor(new TimeoutException());
		Assert.Equal(1001, d.ErrorCode);
	}

	[Fact]
	public void MapExceptionToDescriptor_Returns1001_WhenTaskCanceledException()
	{
		var d = ErrorRegistry.MapExceptionToDescriptor(new TaskCanceledException());
		Assert.Equal(1001, d.ErrorCode);
	}

	[Fact]
	public void MapExceptionToDescriptor_Returns2001_WhenUnauthorizedAccessException()
	{
		var d = ErrorRegistry.MapExceptionToDescriptor(new UnauthorizedAccessException());
		Assert.Equal(2001, d.ErrorCode);
	}

	[Fact]
	public void MapExceptionToDescriptor_Returns3001_WhenArgumentException()
	{
		var d = ErrorRegistry.MapExceptionToDescriptor(new ArgumentException("bad"));
		Assert.Equal(3001, d.ErrorCode);
	}

	[Fact]
	public void MapExceptionToDescriptor_Returns4001_WhenKeyNotFoundException()
	{
		var d = ErrorRegistry.MapExceptionToDescriptor(new KeyNotFoundException());
		Assert.Equal(4001, d.ErrorCode);
	}

	[Fact]
	public void MapExceptionToDescriptor_Returns5000_WhenOtherException()
	{
		var d = ErrorRegistry.MapExceptionToDescriptor(new InvalidOperationException());
		Assert.Equal(5000, d.ErrorCode);
	}
}
