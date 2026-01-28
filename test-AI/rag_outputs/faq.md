# FAQ — Reply-brary solution (from uploaded files)

1. Q: Which automated workflows (Power Automate flows) are included?  
   A: IdentifierReminderFlow, PMFormReminderFlow and ReplybraryDailyPipedrivecheck are present in the uploaded files.   

2. Q: What does IdentifierReminderFlow do?  
   A: Sends an adaptive-card reminder (via Flow bot / Teams) telling the assigned identifier to open the Reply-brary App and assign a PM; it contains a parameter for the app link.  

3. Q: What does PMFormReminderFlow do?  
   A: Groups projects by PM and posts reminders to PMs about submitting project forms (uses variables like GroupedProjects/newarray in the flow). 

4. Q: What does ReplybraryDailyPipedrivecheck do?  
   A: Runs on a daily recurrence and calls the Pipedrive API (HTTP GET) to fetch latest deals using an API key variable. 

5. Q: What triggers / schedules are defined for the flows?  
   A: ReplybraryDailyPipedrivecheck is scheduled daily (Recurrence) and IdentifierReminderFlow is scheduled weekly (Monday); exact PMFormReminderFlow schedule is Not found in uploaded files.  

6. Q: What environment variables / flow parameters are used?  
   A: Flows use parameters such as Replybrary_SP_Site (SharePoint site URL), Replybrary_Project_List, Replybrary_People_List and Replybrary_App_Link; ReplybraryDaily uses an API_Key variable for Pipedrive.  

7. Q: Which external connections / APIs are used?  
   A: Connections include shared_sharepointonline, shared_teams and shared_office365users; Pipedrive API is called via HTTP in a flow.  

8. Q: What canvas apps / screens are in the solution?  
   A: The Reply-brary canvas app (Reply-brary App) is present with screens/components such as Project Library, Project Form/Team tabs, Project Info, Lessons, Ideas, People and Clients (many control IDs and screen names appear in the canvas export).  

9. Q: What SharePoint site / lists are referenced?  
   A: The SharePoint site is https://wmreplyukdev.sharepoint.com/sites/ReplybraryDev and lists include Replybrary_Project_List and Replybrary_People_List (GUIDs provided as default parameter values).  

10. Q: Does the solution include any bot / configuration files?  
    A: Yes — a configuration.json (BotConfiguration / GPT/AI settings) is included (shows GenerativeActionsEnabled and GPT/AI settings). 

11. Q: Where is the Reply-brary app play link?  
    A: The Replybrary_App_Link parameter contains the Power Apps play URL (present in IdentifierReminderFlow parameters). 

12. Q: Any other implementation details (e.g., variable names, galleries, controls)?  
    A: The canvas export shows many control IDs and names (galleries, data cards, labels, buttons, link canvases, ProjectLessonsGallery, etc.), and flows use variables like GroupedProjects/newarray; for granular control/property lists see the canvas/flow JSON exports.  

If you want, I can convert this into a single Markdown file for download or expand any answer with exact JSON snippets from the uploaded files.