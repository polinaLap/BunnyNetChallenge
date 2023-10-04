using System.ComponentModel.DataAnnotations;

public class CreateContainerRequest
{
    [Required]
    public string ImageName { get; set; }

    [Required]
    public string ContainerName { get; set; }

    public string ImageTag { get; set; }
}
