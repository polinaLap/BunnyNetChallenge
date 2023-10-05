using System.ComponentModel.DataAnnotations;

public class StopContainerRequest
{
    [Required]
    public string ContainerName { get; set; }
}
