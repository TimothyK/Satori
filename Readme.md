# ColabTime

It's Collaboration Time!  This is web applications that:
- pulls data from multiple systems
  - Bug tracking: Azure DevOps
  - Source Control: Azure DevOps (git)
  - Call tracking: CMS, Sentry
  - Time tracking: Kimai, TimeTrack
  - Email: Outlook
  - Project Management: BaseCamp
  - Wikis: Confluence
- quick access to the same issue across those multiple systems
- quick mechanisms to stay focused on high priority items
- quick actions to repair inconsistencies between systems

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

