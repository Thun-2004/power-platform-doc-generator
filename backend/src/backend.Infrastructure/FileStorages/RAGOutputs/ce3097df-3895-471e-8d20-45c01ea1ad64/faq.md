# FAQ — Solution (concise)

1. Q: What does the solution contain at a high level?  
   A: Top-level includes Assets/, botcomponents/, bots/, CanvasApps/, environmentvariabledefinitions/, Workflows/, plus customizations.xml and solution.xml. Counts: 10 workflows, 2 canvas-app groups, 16 environment variables. 

2. Q: How many workflows are in the solution and where can I see their filenames?  
   A: There are 10 workflow JSON files; filenames are listed in workflows.json (e.g., EndFormFlow-....json, PeopleListFlow-....json, Replybrary_GetExchangeRate-....json, UploadLogo-....json, etc.). 

3. Q: What triggers do the workflows use?  
   A: Triggers include Recurrence (scheduled), manual (PowerAppV2 / Request), and When an item is created. Example flows and triggers are in the workflow details. 

4. Q: Which connectors are commonly used by the flows?  
   A: Common connectors: shared_sharepointonline, shared_teams, shared_office365users, shared_office365groups (listed across workflows/relationships). 

5. Q: Which environment variables are defined in the solution?  
   A: 16 env vars are present, including wmreply_Replybrary_App_Link, _SP_Site, _Project_List, _People_List, _Client_List, _Currency_List, _M365GroupID, _ClientContacts_List, etc. See envvars.json for the full list. 

6. Q: Which env vars does PeopleListFlow (example) use?  
   A: PeopleListFlow uses wmreply_Replybrary_M365GroupID, wmreply_Replybrary_People_List, and wmreply_Replybrary_SP_Site. 

7. Q: Are there Canvas apps included? Which ones?  
   A: Yes — two canvas-app groups are present: wmreply_replybraryv2_c933c and wmreply_replybrary_b320d (each has a .msapp DocumentUri and associated files). 

8. Q: How many screens are defined for the canvas apps?  
   A: Screens count is 0 in the overview metadata (no screen JSONs in the uploaded chunks). 

9. Q: Do workflows post to Teams or read from SharePoint?  
   A: Yes — many flows use actions like Get items (SharePoint) and Post message (Teams) via respective connectors (shared_sharepointonline, shared_teams).  

10. Q: Where are the env var values (actual connection strings/IDs) or canvas-app screen layouts?  
    A: Not found in uploaded files.

11. Q: Where can I find the solution manifest / customization XML?  
    A: customizations.xml and solution.xml are present at the package root (files listed in overview). 

If you want this converted to a different layout (table, separate sections per flow, or expanded to include every filename per flow), tell me which format and I'll expand using only the uploaded chunks.