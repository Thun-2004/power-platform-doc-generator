# FAQ — Solution (based only on uploaded chunks)

1. Q: What does the solution contain at a high level?  
   A: Top-level components include Assets, botcomponents, bots, CanvasApps, environment variable definitions, Workflows, solution.xml and customizations.xml. Counts: 10 workflows, 2 canvas app groups, 16 environment variables, 18 workflow connectors, 44 relationships. 

2. Q: How many workflows are included and what are their filenames?  
   A: There are 10 workflow files, for example: EndFormFlow-8292..., EndFormFlowReminder-128F..., IdentifierReminderFlow-D4AE..., PeopleListFlow-3CE0..., PMflow-A319..., PMFormReminderFlow-B025..., ReplybraryDailyPipedrivecheck-5898..., Replybrary_GetExchangeRate-4E43..., Replybrary_Identifier_Flow-38CA..., UploadLogo-F9309.... 

3. Q: What triggers and purposes are used by flows (examples)?  
   A: Many flows use Recurrence (scheduled reminders, checks, exchange-rate refreshes). Some are event-driven (When an item is created) and some are manual/PowerApp triggers (invoked from Canvas app to upload or post). Examples documented in the workflow details. 

4. Q: Which connectors do the workflows use?  
   A: Common connectors include SharePoint Online (shared_sharepointonline), Microsoft Teams (shared_teams), Office 365 Users (shared_office365users) and Office 365 Groups (shared_office365groups). Connector relationships are present for many flows. 

5. Q: What environment variables are referenced by the solution?  
   A: Observed env var names include wmreply_Replybrary_App_Link, wmreply_Replybrary_Project_List, wmreply_Replybrary_SP_Site, wmreply_Replybrary_People_List, wmreply_Replybrary_Currency_List, wmreply_Replybrary_Client_List, wmreply_Replybrary_M365GroupID and others (total 16 envvars reported).  

6. Q: Are there Canvas apps included? If so, what are they called?  
   A: Yes — two app groups: wmreply_replybraryv2_c933c and wmreply_replybrary_b320d (each includes an .msapp document URI and related identity/background files).  

7. Q: Do the Canvas apps include screen details or app-level connectors in the uploaded chunks?  
   A: Not found in uploaded files — the canvas app groups exist but screens and connectors arrays are empty in the extracted metadata. 

8. Q: Which workflows are invoked from the Canvas app (PowerApps triggers)?  
   A: PMflow-A3194... and UploadLogo-F9309... are documented as manual flows triggered via PowerApps; PMflow posts to Teams and uses wmreply_Replybrary_App_Link. 

9. Q: Where does the solution store or read data (data sources)?  
   A: Many flows interact with SharePoint lists/sites (env vars referencing Project_List, People_List, Currency_List, Client_List and SP_Site) and some use M365 connectors for people/groups.  

10. Q: Is there deployment, troubleshooting, or user documentation included?  
    A: Not found in uploaded files — no deployment guide or troubleshooting docs present in the provided chunks. 

If you want, I can expand any answer with full lists (all env var names, all connectors per flow, or full workflow triggers/purposes) using the uploaded chunks.