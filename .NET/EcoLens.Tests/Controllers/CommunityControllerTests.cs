using System.Security.Claims;
using EcoLens.Api.Controllers;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Tests.Controllers;

public class CommunityControllerTests
{
	private static async Task<ApplicationDbContext> CreateDbAsync(bool withUser = false, bool withPost = false)
	{
		var options = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		var db = new ApplicationDbContext(options);
		if (withUser)
		{
			db.ApplicationUsers.Add(new ApplicationUser
			{
				Username = "u1",
				Email = "u1@t.com",
				PasswordHash = "x",
				Role = UserRole.User,
				Region = "SG",
				BirthDate = DateTime.UtcNow.AddYears(-20)
			});
			await db.SaveChangesAsync();
		}
		if (withPost)
		{
			var user = await db.ApplicationUsers.FirstAsync();
			db.Posts.Add(new Post { UserId = user.Id, Title = "Test Post", Content = "Content", Type = PostType.User });
			await db.SaveChangesAsync();
		}
		return db;
	}

	private static void SetUser(ControllerBase controller, int userId)
	{
		var identity = new ClaimsIdentity();
		identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
		controller.ControllerContext = new ControllerContext
		{
			HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
		};
	}

	[Fact]
	public async Task GetPosts_ReturnsBadRequest_WhenInvalidType()
	{
		await using var db = await CreateDbAsync();
		var controller = new CommunityController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

		var result = await controller.GetPosts("Invalid", 1, 10, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("Invalid type value.", badRequest.Value);
	}

	[Fact]
	public async Task GetPosts_ReturnsOkWithList_WhenValid()
	{
		await using var db = await CreateDbAsync(withUser: true, withPost: true);
		var controller = new CommunityController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } } };

		var result = await controller.GetPosts(null, 1, 10, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var list = Assert.IsAssignableFrom<IEnumerable<CommunityController.PostListItemDto>>(ok.Value);
		Assert.Single(list);
		Assert.Equal("Test Post", list.First().Title);
	}

	[Fact]
	public async Task GetPostDetail_ReturnsNotFound_WhenPostMissing()
	{
		await using var db = await CreateDbAsync(true);
		var controller = new CommunityController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } } };

		var result = await controller.GetPostDetail(99999, CancellationToken.None);

		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task GetPostDetail_ReturnsOk_WhenPostExists()
	{
		await using var db = await CreateDbAsync(withUser: true, withPost: true);
		var post = await db.Posts.FirstAsync();
		var controller = new CommunityController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Scheme = "https", Host = new HostString("localhost") } } };

		var result = await controller.GetPostDetail(post.Id, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<CommunityController.PostDetailDto>(ok.Value);
		Assert.Equal(post.Id, dto.Id);
		Assert.Equal("Test Post", dto.Title);
		Assert.Equal(1, dto.ViewCount);
	}

	[Fact]
	public async Task CreatePost_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = await CreateDbAsync(true);
		var controller = new CommunityController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

		var result = await controller.CreatePost(new CommunityController.CreatePostDto { Title = "T", Content = "C" }, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task CreatePost_ReturnsBadRequest_WhenTitleOrContentEmpty()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new CommunityController(db);
		SetUser(controller, user.Id);
		controller.ControllerContext.HttpContext ??= new DefaultHttpContext();
		controller.ControllerContext.HttpContext.Request.Scheme = "https";
		controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");

		var result = await controller.CreatePost(new CommunityController.CreatePostDto { Title = "", Content = "C" }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("Title and Content are required.", badRequest.Value);
	}

	[Fact]
	public async Task CreatePost_ReturnsOk_WhenValid()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new CommunityController(db);
		SetUser(controller, user.Id);
		controller.ControllerContext.HttpContext ??= new DefaultHttpContext();
		controller.ControllerContext.HttpContext.Request.Scheme = "https";
		controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");

		var result = await controller.CreatePost(new CommunityController.CreatePostDto { Title = "New", Content = "Body" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<CommunityController.PostDetailDto>(ok.Value);
		Assert.Equal("New", dto.Title);
		Assert.Equal("Body", dto.Content);
		var post = await db.Posts.FirstOrDefaultAsync(p => p.Title == "New");
		Assert.NotNull(post);
	}

	[Fact]
	public async Task CreateComment_ReturnsUnauthorized_WhenUserNotSet()
	{
		await using var db = await CreateDbAsync(withUser: true, withPost: true);
		var controller = new CommunityController(db);
		controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };
		var post = await db.Posts.FirstAsync();

		var result = await controller.CreateComment(post.Id, new CommunityController.CreateCommentDto { Content = "comment" }, CancellationToken.None);

		Assert.IsType<UnauthorizedResult>(result.Result);
	}

	[Fact]
	public async Task CreateComment_ReturnsNotFound_WhenPostMissing()
	{
		await using var db = await CreateDbAsync(true);
		var user = await db.ApplicationUsers.FirstAsync();
		var controller = new CommunityController(db);
		SetUser(controller, user.Id);

		var result = await controller.CreateComment(99999, new CommunityController.CreateCommentDto { Content = "comment" }, CancellationToken.None);

		Assert.IsType<NotFoundResult>(result.Result);
	}

	[Fact]
	public async Task CreateComment_ReturnsBadRequest_WhenContentEmpty()
	{
		await using var db = await CreateDbAsync(withUser: true, withPost: true);
		var user = await db.ApplicationUsers.FirstAsync();
		var post = await db.Posts.FirstAsync();
		var controller = new CommunityController(db);
		SetUser(controller, user.Id);

		var result = await controller.CreateComment(post.Id, new CommunityController.CreateCommentDto { Content = "   " }, CancellationToken.None);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
		Assert.Equal("Content is required.", badRequest.Value);
	}

	[Fact]
	public async Task CreateComment_ReturnsOk_WhenValid()
	{
		await using var db = await CreateDbAsync(withUser: true, withPost: true);
		var user = await db.ApplicationUsers.FirstAsync();
		var post = await db.Posts.FirstAsync();
		var controller = new CommunityController(db);
		SetUser(controller, user.Id);

		var result = await controller.CreateComment(post.Id, new CommunityController.CreateCommentDto { Content = "My comment" }, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result.Result);
		var dto = Assert.IsType<CommunityController.CommentDto>(ok.Value);
		Assert.Equal("My comment", dto.Content);
		Assert.Equal(user.Id, dto.UserId);
		var comment = await db.Comments.FirstOrDefaultAsync(c => c.PostId == post.Id);
		Assert.NotNull(comment);
	}
}
