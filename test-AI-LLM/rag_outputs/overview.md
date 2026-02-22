# Solution overview

This solution contains two canvas apps, ten Power Automate workflows, and a set of environment variables used to configure connectors and lists. The workflows are primarily scheduled or event-driven flows that read SharePoint lists and post notifications to Teams; some are invoked manually from the canvas app (PowerApps) to perform file uploads or ad-hoc operations. (Counts and item details below are taken from the uploaded solution chunks.) 

## Counts
- Canvas apps (groups): 2   
- Workflows: 10   
- Environment variables: 16   
- Screens (all canvas apps): 0   
- Relationship edges (total): 44   
  - workflow_to_connector: 18 (workflow connector edges)   
  - workflow_to_env: 26 (workflow → environment variable edges; remaining relationships) 

## Canvas Apps
From the uploaded canvas app details, there are two apps; both have no discovered screens or connectors in the detailed export.  

- wmreply_replybraryv2_c933c  
  - Screens: 0   
  - Connectors: none detected in the detailed export 

- wmreply_replybrary_b320d  
  - Screens: 0   
  - Connectors: none detected in the detailed export 

(Additional canvas app package/document URIs are present in the grouping file export.) 

## Workflows
List of discovered workflows with trigger, purpose, and detected actions (names normalized per the naming rule: CleanName (FullName) where applicable). Details come from the workflows_detailed.json export. 

- EndFormFlow (EndFormFlow-82921763-2342-F011-8779-000D3A0CEB69)  
  - Trigger: Recurrence: Recurrence  
  - Purpose: Scheduled flow that runs automatically and performs automated steps using configured connectors and environment variables.  
  - Actions detected: Get items, Post message 

- EndFormFlowReminder (EndFormFlowReminder-128F82AA-B942-F011-877A-000D3A0CEB69)  
  - Trigger: Recurrence: Recurrence  
  - Purpose: Scheduled reminder flow that notifies users (channel depends on configured connectors).  
  - Actions detected: Get items, Post message 

- IdentifierReminderFlow (IdentifierReminderFlow-D4AE74B0-DA42-F011-877A-000D3A0CEB69)  
  - Trigger: Recurrence: Recurrence  
  - Purpose: Scheduled reminder flow that notifies users (channel depends on configured connectors).  
  - Actions detected: Get items, Post message 

- PeopleListFlow (PeopleListFlow-3CE0853C-4C36-F011-8C4C-6045BD0ACADB)  
  - Trigger: Recurrence: Recurrence  
  - Purpose: Flow that maintains / syncs people data using configured connectors.  
  - Actions detected: Get items 

- PMflow (PMflow-A3194D5E-D434-F011-8C4C-6045BD0ACADB)  
  - Trigger: manual: Request, PowerAppV2  
  - Purpose: Manual flow invoked from the app (PowerApps trigger); performs automated steps using configured connectors and environment variables.  
  - Actions detected: Post message 

- PMFormReminderFlow (PMFormReminderFlow-B025C04D-D342-F011-877A-000D3A0CEB69)  
  - Trigger: Recurrence: Recurrence  
  - Purpose: Scheduled reminder flow that notifies users (channel depends on configured connectors).  
  - Actions detected: Get items, Post message 

- ReplybraryDailyPipedrivecheck (ReplybraryDailyPipedrivecheck-5898F434-A147-F011-8779-000D3A0CEB69)  
  - Trigger: Recurrence: Recurrence  
  - Purpose: Scheduled daily check flow related to Pipedrive (business logic depends on flow actions).  
  - Actions detected: Get items 

- Replybrary_GetExchangeRate (Replybrary_GetExchangeRate-4E437A2B-C74D-F011-877B-000D3A0CEB69)  
  - Trigger: Recurrence: Recurrence  
  - Purpose: Scheduled flow that refreshes or updates exchange-rate data using configured connectors.  
  - Actions detected: Get items 

- Replybrary_Identifier_Flow (Replybrary_Identifier_Flow-38CA161E-BA34-F011-8C4C-6045BD0ACADB)  
  - Trigger: When_an_item_is_created: OpenApiConnection  
  - Purpose: Event-driven flow triggered when new records are created; performs follow-up updates / notifications.  
  - Actions detected: Post message 

- UploadLogo (UploadLogo-F9309D25-C742-F011-877A-000D3A0CEB69)  
  - Trigger: manual: Request, PowerAppV2  
  - Purpose: Triggered from the app to upload / store a file using configured connectors.  
  - Actions detected: Get items 

## Environment variables
The environment variable definitions export lists 16 keys discovered in the solution; no friendly display names were supplied in the provided mapping, so keys are shown as-is (items taken from envvars.json). 

- wmreply_Replybrary_Admin_List  
- wmreply_Replybrary_App_Link  
- wmreply_Replybrary_CertificationsTracker_List  
- wmreply_Replybrary_ClientContacts_List  
- wmreply_Replybrary_Client_List  
- wmreply_Replybrary_Currency_List  
- wmreply_Replybrary_LessonsLearnt_List  
- wmreply_Replybrary_Links_List  
- wmreply_Replybrary_M365GroupID  
- wmreply_Replybrary_People_List  
- wmreply_Replybrary_ProjectHistory_List  
- wmreply_Replybrary_Project_List  
- wmreply_Replybrary_ReusableIdeas_List  
- wmreply_Replybrary_SkillLevels_List  
- wmreply_Replybrary_SMEs_List  
- wmreply_Replybrary_SP_Site

(These are directory/key names as exported.) 

---

If you want, I can:
- Expand each workflow entry to include connectors and the specific env var keys used per flow (from workflows_detailed.json).   
- Produce a simple dependency graph (which workflows use which connectors / env vars) based on relationships.json. 