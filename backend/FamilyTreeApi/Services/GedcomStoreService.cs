using FamilyTreeApi.Models;

namespace FamilyTreeApi.Services;

public class GedcomStoreService : IGedcomStoreService
{
    private FamilyTreeData? _data;

    public void Store(FamilyTreeData data) => _data = data;
    public FamilyTreeData? Get() => _data;
    public void Clear() => _data = null;
}
