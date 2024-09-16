# Satori

Satori is web application that provides comprehension and understanding to the existing systems you already are using.  These include issue tracking, project management, time tracking, and other systems.  Unified dashboards pull information from multiple systems and merging them into a consistent view.  

This is not yet another To Do List, you already have enough of those.  Satori does not store any data itself.  It only makes sense of the distributed data you already have.  It streamlines navigating between those systems.  Quick actions ensure that data is kept consistent between all systems.

Currently Supported Systems 
- Kimai (Time Tracking)
  - Although Kimai does provide a great interface for timing the task you are working on, Satori provides a Daily Stand-Up page to group the time records.
  - The Stand-Up page allows for easily updating the Kimai comments to record 2 of the "Scrum 3 questions", 1) What did I accomplish today? 2) What were my impediments?
  - Kimai time entry records can also be annotated with the Azure DevOps work item references.  This provides quick navigation between systems.
  - Kimai Time entry can be exported to Azure DevOps to keep the Completed Work and Remaining Work totals up to date.  The daily stand-up page is a great place to review the daily work and mark tasks as completed.
- Azure DevOps
  - The Satori Sprint Board page provides a unified view of tasks across multiple boards.  
  - Priorities relative to other boards can be adjusted (coming soon)
  - Issue dependency and cumulative estimates (coming soon)
  - Status of all Pull Requests can be seen
- Azure Service Bus
  - Kimai Time entries can also be exported to an Azure Service Bus Queue.  Custom programs can be written to import these daily totals.  Duplicate comments created returning (restarting) the same task multiple times in a day are removed. 

# Getting Started

## As an end user
This web application is already hosted at [https://satori.nexus](https://satori.nexus).  Enter your API keys to connect to any or all of the supported integrated systems.

## Contributing
This is a Blazor Web Assembly application.

# Future Roadmap

Overtime the number of integrated systems is expected to grow.  These may include:

- ✔️ Bug & backlog tracking: Azure DevOps
- ✔️ Source Control: Azure DevOps (git)
- ✔️ Time tracking: Kimai
- Support Ticket & Sales tracking: Microsoft Dynamics CRM, FreshDesk
- Email: Outlook
- Chat: Slack, Microsoft Teams
- Wiki: Confluence
- Project Management: BaseCamp
