using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoLens.Api.Models;

[Table("UserFollows")]
public class UserFollow : BaseEntity
{
	[Required]
	public int FollowerId { get; set; }

	[Required]
	public int FolloweeId { get; set; }

	public ApplicationUser? Follower { get; set; }
	public ApplicationUser? Followee { get; set; }
}



