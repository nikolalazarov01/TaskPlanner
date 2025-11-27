using MongoDB.Bson;

namespace TaskPlanner.API.Data.Interfaces;

public interface IEntity
{
    ObjectId Id { get; set; }
}

