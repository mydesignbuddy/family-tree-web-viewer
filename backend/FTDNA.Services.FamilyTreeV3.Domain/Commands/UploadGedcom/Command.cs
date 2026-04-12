namespace FTDNA.Services.FamilyTreeV3.Domain.Commands.UploadGedcom;

public record UploadGedcomCommand(Stream FileStream, string FileName = "upload.ged");
