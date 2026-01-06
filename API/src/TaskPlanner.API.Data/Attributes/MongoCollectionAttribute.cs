namespace TaskPlanner.API.Data.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class MongoCollectionAttribute : Attribute
{
    public string Name { get; }
    public MongoCollectionAttribute(string name) => Name = name;
}