using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoLens.Api.Models;

[Table("StepRecords")]
public class StepRecord : BaseEntity
{
  [Required]
  public int UserId { get; set; }

  [Required]
  public int StepCount { get; set; }

  [Required]
  public DateTime RecordDate { get; set; }

  [Required]
  [Column(TypeName = "decimal(18,4)")]
  public decimal CarbonOffset { get; set; }

  public ApplicationUser? User { get; set; }
}

