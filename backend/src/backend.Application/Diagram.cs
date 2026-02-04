
using ClosedXML.Excel;

public class EnvironmentVariableValue
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required string Description { get; set; }

    public required string DevValue { get; set; }
    public string? TestValue { get; set; }
    public string? ProductionValue { get; set; }

    public override bool Equals(object? obj)
        => obj is EnvironmentVariableValue other
           && Name == other.Name;

    public override int GetHashCode()
        => Name.GetHashCode();
}


class Diagram
{
    public static void construct_table(HashSet<EnvironmentVariableValue> parsedJsonRes, string file_path)
    {
        //convert string to json object
        //loop through json object and create table

        //table specifications
        string[] columns = { "Environment Variable Name", "Type", "Description", "Dev Value - *Name of Dev Environment* - DEV", "Test Value -  * Name of UAT Environment* - UAT", "Production Value - *Name of Production Environment*" };
        string header_color = "#90bcf2";
        string depreciated_color = "#ffc738";

        using (var workbook = new XLWorkbook())
        {
            //init table
            var worksheet = workbook.Worksheets.Add("Sample Sheet");
            for (int i = 0; i < columns.Length; i++)
            {
                char col = (char)('B' + i);
                worksheet.Cell($"{col}2").Value = columns[i];
                worksheet.Cell($"{col}2").Style.Fill.BackgroundColor = XLColor.FromHtml(header_color);
                worksheet.Cell($"{col}2").Style.Font.Bold = true;
                worksheet.Cell($"{col}2").Style.Font.FontColor = XLColor.White;
            }

            //populate table
            for (int c = 0; c < parsedJsonRes.Count; c++)
            {
                worksheet.Cell($"B{c + 3}").Value = parsedJsonRes.ElementAt(c).Name; 
                worksheet.Cell($"C{c + 3}").Value = parsedJsonRes.ElementAt(c).Type; 
                worksheet.Cell($"D{c + 3}").Value = parsedJsonRes.ElementAt(c).Description; 
                worksheet.Cell($"E{c + 3}").Value = parsedJsonRes.ElementAt(c).DevValue; 
                worksheet.Cell($"F{c + 3}").Value = parsedJsonRes.ElementAt(c).TestValue;
                worksheet.Cell($"G{c + 3}").Value = parsedJsonRes.ElementAt(c).ProductionValue;
            }
            workbook.SaveAs(file_path);
        }
       
    }
}