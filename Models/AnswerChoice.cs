using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMSPlatform.Models;

public class AnswerChoice
{
    [Key]
    public int ChoiceID { get; set; }

    [Required]
    [ForeignKey("Question")]
    public int QuestionID { get; set; }

    [Required]
    [StringLength(255)]
    public string ChoiceText { get; set; } = string.Empty;

    [Required]
    public bool IsCorrect { get; set; } = false;

    public int ChoiceOrder { get; set; } = 1;

    // Navigation Properties
    public virtual Question Question { get; set; } = null!;
}