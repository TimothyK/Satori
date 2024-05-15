﻿namespace Satori.AppServices.ViewModels;

public class Person
{
    public Guid Id { get; init; }
    public required string DisplayName { get; init; }
    public required Uri AvatarUrl { get; init; }
}