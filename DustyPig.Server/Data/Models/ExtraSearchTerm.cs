using DustyPig.API.v3.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DustyPig.Server.Data.Models;

/*
CREATE TABLE `dustypig_v3_dev`.`ExtraSearchTerms` (
  `Id` INT NOT NULL,
  `MediaEntryId` INT NOT NULL,
  `Term` VARCHAR(200) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE INDEX `IX_ExtraSearchTerms_MediaEntryId_Term` (`MediaEntryId` ASC, `Term` ASC) VISIBLE,
  CONSTRAINT `FK_ExtraSearchTerms_MediaEntries_MediaEntryId`
    FOREIGN KEY (`MediaEntryId`)
    REFERENCES `dustypig_v3_dev`.`MediaEntries` (`Id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION);
*/
[Index(nameof(MediaEntryId), nameof(Term), IsUnique = true)]
public class ExtraSearchTerm
{
    public int Id { get; set; }

    public int MediaEntryId { get; set; }
    public MediaEntry MediaEntry { get; set; }

    [MaxLength(Constants.MAX_NAME_LENGTH)]
    public string Term { get; set; }
}
