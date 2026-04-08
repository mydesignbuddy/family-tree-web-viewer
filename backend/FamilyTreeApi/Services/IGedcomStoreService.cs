using FamilyTreeApi.Models;

namespace FamilyTreeApi.Services;

public interface IGedcomStoreService
{
    void Store(FamilyTreeData data);
    FamilyTreeData? Get();
    void Clear();
}
