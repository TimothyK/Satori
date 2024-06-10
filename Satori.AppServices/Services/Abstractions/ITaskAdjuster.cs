﻿using Satori.AppServices.ViewModels.ExportPayloads;

namespace Satori.AppServices.Services.Abstractions;

public interface ITaskAdjuster
{
    Task SendAsync(TaskAdjustment payload);
}