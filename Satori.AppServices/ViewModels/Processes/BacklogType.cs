namespace Satori.AppServices.ViewModels.Processes;

public enum BacklogType
{
    /// <summary>
    /// Backlog for Tasks
    /// </summary>
    Task,
    /// <summary>
    /// Backlog for the board items.  E.g. Product Backlog Items, User Stories, Issues
    /// </summary>
    Requirement,
    /// <summary>
    /// Backlog for Epics and Features
    /// </summary>
    Portfolio,
}