using FamilyTreeApi.Models;

namespace FamilyTreeApi.Services;

public interface IGedcomParserService
{
    FamilyTreeData Parse(Stream stream);
}
