using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoLens.Api.Models;

[Table("Comments")]
public class Comment : BaseEntity
{
	[Required]
	public int PostId { get; set; }

	[Required]
	public int UserId { get; set; }

	[Required]
	[MaxLength(2000)]
	public string Content { get; set; } = string.Empty;

	public Post? Post { get; set; }
	public ApplicationUser? User { get; set; }
}




