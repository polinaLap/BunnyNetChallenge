using System.ComponentModel.DataAnnotations;

public class StopContainerRequest
{
    [Required]
    public string ContainerID { get; set; }
}
