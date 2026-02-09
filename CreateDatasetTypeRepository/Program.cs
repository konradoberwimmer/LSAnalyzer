using System.Text;
using System.Text.Json;
using LSAnalyzer.Models;
using Microsoft.Extensions.Configuration;

ConfigurationBuilder builder = new();
Dictionary<string, string> switchMappings = new() { { "-f", "fileName"} };
builder.AddCommandLine(args, switchMappings);
var configuration = builder.Build();

if (configuration["fileName"] is null)
{
    Console.WriteLine("No file name provided (Option -f).");
    return 1;
}

var fileName = configuration["fileName"];

if (!File.Exists(fileName))
{
    Console.WriteLine($"File {fileName} not found.");
    return 2;
}

var directory = Path.GetDirectoryName(fileName)!;

List<DatasetTypeCollection> datasetTypeCollections;

try
{
    datasetTypeCollections = JsonSerializer.Deserialize<List<DatasetTypeCollection>>(File.ReadAllText(fileName))!;
}
catch (Exception e)
{
    Console.WriteLine(e);
    return 3;
}

if (datasetTypeCollections.Any(collection =>
        collection.Entries.Any(entry => !File.Exists(Path.Combine(directory, entry.FileName)))))
{
    var missingFiles = datasetTypeCollections.
        SelectMany(collection => collection.Entries
            .Where(entry => !File.Exists(Path.Combine(directory, entry.FileName)))
            .Select(entry => entry.FileName)
            .ToList())
        .ToList();
    Console.WriteLine($"Mising file(s) from configuration file: {string.Join(", ", missingFiles)}.");
    return 4;
}

StringBuilder indexMdBuilder = new();
indexMdBuilder.AppendLine("# LSAnalyzer official dataset type repository").AppendLine();

foreach (var datasetTypeCollection in datasetTypeCollections)
{
    indexMdBuilder.AppendLine($"## {datasetTypeCollection.Name}").AppendLine();
    
    List<DatasetType> datasetTypes = [];
    foreach (var datasetTypeEntry in datasetTypeCollection.Entries)
    {
        try
        {
            var fileContent = File.ReadAllText(Path.Combine(directory, datasetTypeEntry.FileName));
            
            var datasetType = JsonSerializer.Deserialize<DatasetType>(fileContent)!;

            if (datasetType.Id != datasetTypeEntry.DatasetTypeId)
            {
                Console.WriteLine($"ID {datasetTypeEntry.DatasetTypeId} does not match ID {datasetType.Id} in file {datasetTypeEntry.FileName}.");
                return 6;
            }
            
            datasetTypeEntry.Hash = CreateMD5(fileContent);
            
            datasetTypes.Add(datasetType);
        } catch (Exception e)
        {
            Console.WriteLine(e);
            return 5;
        }
    }
    
    foreach (var datasetTypeGroup in datasetTypes.GroupBy(datasetType => datasetType.Group))
    {
        indexMdBuilder.AppendLine($"### {datasetTypeGroup.Key}").AppendLine();

        foreach (var datasetType in datasetTypeGroup.OrderBy(datasetType => datasetType.Id))
        {
            var correspondingEntry = datasetTypeCollection.Entries.First(entry => entry.DatasetTypeId == datasetType.Id);
            indexMdBuilder.AppendLine($"- {datasetType.Id} - {datasetType.Name} ([{correspondingEntry.FileName}]({correspondingEntry.FileName}), #{correspondingEntry.Hash})").AppendLine();
        }
    }
}

File.WriteAllText(Path.Combine(directory, "index.md"), indexMdBuilder.ToString());
File.WriteAllText(Path.Combine(directory, "index.json"), JsonSerializer.Serialize(datasetTypeCollections));
    
return 0;

static string CreateMD5(string input)
{
    var inputBytes = Encoding.UTF8.GetBytes(input);
    var hashBytes = System.Security.Cryptography.MD5.HashData(inputBytes);

    return Convert.ToHexString(hashBytes);
}