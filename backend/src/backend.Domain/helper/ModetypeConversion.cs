namespace backend.Domain; 

public static class ModetypeConversion
{
    public static SelectedMode ToModeType(string mode) => mode switch
    {
        "erdiagram" => SelectedMode.ERDiagram,
        "uihierarchy" => SelectedMode.UIHierarchy, 
        "programflow" => SelectedMode.ProgramFlow, 
        _ => SelectedMode.None

    }; 
}