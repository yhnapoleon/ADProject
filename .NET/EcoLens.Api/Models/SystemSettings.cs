using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoLens.Api.Models;

[Table("SystemSettings")]
public class SystemSettings
{
	[Key]
	public int Id { get; set; } = 1;

	public int ConfidenceThreshold { get; set; } = 80;
	[MaxLength(100)]
	public string VisionModel { get; set; } = "default";
	public bool WeeklyDigest { get; set; } = true;
	public bool MaintenanceMode { get; set; } = false;
}

