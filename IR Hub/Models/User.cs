using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace IR_Hub.Models;

public class User : IdentityUser
{
    public string? FirstName { get; set; } = "Utilizator";

    public string? LastName { get; set; } = "Necunoscut";
    public string? About { get; set; }
    public string? Profile_image { get; set; }
    public string? Role { get; set; }
}