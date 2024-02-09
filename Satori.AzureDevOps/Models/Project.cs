﻿namespace Satori.AzureDevOps.Models;

public class Project
{
    public Guid id { get; set; }
    public DateTime lastUpdateTime { get; set; }
    public string name { get; set; }
    public string state { get; set; }
    public string visibility { get; set; }
}