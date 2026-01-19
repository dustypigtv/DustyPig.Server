using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models;

/// <summary>
/// Use FTs to actually search, but keep these for admin of media
/// </summary>
[Index(nameof(MediaEntryId), nameof(Term), IsUnique = true)]
public class ExtraSearchTerm
{
    public int Id { get; set; }

    public int MediaEntryId { get; set; }
    public MediaEntry MediaEntry { get; set; }

    [MaxLength(Constants.MAX_NAME_LENGTH)]
    public string Term { get; set; }
}
