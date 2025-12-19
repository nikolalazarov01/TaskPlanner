using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace TaskPlanner.API.Web.Extensions;

public static class ControllerExtensions
{
    public static bool TryGetUserObjectId(this ControllerBase controller, out ObjectId userId)
    {
        if (controller is null) throw new ArgumentNullException(nameof(controller));

        // Common claim types: NameIdentifier (recommended), "sub" (JWT standard), or custom.
        var id =
            controller.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            controller.User.FindFirstValue("sub") ??
            controller.User.FindFirstValue("uid");

        return ObjectId.TryParse(id, out userId);
    }
}