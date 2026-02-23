# FAQ — Solution (from uploaded files only)

1. Q: What does this solution package contain?  
   A: Top-level items include Assets/, botcomponents/, bots/, CanvasApps/, environmentvariabledefinitions/, Workflows/, plus customizations.xml and solution.xml; counts show 10 workflows, 2 canvas app groups, and 16 env vars. 

2. Q: How many workflows are included and what are their filenames?  
   A: 10 workflow files: EndFormFlow-82921763..., EndFormFlowReminder-128F82AA..., IdentifierReminderFlow-D4AE74B0..., PeopleListFlow-3CE0853C..., PMflow-A3194D5E..., PMFormReminderFlow-B025C04D..., ReplybraryDailyPipedrivecheck-5898F434..., Replybrary_GetExchangeRate-4E437A2B..., Replybrary_Identifier_Flow-38CA161E..., UploadLogo-F9309D25.... 

3. Q: What triggers and general purposes do workflows have?  
   A: Many are Recurrence (scheduled) flows for reminders, data refreshes or checks; some are event-driven (When an item is created) and a few are manual/PowerApp-triggered for app-initiated actions. Examples and purposes are listed per workflow in the workflows_detailed file. 

4. Q: Which connectors do the workflows use?  
   A: Workflows use SharePoint (shared_sharepointonline), Teams (shared_teams), Office365Users, Office365Groups and related connectors — see workflow-to-connector relationships for each flow. 

5. Q: What environment variables are defined in the solution?  
   A: 16 env vars are defined, e.g. wmreply_Replybrary_Admin_List, wmreply_Replybrary_App_Link, wmreply_Replybrary_Client_List, wmreply_Replybrary_Currency_List, wmreply_Replybrary_M365GroupID, wmreply_Replybrary_People_List, wmreply_Replybrary_Project_List, wmreply_Replybrary_SP_Site, etc. 

6. Q: Which env vars do workflows reference?  
   A: Many workflows reference vars such as wmreply_Replybrary_App_Link, wmreply_Replybrary_Project_List, wmreply_Replybrary_SP_Site, wmreply_Replybrary_People_List, wmreply_Replybrary_M365GroupID, wmreply_Replybrary_Currency_List and others — detailed per-workflow in workflows_detailed. 

7. Q: Are there Canvas apps included? If so, which ones?  
   A: Yes — two canvas app groups: wmreply_replybraryv2_c933c and wmreply_replybrary_b320d (each includes a .msapp document URI and related metadata). 

8. Q: Do the Canvas apps include screens or connectors in the uploaded chunks?  
   A: The canvasapps_detailed file shows both apps with empty "screens" and "connectors" lists (no screen or connector details present in uploaded chunks). 

9. Q: Which flows are triggered from the app (PowerApps) and what do they do?  
   A: PMflow-A3194D5E... and UploadLogo-F9309D25... show manual triggers (Request, PowerAppV2). PMflow posts messages to Teams; UploadLogo is an app-triggered flow to upload/store files. See the workflows_detailed entries for specifics. 

10. Q: Where are deployment steps, prerequisites, or detailed setup instructions?  
    A: Not found in uploaded files.

If you want, I can expand any specific Q&A with exact per-workflow env-var lists, connector lists or full filenames from the detailed files.