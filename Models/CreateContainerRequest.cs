using System.ComponentModel.DataAnnotations;

public class CreateContainerRequest
{
    [Required]
    public string ImageName { get; set; }

    public string Tag { get; set; }
}
