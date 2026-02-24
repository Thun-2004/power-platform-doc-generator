# Solution overview

## Counts
- Canvas apps: 2 — from overview.json  and detailed apps list   
- Workflows (flows): 10 — from overview.json and workflows listing    
- Environment variables: 16 — from envvars.json   
- Screens: 0 (no screens present in the apps) — overview and canvas details show empty screens arrays    
- Relationship edges (total): 44 — overview.json   
  - workflow_to_connector: 18 — workflow connector count in overview.json   
  - workflow_to_env (env relationships): 26 (derived: 44 total − 18 connector edges) 

## Connectors used (unique)
- shared_sharepointonline  
- shared_teams  
- shared_office365groups  
- shared_office365users

(Connectors aggregated from workflows detailed scan) 

## Workflows (list)
- EndFormFlow-82921763-2342-F011-8779-000D3A0CEB69  
- EndFormFlowReminder-128F82AA-B942-F011-877A-000D3A0CEB69  
- IdentifierReminderFlow-D4AE74B0-DA42-F011-877A-000D3A0CEB69  
- PeopleListFlow-3CE0853C-4C36-F011-8C4C-6045BD0ACADB  
- PMflow-A3194D5E-D434-F011-8C4C-6045BD0ACADB  
- PMFormReminderFlow-B025C04D-D342-F011-877A-000D3A0CEB69  
- ReplybraryDailyPipedrivecheck-5898F434-A147-F011-8779-000D3A0CEB69  
- Replybrary_GetExchangeRate-4E437A2B-C74D-F011-877B-000D3A0CEB69  
- Replybrary_Identifier_Flow-38CA161E-BA34-F011-8C4C-6045BD0ACADB  
- UploadLogo-F9309D25-C742-F011-877A-000D3A0CEB69

(Source: workflows.json / workflows_detailed.json)  

## Environment variable names
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

(List taken from envvars.json) 

## Screens per app
- No screens present (both canvas apps show empty screens arrays) 

If you want, I can export this as a plaintext/Markdown file or expand each workflow entry to show its connectors, env vars used, trigger and purpose (those details are available in workflows_detailed.json) .