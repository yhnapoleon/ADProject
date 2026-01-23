using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EcoLens.Api.Models.Enums;

namespace EcoLens.Api.Models;

[Table("Posts")]
public class Post : BaseEntity
{
	[Required]
	public int UserId { get; set; }

	[Required]
	[MaxLength(200)]
	public string Title { get; set; } = string.Empty;

	[Required]
	[Column(TypeName = "nvarchar(max)")]
	public string Content { get; set; } = string.Empty;

	// 可存储 JSON 字符串或以逗号分隔的 URL 列表
	[Column(TypeName = "nvarchar(max)")]
	public string? ImageUrls { get; set; }

	[Required]
	public PostType Type { get; set; } = PostType.User;

	public int ViewCount { get; set; }

	// PB-008: 软删除标记
	public bool IsDeleted { get; set; } = false;

	public ApplicationUser? User { get; set; }
	public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}


