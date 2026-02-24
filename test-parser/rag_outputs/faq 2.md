# FAQ — Solution (based only on uploaded chunks)

### Q1: What does this solution package contain?
A1: Top-level items include Assets/, botcomponents/, bots/, CanvasApps/, Workflows/, environmentvariabledefinitions/, customizations.xml, solution.xml and [Content_Types].xml, as shown in the overview.json file .

### Q2: How many workflows are included?
A2: There are 10 workflows in the package (see workflows.json) .

### Q3: What are the workflow filenames?
A3: The workflows listed include EndFormFlow-82921763-..., EndFormFlowReminder-128F82AA-..., IdentifierReminderFlow-D4AE74B0-..., PeopleListFlow-3CE0853C-..., PMflow-A3194D5E-..., PMFormReminderFlow-B025C04D-..., ReplybraryDailyPipedrivecheck-5898F434-..., Replybrary_GetExchangeRate-4E437A2B-..., Replybrary_Identifier_Flow-38CA161E-..., and UploadLogo-F9309D25-... (full names present in workflows.json) .

### Q4: Which environment variables are defined for the solution?
A4: The uploaded env var list shows 16 definitions, including wmreply_Replybrary_Admin_List, _App_Link, _Client_List, _Currency_List, _M365GroupID, _People_List, _Project_List, _SP_Site, etc. See envvars.json for the full list .

### Q5: Which env vars does each workflow use?
A5: The detailed mapping in workflows_detailed.json shows per-workflow env vars. Examples:
- EndFormFlow / EndFormFlowReminder / IdentifierReminderFlow / Replybrary_Identifier_Flow: wmreply_Replybrary_App_Link, wmreply_Replybrary_Project_List, wmreply_Replybrary_SP_Site.  
- PeopleListFlow: wmreply_Replybrary_M365GroupID, wmreply_Replybrary_People_List, wmreply_Replybrary_SP_Site.  
- PMflow: wmreply_Replybrary_App_Link.  
- ReplybraryDailyPipedrivecheck: wmreply_Replybrary_People_List, wmreply_Replybrary_Project_List, wmreply_Replybrary_SP_Site.  
- Replybrary_GetExchangeRate: wmreply_Replybrary_Currency_List, wmreply_Replybrary_SP_Site.  
- UploadLogo: wmreply_Replybrary_Client_List, wmreply_Replybrary_SP_Site.  
(Full mappings available in workflows_detailed.json) .

### Q6: What connectors do the workflows use?
A6: Connectors referenced include shared_sharepointonline, shared_teams, shared_office365users, and shared_office365groups, as shown in the workflow details and relationships data  .

### Q7: How many Canvas apps are included and what are their names?
A7: Two canvas apps are listed: wmreply_replybraryv2_c933c and wmreply_replybrary_b320d, per canvasapps_detailed.json .

### Q8: What screens / connectors / files do the Canvas apps contain?
A8: In the uploaded canvasapps_detailed.json the screens, connectors, and files_seen arrays are empty for both apps — no detailed screen/connector/file entries found in the chunks .

### Q9: Are bot components and bots detailed in the uploads?
A9: The overview lists botcomponents/ and bots/ directories, but no detailed bot files or definitions were present in the uploaded chunks — Not found in uploaded files for detailed bot contents .

### Q10: Where can I find the raw workflow and solution XML files in the package?
A10: Raw workflow JSON files are under Workflows/ (names listed in workflows.json) and solution metadata is present as customizations.xml and solution.xml at the package root, per overview.json and workflows.json  .

If you want, I can expand any answer (e.g., paste full workflow→env-var mappings or full env-var list) using only the uploaded chunks.