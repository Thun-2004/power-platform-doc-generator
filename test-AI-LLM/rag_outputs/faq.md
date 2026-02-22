# Solution FAQ

- Q1: What does this solution contain at a high level?
  - A: It includes multiple folders (Assets, botcomponents, CanvasApps, Workflows, environmentvariabledefinitions, etc.), two customizations.xml files and solution.xml files; counts show 10 workflows, 2 canvas app groups, 16 environment variables and 44 relationships. 

- Q2: Which workflows are included?
  - A: There are 10 flows, including EndFormFlow, EndFormFlowReminder, IdentifierReminderFlow, PeopleListFlow, PMflow, PMFormReminderFlow, ReplybraryDailyPipedrivecheck, Replybrary_GetExchangeRate, Replybrary_Identifier_Flow, and UploadLogo. See the workflows listing. 

- Q3: What triggers do the workflows use?
  - A: Triggers include scheduled Recurrence, manual PowerApp/Request triggers, and an item-created event trigger ("When an item is created"). Examples shown in flows' details. 

- Q4: Which connectors are used by the flows?
  - A: Connectors observed include shared_sharepointonline, shared_teams, shared_office365groups, shared_office365users and others referenced in flow details and relationships.  

- Q5: Which environment variables exist in the solution?
  - A: The solution defines these env vars: wmreply_Replybrary_Admin_List, wmreply_Replybrary_App_Link, wmreply_Replybrary_CertificationsTracker_List, wmreply_Replybrary_ClientContacts_List, wmreply_Replybrary_Client_List, wmreply_Replybrary_Currency_List, wmreply_Replybrary_LessonsLearnt_List, wmreply_Replybrary_Links_List, wmreply_Replybrary_M365GroupID, wmreply_Replybrary_People_List, wmreply_Replybrary_ProjectHistory_List, wmreply_Replybrary_Project_List, wmreply_Replybrary_ReusableIdeas_List, wmreply_Replybrary_SkillLevels_List, wmreply_Replybrary_SMEs_List, wmreply_Replybrary_SP_Site. 

- Q6: Which env vars do the flows reference?
  - A: Flows commonly reference wmreply_Replybrary_App_Link, wmreply_Replybrary_Project_List, wmreply_Replybrary_SP_Site, wmreply_Replybrary_People_List, wmreply_Replybrary_M365GroupID, wmreply_Replybrary_Client_List, wmreply_Replybrary_Currency_List among others as shown in flow details. 

- Q7: What canvas apps are included?
  - A: Two canvas app groups are present: wmreply_replybraryv2_c933c and wmreply_replybrary_b320d (each has a .msapp document and related files).  

- Q8: How many screens/connectors/files do the canvas apps have?
  - A: The extracted details show screens: [] and connectors: [] for each app (no screen/connector details found in uploaded chunks); files include DocumentUri.msapp and related identity/background file entries. If you need full screen definitions, Not found in uploaded files.  

- Q9: Are there solution/customization files included?
  - A: Yes — customizations.xml, customizations 2.xml and solution.xml / solution 2.xml are present in the top-level. 

- Q10: What relationships or data entities are present or referenced?
  - A: The package shows 44 relationships and many workflow-to-env and workflow-to-connector links (examples in the relationships file connecting flows to SharePoint, Teams, env vars, and Office365 connectors). For specific entity/table definitions, Not found in uploaded files.  

If you want this converted to a different layout or expanded with per-flow details (triggers, connectors, env vars per flow), I can expand each Q&A using the flow detail entries.