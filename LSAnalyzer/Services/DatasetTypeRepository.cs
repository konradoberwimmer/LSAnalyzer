using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using LSAnalyzer.Models;

namespace LSAnalyzer.Services;

public class DatasetTypeRepository : IDatasetTypeRepository
{
    public int TimeoutInSeconds { get; set; } = 10;
    
    public (IDatasetTypeRepository.FetchResult result, List<DatasetTypeCollection> datasetTypeCollections) FetchDatasetTypeCollections(string url)
    {
        using HttpClient client = new();
        client.Timeout = TimeSpan.FromSeconds(TimeoutInSeconds);

        HttpRequestMessage request = new(HttpMethod.Get, url);
        
        HttpResponseMessage response;

        try
        {
            response = client.Send(request);
        } 
        catch (Exception)
        {
            return (IDatasetTypeRepository.FetchResult.NotFound, []);
        }
        
        if (!response.IsSuccessStatusCode) return (IDatasetTypeRepository.FetchResult.NotFound, []);

        try
        {
            var datasetTypeCollections =
                JsonSerializer.Deserialize<List<DatasetTypeCollection>>(response.Content.ReadAsStream());

            return datasetTypeCollections is null
                ? throw new JsonException("Could not deserialize dataset type collections")
                : (IDatasetTypeRepository.FetchResult.Success, datasetTypeCollections);
        }
        catch (Exception)
        {
            return (IDatasetTypeRepository.FetchResult.Malformed, []);
        }
    }

    public (IDatasetTypeRepository.FetchResult result, DatasetType? datasetType) FetchDatasetType(string baseUrl, string fileName)
    {
        using HttpClient client = new();
        client.Timeout = TimeSpan.FromSeconds(TimeoutInSeconds);

        HttpRequestMessage request = new(HttpMethod.Get, new Uri(new Uri(baseUrl), fileName));
        
        HttpResponseMessage response;

        try
        {
            response = client.Send(request);
        } 
        catch (Exception)
        {
            return (IDatasetTypeRepository.FetchResult.NotFound, null);
        }
        
        if (!response.IsSuccessStatusCode) return (IDatasetTypeRepository.FetchResult.NotFound, null);

        try
        {
            var datasetTypeCollections =
                JsonSerializer.Deserialize<DatasetType>(response.Content.ReadAsStream());

            return datasetTypeCollections is null
                ? throw new JsonException("Could not deserialize dataset type collections")
                : (IDatasetTypeRepository.FetchResult.Success, datasetTypeCollections);
        }
        catch (Exception)
        {
            return (IDatasetTypeRepository.FetchResult.Malformed, null);
        }
    }
}