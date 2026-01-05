namespace backend.Domain; 

public static class PermittedFiletypeConversion
{
    public static PermittedExtensions ToExtension(string type) => type switch
    {
        ".msapp" => PermittedExtensions.MSApp,
        ".zip" => PermittedExtensions.Zip, 
        ".pdf" => PermittedExtensions.Pdf, 
        _ => PermittedExtensions.Unknown
    }; 
}