# Solution overview (based ONLY on uploaded chunks)

## Counts
- Canvas apps (groups): 2 — as detected in the CanvasApps extract .  
- Workflows: 10 — workflow files detected in the Workflows folder .  
- Environment variables: 16 — found in environmentvariabledefinitions .  
- Screens: 0 — no screens present for the canvas apps (screens = 0)  .  
- Relationship edges (total): 44 — overall relationships count from the overview .  
  - By type (from uploaded counts): workflow_to_connector = 18; workflow_to_env = 26 (44 total − 18 connector edges = 26 env edges) .  
  - Relationship types observed in the relationships file include workflow_to_connector and workflow_to_env .

## Canvas apps
- Apps detected:
  - wmreply_replybraryv2_c933c 
  - wmreply_replybrary_b320d 
- Screens per app: none (each app's screens array is empty) .

## Connectors used (unique)
- shared_sharepointonline  
- shared_teams  
- shared_office365groups  
- shared_office365users  
These connectors are referenced across the workflow details and relationships  .

## Workflows (list)
- EndFormFlow-82921763-2342-F011-8779-000D3A0CEB69.json   
- EndFormFlowReminder-128F82AA-B942-F011-877A-000D3A0CEB69.json   
- IdentifierReminderFlow-D4AE74B0-DA42-F011-877A-000D3A0CEB69.json   
- PeopleListFlow-3CE0853C-4C36-F011-8C4C-6045BD0ACADB.json   
- PMflow-A3194D5E-D434-F011-8C4C-6045BD0ACADB.json   
- PMFormReminderFlow-B025C04D-D342-F011-877A-000D3A0CEB69.json   
- ReplybraryDailyPipedrivecheck-5898F434-A147-F011-8779-000D3A0CEB69.json   
- Replybrary_GetExchangeRate-4E437A2B-C74D-F011-877B-000D3A0CEB69.json   
- Replybrary_Identifier_Flow-38CA161E-BA34-F011-8C4C-6045BD0ACADB.json   
- UploadLogo-F9309D25-C742-F011-877A-000D3A0CEB69.json 

(Workflow-level connector and env-var usage details are available in the workflows_detailed extract) .

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
(List taken from envvars.json) .

## Screens
- No screens detected for the canvas apps (screens = 0) — nothing to list per app  .

If you want, I can produce a compact CSV export of these lists or a visual mapping of workflows → connectors → env-vars using the relationships file next.