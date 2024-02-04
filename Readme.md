# Satori

Satori is web application that provides comprehension and understanding to the existing systems you already are using.  These include issue tracking, project management, time tracking, and other systems.  Unified dashboards pull information from multiple systems and merging them into a consistent view.  

This is not yet another To Do List, you already have enough of those.  Satori does not store any data itself.  It only makes sense of the distributed data you already have.  It streamlines navigating between those systems.  Quick actions ensure that data is kept consistent between all systems.

Supported Systems 
- Bug & backlog tracking: Azure DevOps
- Source Control: Azure DevOps (git)
- Support Ticket & Sales tracking: Microsoft Dynamics CRM, Sentry
- Time tracking: Kimai, TimeTrack
- Email: Outlook
- Chat: Slack, Microsoft Teams
- Wiki: Confluence
- Project Management: BaseCamp
  
Features:
- Quick navigation to the same issue across those multiple systems
- Quick actions to repair inconsistencies between systems, and keep the issues in all systems progressing.
- Dashboards and reminders to stay focused on high priority items, and not bombard you with the other high priority items you can worry about tomorrow.  Realistic time management and Work In Progress (WIP) limits.
- Daily stand-up boards to answer the 3 questions:  yesterday's accomplishments & impediments, and today's plan.
- Accurate time tracking.  Although we don't need big brother tracking us every minute of every day, if we are going to track our time we may as well do it well.

# Getting Started

This (currently) uses a personal access token of the developer when running this web application in order to interact with Azure DevOps.  This token is stored in a user secrets file stored on the developer machine.  It is not checked into source control.  This will need to be configured before the program will successfully run.

Open the solution in Visual Studio
- right click the project in the Solution Explorer
- select the "Manage User Secrets" menu
- copy the following into this empty json file.  Note that this is also available from `appsettings.Development.json`.

```
{
  "AzureDevOps": {
    "PersonalAccessToken": "the token value from AzureDevOps"
  }
}
```

Create your token by
- click on your profile avatar picture in the top right of any Azure DevOps web portal page.
- Click Security menu
- This takes you to the Person Access Tokens page.  Click "+New Token".
- Create a the token
- Copy its value
- Paste it into your secrets.json file

For more on User Secrets, see [here](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows)

