using System.Security.Claims;
using EcoLens.Api.Data;
using EcoLens.Api.Models;
using EcoLens.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoLens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommunityController : ControllerBase
{
	private readonly ApplicationDbContext _db;

	public CommunityController(ApplicationDbContext db)
	{
		_db = db;
	}

	private int? GetUserId()
	{
		var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
		return int.TryParse(id, out var uid) ? uid : null;
	}

	public class PostListItemDto
	{
		public int Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;
		public string? ImageUrls { get; set; }
		public PostType Type { get; set; }
		public int ViewCount { get; set; }
		public DateTime CreatedAt { get; set; }
		public int UserId { get; set; }
		public string Username { get; set; } = string.Empty;
		public string? AvatarUrl { get; set; }
	}

	public class CreatePostDto
	{
		public string Title { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;
		public string? ImageUrls { get; set; }
		public PostType Type { get; set; } = PostType.User;
	}

	public class CommentDto
	{
		public int Id { get; set; }
		public int UserId { get; set; }
		public string Username { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; }
	}

	public class CreateCommentDto
	{
		public string Content { get; set; } = string.Empty;
	}

	public class PostDetailDto
	{
		public int Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;
		public string? ImageUrls { get; set; }
		public PostType Type { get; set; }
		public int ViewCount { get; set; }
		public DateTime CreatedAt { get; set; }
		public int UserId { get; set; }
		public string Username { get; set; } = string.Empty;
		public string? AvatarUrl { get; set; }
		public List<CommentDto> Comments { get; set; } = new();
	}

	/// <summary>
	/// 分页获取帖子列表（可选 type=User/Official）。
	/// </summary>
	[HttpGet("posts")]
	public async Task<ActionResult<IEnumerable<PostListItemDto>>> GetPosts([FromQuery] string? type, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
	{
		if (page <= 0) page = 1;
		if (pageSize <= 0 || pageSize > 100) pageSize = 10;

		IQueryable<Post> query = _db.Posts.AsNoTracking().Include(p => p.User);

		if (!string.IsNullOrWhiteSpace(type))
		{
			if (!Enum.TryParse<PostType>(type, true, out var t))
			{
				return BadRequest("Invalid type value.");
			}
			query = query.Where(p => p.Type == t);
		}

		var items = await query
			.OrderByDescending(p => p.CreatedAt)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(p => new PostListItemDto
			{
				Id = p.Id,
				Title = p.Title,
				Content = p.Content,
				ImageUrls = p.ImageUrls,
				Type = p.Type,
				ViewCount = p.ViewCount,
				CreatedAt = p.CreatedAt,
				UserId = p.UserId,
				Username = p.User != null ? p.User.Username : string.Empty,
				AvatarUrl = p.User != null ? p.User.AvatarUrl : null
			})
			.ToListAsync(ct);

		return Ok(items);
	}

	/// <summary>
	/// 获取帖子详情及评论列表，同时自增浏览量。
	/// </summary>
	[HttpGet("posts/{id:int}")]
	public async Task<ActionResult<PostDetailDto>> GetPostDetail([FromRoute] int id, CancellationToken ct)
	{
		var post = await _db.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id, ct);
		if (post is null) return NotFound();

		post.ViewCount += 1;
		await _db.SaveChangesAsync(ct);

		var comments = await _db.Comments
			.Where(c => c.PostId == id)
			.Include(c => c.User)
			.OrderBy(c => c.CreatedAt)
			.Select(c => new CommentDto
			{
				Id = c.Id,
				UserId = c.UserId,
				Username = c.User != null ? c.User.Username : string.Empty,
				Content = c.Content,
				CreatedAt = c.CreatedAt
			})
			.ToListAsync(ct);

		var dto = new PostDetailDto
		{
			Id = post.Id,
			Title = post.Title,
			Content = post.Content,
			ImageUrls = post.ImageUrls,
			Type = post.Type,
			ViewCount = post.ViewCount,
			CreatedAt = post.CreatedAt,
			UserId = post.UserId,
			Username = post.User != null ? post.User.Username : string.Empty,
			AvatarUrl = post.User != null ? post.User.AvatarUrl : null,
			Comments = comments
		};

		return Ok(dto);
	}

	/// <summary>
	/// 发布新帖子。
	/// </summary>
	[HttpPost("posts")]
	public async Task<ActionResult<PostDetailDto>> CreatePost([FromBody] CreatePostDto dto, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Content))
		{
			return BadRequest("Title and Content are required.");
		}

		var post = new Post
		{
			UserId = userId.Value,
			Title = dto.Title,
			Content = dto.Content,
			ImageUrls = dto.ImageUrls,
			Type = dto.Type
		};

		await _db.Posts.AddAsync(post, ct);
		await _db.SaveChangesAsync(ct);

		// 返回简要详情
		var result = new PostDetailDto
		{
			Id = post.Id,
			Title = post.Title,
			Content = post.Content,
			ImageUrls = post.ImageUrls,
			Type = post.Type,
			ViewCount = post.ViewCount,
			CreatedAt = post.CreatedAt,
			UserId = post.UserId,
			Username = (await _db.ApplicationUsers.Where(u => u.Id == post.UserId).Select(u => u.Username).FirstOrDefaultAsync(ct)) ?? string.Empty,
			AvatarUrl = await _db.ApplicationUsers.Where(u => u.Id == post.UserId).Select(u => u.AvatarUrl).FirstOrDefaultAsync(ct),
			Comments = new List<CommentDto>()
		};

		return Ok(result);
	}

	/// <summary>
	/// 对帖子进行评论。
	/// </summary>
	[HttpPost("posts/{id:int}/comments")]
	public async Task<ActionResult<CommentDto>> CreateComment([FromRoute] int id, [FromBody] CreateCommentDto dto, CancellationToken ct)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var postExists = await _db.Posts.AnyAsync(p => p.Id == id, ct);
		if (!postExists) return NotFound();

		if (string.IsNullOrWhiteSpace(dto.Content))
		{
			return BadRequest("Content is required.");
		}

		var comment = new Comment
		{
			PostId = id,
			UserId = userId.Value,
			Content = dto.Content
		};

		await _db.Comments.AddAsync(comment, ct);
		await _db.SaveChangesAsync(ct);

		var user = await _db.ApplicationUsers.Where(u => u.Id == userId.Value).Select(u => new { u.Username }).FirstOrDefaultAsync(ct);

		var result = new CommentDto
		{
			Id = comment.Id,
			UserId = comment.UserId,
			Username = user?.Username ?? string.Empty,
			Content = comment.Content,
			CreatedAt = comment.CreatedAt
		};

		return Ok(result);
	}
}


