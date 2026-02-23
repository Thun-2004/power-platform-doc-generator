- Source: workflows_detailed.json  and relationships.json 

# EndFormFlow-82921763-2342-F011-8779-000D3A0CEB69
- Trigger: Recurrence: Recurrence 
- Purpose: Scheduled flow (recurrence trigger) that runs automatically and performs automated steps using configured connectors and environment variables. 
- Actions detected: Get items; Post message 
- Connectors used: shared_sharepointonline; shared_teams 
- Environment variables referenced: wmreply_Replybrary_App_Link; wmreply_Replybrary_Project_List; wmreply_Replybrary_SP_Site 
- Invoked from screens: Not found in uploaded files. (No screen_to_workflow entries present in relationships.json) 

# EndFormFlowReminder-128F82AA-B942-F011-877A-000D3A0CEB69
- Trigger: Recurrence: Recurrence 
- Purpose: Scheduled reminder flow that notifies users (exact channel depends on configured connectors). 
- Actions detected: Get items; Post message 
- Connectors used: shared_sharepointonline; shared_teams 
- Environment variables referenced: wmreply_Replybrary_App_Link; wmreply_Replybrary_Project_List; wmreply_Replybrary_SP_Site 
- Invoked from screens: Not found in uploaded files. (No screen_to_workflow entries present in relationships.json) 

# IdentifierReminderFlow-D4AE74B0-DA42-F011-877A-000D3A0CEB69
- Trigger: Recurrence: Recurrence 
- Purpose: Scheduled reminder flow that notifies users (exact channel depends on configured connectors). 
- Actions detected: Get items; Post message 
- Connectors used: shared_sharepointonline; shared_teams 
- Environment variables referenced: wmreply_Replybrary_App_Link; wmreply_Replybrary_Project_List; wmreply_Replybrary_SP_Site 
- Invoked from screens: Not found in uploaded files. (No screen_to_workflow entries present in relationships.json) 

# PeopleListFlow-3CE0853C-4C36-F011-8C4C-6045BD0ACADB
- Trigger: Recurrence: Recurrence 
- Purpose: Flow that maintains/syncs people data using configured connectors. 
- Actions detected: Get items 
- Connectors used: shared_office365groups; shared_office365users; shared_sharepointonline 
- Environment variables referenced: wmreply_Replybrary_M365GroupID; wmreply_Replybrary_People_List; wmreply_Replybrary_SP_Site 
- Invoked from screens: Not found in uploaded files. (No screen_to_workflow entries present in relationships.json) 

# PMflow-A3194D5E-D434-F011-8C4C-6045BD0ACADB
- Trigger: manual: Request, PowerAppV2 
- Purpose: Manual flow invoked from the app (PowerApps trigger); performs automated steps using configured connectors and environment variables. 
- Actions detected: Post message 
- Connectors used: shared_teams 
- Environment variables referenced: wmreply_Replybrary_App_Link 
- Invoked from screens: Not found in uploaded files. (No screen_to_workflow entries present in relationships.json) 

# PMFormReminderFlow-B025C04D-D342-F011-877A-000D3A0CEB69
- Trigger: Recurrence: Recurrence 
- Purpose: Scheduled reminder flow that notifies users (exact channel depends on configured connectors). 
- Actions detected: Get items; Post message 
- Connectors used: shared_sharepointonline; shared_teams 
- Environment variables referenced: wmreply_Replybrary_App_Link; wmreply_Replybrary_Project_List; wmreply_Replybrary_SP_Site 
- Invoked from screens: Not found in uploaded files. (No screen_to_workflow entries present in relationships.json) 

# ReplybraryDailyPipedrivecheck-5898F434-A147-F011-8779-000D3A0CEB69
- Trigger: Recurrence: Recurrence 
- Purpose: Scheduled daily check flow related to Pipedrive (business logic depends on flow actions). 
- Actions detected: Get items 
- Connectors used: shared_office365users; shared_sharepointonline 
- Environment variables referenced: wmreply_Replybrary_People_List; wmreply_Replybrary_Project_List; wmreply_Replybrary_SP_Site 
- Invoked from screens: Not found in uploaded files. (No screen_to_workflow entries present in relationships.json) 

# Replybrary_GetExchangeRate-4E437A2B-C74D-F011-877B-000D3A0CEB69
- Trigger: Recurrence: Recurrence 
- Purpose: Scheduled flow that refreshes or updates exchange-rate data using configured connectors. 
- Actions detected: Get items 
- Connectors used: shared_sharepointonline 
- Environment variables referenced: wmreply_Replybrary_Currency_List; wmreply_Replybrary_SP_Site 
- Invoked from screens: Not found in uploaded files. (No screen_to_workflow entries present in relationships.json) 

# Replybrary_Identifier_Flow-38CA161E-BA34-F011-8C4C-6045BD0ACADB
- Trigger: When_an_item_is_created: OpenApiConnection 
- Purpose: Event-driven flow triggered when new records are created; performs follow-up updates/notifications. 
- Actions detected: Post message 
- Connectors used: shared_sharepointonline; shared_teams 
- Environment variables referenced: wmreply_Replybrary_App_Link; wmreply_Replybrary_Project_List; wmreply_Replybrary_SP_Site 
- Invoked from screens: Not found in uploaded files. (No screen_to_workflow entries present in relationships.json) 

# UploadLogo-F9309D25-C742-F011-877A-000D3A0CEB69
- Trigger: manual: Request, PowerAppV2 
- Purpose: Triggered from the app to upload/store a file using configured connectors. 
- Actions detected: Get items 
- Connectors used: shared_sharepointonline 
- Environment variables referenced: wmreply_Replybrary_Client_List; wmreply_Replybrary_SP_Site 
- Invoked from screens: Not found in uploaded files. (No screen_to_workflow entries present in relationships.json) 

(Note: All workflow details above are taken from workflows_detailed.json; relationships.json was checked for screen_to_workflow mappings but none were present in the uploaded relationships.json file.)  