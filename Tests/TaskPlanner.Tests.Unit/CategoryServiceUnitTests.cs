using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Models.Category;
using TaskPlanner.API.Core.Services;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Utilities;
using TaskPlanner.API.Utilities.Constants;

namespace TaskPlanner.Tests.Unit;

public class CategoryServiceTests
{
    private static (CategoryService Service, Mock<IBaseRepository<Category>> Repo) CreateSut()
    {
        var repo = new Mock<IBaseRepository<Category>>(MockBehavior.Strict);
        var sut = new CategoryService(repo.Object);
        return (sut, repo);
    }

    private static BsonDocument RenderFilter(FilterDefinition<Category> filter)
    {
        var serializer = BsonSerializer.SerializerRegistry.GetSerializer<Category>();
        var args = new RenderArgs<Category>(serializer, BsonSerializer.SerializerRegistry);
        return filter.Render(args);
    }

    private static BsonDocument RenderSort(SortDefinition<Category> sort)
    {
        var serializer = BsonSerializer.SerializerRegistry.GetSerializer<Category>();
        var args = new RenderArgs<Category>(serializer, BsonSerializer.SerializerRegistry);
        return sort.Render(args);
    }

    private static BsonDocument RenderUpdate(UpdateDefinition<Category> update)
    {
        var args = new RenderArgs<Category>
        {
            DocumentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<Category>(),
            SerializerRegistry = BsonSerializer.SerializerRegistry
        };

        var rendered = update.Render(args); // BsonValue
        return rendered.AsBsonDocument;
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateCategory_ShouldUse_DefaultValues_When_Not_Provided()
    {
        var (sut, repo) = CreateSut();

        Category? captured = null;
        repo.Setup(x => x.CreateAsync(It.IsAny<Category>()))
            .Callback<Category>(c => captured = c)
            .ReturnsAsync(new OperationResult<Category>());

        var userId = ObjectId.GenerateNewId();
        var input = new CategoryInputModel { Name = "Work" };

        await sut.CreateCategory(input, userId, CancellationToken.None);

        repo.Verify(x => x.CreateAsync(It.IsAny<Category>()), Times.Once);

        Assert.NotNull(captured);
        Assert.Equal("Work", captured!.Name);
        Assert.Equal(userId, captured.UserId);
        Assert.Equal(ApiConstants.CategoryConstants.DefaultColor, captured.Color);
        Assert.Equal(ApiConstants.CategoryConstants.DefaultSortOrder, captured.SortOrder);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateCategory_ShouldUse_ProvidedValues_When_Present()
    {
        var (sut, repo) = CreateSut();

        Category? captured = null;
        repo.Setup(x => x.CreateAsync(It.IsAny<Category>()))
            .Callback<Category>(c => captured = c)
            .ReturnsAsync(new OperationResult<Category>());

        var userId = ObjectId.GenerateNewId();
        var input = new CategoryInputModel { Name = "Work", Color = "#ABCDEF", SortOrder = 7 };

        await sut.CreateCategory(input, userId, CancellationToken.None);

        repo.Verify(x => x.CreateAsync(It.IsAny<Category>()), Times.Once);

        Assert.NotNull(captured);
        Assert.Equal("Work", captured!.Name);
        Assert.Equal("#ABCDEF", captured.Color);
        Assert.Equal(7, captured.SortOrder);
        Assert.Equal(userId, captured.UserId);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateCategory_ShouldReturnError_When_Id_IsInvalid_AndNotCallRepository()
    {
        var (sut, repo) = CreateSut();

        var input = new UpdateCategoryInputModel
        {
            Id = "not-an-objectid",
            Name = "New Name"
        };

        var result = await sut.UpdateCategory(input, ObjectId.GenerateNewId(), CancellationToken.None);

        repo.Verify(x => x.ModifyAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>(), It.IsAny<UpdateDefinition<Category>>()), Times.Never);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Invalid category id."));
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateCategory_ShouldReturnError_When_NoFieldsProvided_AndNotCallRepository()
    {
        var (sut, repo) = CreateSut();

        var input = new UpdateCategoryInputModel
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = null,
            Color = null,
            SortOrder = null
        };

        var result = await sut.UpdateCategory(input, ObjectId.GenerateNewId(), CancellationToken.None);

        repo.Verify(x => x.ModifyAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>(), It.IsAny<UpdateDefinition<Category>>()), Times.Never);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("No fields provided for update."));
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateCategory_ShouldCallRepository_WithCorrectId_AndUpdateDefinition_When_UpdatingNameOnly()
    {
        var (sut, repo) = CreateSut();

        Category? capturedEntity = null;
        UpdateDefinition<Category>? capturedUpdate = null;
        CancellationToken capturedToken = default;

        repo.Setup(x => x.ModifyAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>(), It.IsAny<UpdateDefinition<Category>>()))
            .Callback<Category, CancellationToken, UpdateDefinition<Category>>((e, ct, u) =>
            {
                capturedEntity = e;
                capturedToken = ct;
                capturedUpdate = u;
            })
            .ReturnsAsync(new OperationResult<Category>());

        var id = ObjectId.GenerateNewId();
        var cts = new CancellationTokenSource();

        var input = new UpdateCategoryInputModel
        {
            Id = id.ToString(),
            Name = "Renamed"
        };

        await sut.UpdateCategory(input, ObjectId.GenerateNewId(), cts.Token);

        repo.Verify(x => x.ModifyAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>(), It.IsAny<UpdateDefinition<Category>>()), Times.Once);

        Assert.NotNull(capturedEntity);
        Assert.Equal(id, capturedEntity!.Id);
        Assert.Equal(cts.Token, capturedToken);

        Assert.NotNull(capturedUpdate);
        var rendered = RenderUpdate(capturedUpdate!);

        Assert.True(rendered.Contains("$set"));
        var setDoc = rendered["$set"].AsBsonDocument;
        Assert.Equal("Renamed", setDoc["Name"].AsString);
        Assert.False(setDoc.Contains("Color"));
        Assert.False(setDoc.Contains("SortOrder"));
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateCategory_ShouldCallRepository_WithCombinedUpdate_When_UpdatingMultipleFields()
    {
        var (sut, repo) = CreateSut();

        UpdateDefinition<Category>? capturedUpdate = null;

        repo.Setup(x => x.ModifyAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>(), It.IsAny<UpdateDefinition<Category>>()))
            .Callback<Category, CancellationToken, UpdateDefinition<Category>>((_, __, u) => capturedUpdate = u)
            .ReturnsAsync(new OperationResult<Category>());

        var id = ObjectId.GenerateNewId();

        var input = new UpdateCategoryInputModel
        {
            Id = id.ToString(),
            Name = "Work",
            Color = "#FFFFFF",
            SortOrder = 5
        };

        await sut.UpdateCategory(input, ObjectId.GenerateNewId(), CancellationToken.None);

        repo.Verify(x => x.ModifyAsync(It.Is<Category>(c => c.Id == id), It.IsAny<CancellationToken>(), It.IsAny<UpdateDefinition<Category>>()), Times.Once);

        Assert.NotNull(capturedUpdate);
        var rendered = RenderUpdate(capturedUpdate!);

        Assert.True(rendered.Contains("$set"));
        var setDoc = rendered["$set"].AsBsonDocument;

        Assert.Equal("Work", setDoc["Name"].AsString);
        Assert.Equal("#FFFFFF", setDoc["Color"].AsString);
        Assert.Equal(5, setDoc["SortOrder"].AsInt32);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetCategoryById_ShouldCallRepository_WithFilter_OnIdAndUserId()
    {
        var (sut, repo) = CreateSut();

        FilterDefinition<Category>? capturedFilter = null;

        repo.Setup(x => x.GetOneAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<Category>, CancellationToken>((f, _) => capturedFilter = f)
            .ReturnsAsync(new OperationResult<Category>());

        var categoryId = ObjectId.GenerateNewId();
        var userId = ObjectId.GenerateNewId();

        await sut.GetCategoryById(categoryId.ToString(), userId, CancellationToken.None);

        repo.Verify(x => x.GetOneAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(capturedFilter);
        var rendered = RenderFilter(capturedFilter!);

        // MongoDB may optimize the AND into a single document.
        // Also field name might be "_id" (most common) or "Id" depending on serialization.
        static string GetIdField(BsonDocument doc) => doc.Contains("_id") ? "_id" : "Id";

        if (rendered.Contains("$and"))
        {
            var andArray = rendered["$and"].AsBsonArray;

            Assert.Contains(andArray, x =>
            {
                var d = x.AsBsonDocument;
                var idField = GetIdField(d);
                return d.Contains(idField) && d[idField].IsObjectId && d[idField].AsObjectId == categoryId;
            });

            Assert.Contains(andArray, x =>
            {
                var d = x.AsBsonDocument;
                return d.Contains("UserId") && d["UserId"].IsObjectId && d["UserId"].AsObjectId == userId;
            });
        }
        else
        {
            var idField = GetIdField(rendered);

            Assert.True(rendered.Contains(idField));
            Assert.True(rendered[idField].IsObjectId);
            Assert.Equal(categoryId, rendered[idField].AsObjectId);

            Assert.True(rendered.Contains("UserId"));
            Assert.True(rendered["UserId"].IsObjectId);
            Assert.Equal(userId, rendered["UserId"].AsObjectId);
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task GetCategories_ShouldCallRepository_WithFilter_OnUserId_AndExpectedSort()
    {
        var (sut, repo) = CreateSut();

        FilterDefinition<Category>? capturedFilter = null;
        SortDefinition<Category>? capturedSort = null;
        CancellationToken capturedToken = default;

        repo.Setup(x => x.GetAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>(), It.IsAny<SortDefinition<Category>?>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .Callback<FilterDefinition<Category>, CancellationToken, SortDefinition<Category>?, int?, int?>((f, ct, s, _, __) =>
            {
                capturedFilter = f;
                capturedSort = s;
                capturedToken = ct;
            })
            .ReturnsAsync(new OperationResult<IReadOnlyList<Category>>().WithRelatedObject(Array.Empty<Category>()));

        var userId = ObjectId.GenerateNewId();
        var cts = new CancellationTokenSource();

        await sut.GetCategories(userId, cts.Token);

        repo.Verify(x => x.GetAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>(), It.IsAny<SortDefinition<Category>?>(), null, null), Times.Once);

        Assert.Equal(cts.Token, capturedToken);

        Assert.NotNull(capturedFilter);
        var renderedFilter = RenderFilter(capturedFilter!);
        Assert.True(renderedFilter.Contains("UserId"));
        Assert.Equal(userId, renderedFilter["UserId"]);

        Assert.NotNull(capturedSort);
        var renderedSort = RenderSort(capturedSort!);

        Assert.True(renderedSort.Contains("SortOrder"));
        Assert.True(renderedSort.Contains("CreatedAt"));
        Assert.Equal(1, renderedSort["SortOrder"].AsInt32);
        Assert.Equal(1, renderedSort["CreatedAt"].AsInt32);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteCategory_ShouldReturnError_When_Id_IsInvalid_AndNotCallRepository()
    {
        var (sut, repo) = CreateSut();

        var result = await sut.DeleteCategory("invalid-id", ObjectId.GenerateNewId(), CancellationToken.None);

        repo.Verify(x => x.DeleteOneAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()), Times.Never);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Invalid category id."));
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteCategory_ShouldCallRepository_WithFilter_OnIdAndUserId_When_Id_IsValid()
    {
        var (sut, repo) = CreateSut();

        FilterDefinition<Category>? capturedFilter = null;
        CancellationToken capturedToken = default;

        repo.Setup(x => x.DeleteOneAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<Category>, CancellationToken>((f, ct) =>
            {
                capturedFilter = f;
                capturedToken = ct;
            })
            .ReturnsAsync(new OperationResult<Category>());

        var categoryId = ObjectId.GenerateNewId();
        var userId = ObjectId.GenerateNewId();
        var cts = new CancellationTokenSource();

        await sut.DeleteCategory(categoryId.ToString(), userId, cts.Token);

        repo.Verify(x => x.DeleteOneAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(cts.Token, capturedToken);

        Assert.NotNull(capturedFilter);
        var rendered = RenderFilter(capturedFilter!);

        // Accept both shapes: optimized document OR $and array
        if (rendered.Contains("$and"))
        {
            var andArray = rendered["$and"].AsBsonArray;

            Assert.Contains(andArray, x =>
                x.AsBsonDocument.Contains("_id") &&
                x.AsBsonDocument["_id"].IsObjectId &&
                x.AsBsonDocument["_id"].AsObjectId == categoryId);

            Assert.Contains(andArray, x =>
                x.AsBsonDocument.Contains("UserId") &&
                x.AsBsonDocument["UserId"].IsObjectId &&
                x.AsBsonDocument["UserId"].AsObjectId == userId);
        }
        else
        {
            Assert.True(rendered.Contains("_id"));
            Assert.True(rendered["_id"].IsObjectId);
            Assert.Equal(categoryId, rendered["_id"].AsObjectId);

            Assert.True(rendered.Contains("UserId"));
            Assert.True(rendered["UserId"].IsObjectId);
            Assert.Equal(userId, rendered["UserId"].AsObjectId);
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteCategories_ShouldCallRepository_WithFilter_OnUserId()
    {
        var (sut, repo) = CreateSut();

        FilterDefinition<Category>? capturedFilter = null;

        repo.Setup(x => x.DeleteManyAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<Category>, CancellationToken>((f, _) => capturedFilter = f)
            .ReturnsAsync(new OperationResult<long>().WithRelatedObject(0L));

        var userId = ObjectId.GenerateNewId();

        await sut.DeleteCategories(userId, CancellationToken.None);

        repo.Verify(x => x.DeleteManyAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(capturedFilter);
        var rendered = RenderFilter(capturedFilter!);

        Assert.True(rendered.Contains("UserId"));
        Assert.Equal(userId, rendered["UserId"]);
    }

    [Fact]
    public async System.Threading.Tasks.Task Service_ShouldReturn_RepositoryResult_For_GetAndDeleteOperations()
    {
        var (sut, repo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var categoryId = ObjectId.GenerateNewId();

        var expectedCategory = new Category { Id = categoryId, UserId = userId, Name = "Work" };

        var getOneResult = new OperationResult<Category>().WithRelatedObject(expectedCategory);
        var deleteOneResult = new OperationResult<Category>().WithRelatedObject(expectedCategory);

        repo.Setup(x => x.GetOneAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getOneResult);

        repo.Setup(x => x.DeleteOneAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteOneResult);

        var get = await sut.GetCategoryById(categoryId.ToString(), userId, CancellationToken.None);
        var del = await sut.DeleteCategory(categoryId.ToString(), userId, CancellationToken.None);

        Assert.Same(expectedCategory, get.ResultObject);
        Assert.Same(expectedCategory, del.ResultObject);

        repo.Verify(x => x.GetOneAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.DeleteOneAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async System.Threading.Tasks.Task UpdateCategory_ShouldReturnSuccess_When_RepositorySucceeds()
    {
        var (sut, repo) = CreateSut();
        var id = ObjectId.GenerateNewId();
        var userId = ObjectId.GenerateNewId();
        
        var updatedCategory = new Category 
        { 
            Id = id, 
            UserId = userId, 
            Name = "Updated" 
        };
        
        repo.Setup(x => x.ModifyAsync(
                It.Is<Category>(c => c.Id == id), 
                It.IsAny<CancellationToken>(), 
                It.IsAny<UpdateDefinition<Category>>()))
            .ReturnsAsync(new OperationResult<Category>().WithRelatedObject(updatedCategory));
        
        var input = new UpdateCategoryInputModel
        {
            Id = id.ToString(),
            Name = "Updated"
        };
        
        var result = await sut.UpdateCategory(input, userId, CancellationToken.None);
        
        Assert.True(result.Success);
        Assert.Equal(updatedCategory, result.ResultObject);
    }
    
    [Fact]
    public async System.Threading.Tasks.Task UpdateCategory_ShouldReturnError_When_RepositoryFails()
    {
        var (sut, repo) = CreateSut();
        var id = ObjectId.GenerateNewId();
        
        var failedResult = new OperationResult<Category>();
        failedResult.AppendError("Database error");
        
        repo.Setup(x => x.ModifyAsync(
                It.IsAny<Category>(), 
                It.IsAny<CancellationToken>(), 
                It.IsAny<UpdateDefinition<Category>>()))
            .ReturnsAsync(failedResult);
        
        var input = new UpdateCategoryInputModel
        {
            Id = id.ToString(),
            Name = "Updated"
        };
        
        var result = await sut.UpdateCategory(input, ObjectId.GenerateNewId(), CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Database error"));
    }
    
    [Fact]
    public async System.Threading.Tasks.Task GetCategoryById_ShouldReturnNotFound_When_CategoryDoesNotExist()
    {
        var (sut, repo) = CreateSut();
        
        var notFoundResult = new OperationResult<Category>();
        notFoundResult.AppendError(new NotFoundError("Entity not found."));
        
        repo.Setup(x => x.GetOneAsync(
                It.IsAny<FilterDefinition<Category>>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(notFoundResult);
        
        var result = await sut.GetCategoryById(
            ObjectId.GenerateNewId().ToString(), 
            ObjectId.GenerateNewId(), 
            CancellationToken.None);
        
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e is NotFoundError);
    }
}
