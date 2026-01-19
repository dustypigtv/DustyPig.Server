using DustyPig.API.v3.Models;

namespace DustyPig.Server.Utilities;

public static class CommonResponses
{
    public static Result RequiredValueMissing(string name) => Result.BuildError($"Validation failed: {name} must be specified");

    public static Result ValueNotFound(string name) => Result.BuildError($"{name} not found");

    public static Result InvalidValue(string name) => Result.BuildError($"Validation failed: Invalid {name}");

    public static Result RequireMainProfile() => Result.BuildError("You must be logged in with the main profile to perform this action");

    public static Result ProhibitTestUser() => Result.BuildError("Test account is not authorized to to perform this action");

    public static Result ProfileIsLocked() => Result.BuildError("Your profile is locked");

    public static Result Forbid() => Result.BuildError("You are forbidden from performing this action");
}
