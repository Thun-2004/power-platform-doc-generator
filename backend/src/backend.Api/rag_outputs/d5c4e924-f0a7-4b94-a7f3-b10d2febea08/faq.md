# FAQ — Solution (from uploaded files)

1. Q: What does this solution contain at a high level?  
   A: Top-level items include Assets/, botcomponents/, bots/, CanvasApps/, customizations.xml, environmentvariabledefinitions/, solution.xml and Workflows/. Counts show 2 canvas app groups, 10 workflows, 16 environment variables, 18 workflow connectors and 44 relationships. 

2. Q: How many workflows are included and what are their names?  
   A: There are 10 workflow files: EndFormFlow, EndFormFlowReminder, IdentifierReminderFlow, PeopleListFlow, PMflow, PMFormReminderFlow, ReplybraryDailyPipedrivecheck, Replybrary_GetExchangeRate, Replybrary_Identifier_Flow, and UploadLogo (full filenames in workflows.json). 

3. Q: What triggers do the workflows use and what are their purposes?  
   A: Most workflows use Recurrence (scheduled) triggers; some use event triggers ("When an item is created") or manual PowerApp triggers. Purposes include scheduled checks/refreshes, reminders/notifications, maintaining people data, event-driven notifications, and an app-invoked upload flow. Examples are in the workflows detailed metadata. 

4. Q: Which connectors are used by the flows?  
   A: Common connectors detected include shared_sharepointonline, shared_teams, shared_office365users and shared_office365groups (varies by flow). Connector usage is shown per-workflow in the detailed workflow metadata. 

5. Q: Which environment variables are defined in the solution?  
   A: The solution defines 16 environment variable entries, including wmreply_Replybrary_Admin_List, wmreply_Replybrary_App_Link, wmreply_Replybrary_Currency_List, wmreply_Replybrary_M365GroupID, wmreply_Replybrary_People_List, wmreply_Replybrary_Project_List, wmreply_Replybrary_SP_Site, wmreply_Replybrary_Client_List, and others. 

6. Q: Which environment variables do workflows reference?  
   A: Workflows reference env vars such as wmreply_Replybrary_App_Link, wmreply_Replybrary_Project_List, wmreply_Replybrary_SP_Site, wmreply_Replybrary_People_List, wmreply_Replybrary_M365GroupID, wmreply_Replybrary_Client_List and wmreply_Replybrary_Currency_List (listed per-workflow in the detailed metadata). 

7. Q: What Canvas apps are included in the solution?  
   A: Two canvas app groups are present: wmreply_replybraryv2_c933c and wmreply_replybrary_b320d (each includes a .msapp document URI and related files). 

8. Q: How many screens/connectors/files were detected inside the Canvas apps?  
   A: The canvas app detail shows empty screen and connector lists for both apps (no screens/connectors detected in the extracted metadata). 

9. Q: Are there app package (.msapp) files included?  
   A: Yes — each canvas app group lists a DocumentUri.msapp entry in the groups metadata (wmreply_replybraryv2_c933c_DocumentUri.msapp and wmreply_replybrary_b320d_DocumentUri.msapp). 

10. Q: Where can I find values for the environment variables or instructions to set them before deployment?  
    A: The uploaded files list the environment variable names but do not contain their runtime values or deployment instructions. Not found in uploaded files. 

If you want, I can produce a compact checklist for deploying this solution (which env vars to populate and which connectors to grant) based on the names and workflow references above.