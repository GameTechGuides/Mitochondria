﻿using System.Runtime.CompilerServices;
using Mitochondria.Api.Storage;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Mitochondria.Framework.Storage;

public class YamlStorage : IStorage
{
    public YamlStorageConfiguration StorageConfiguration { get; }

    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    private readonly ConditionalWeakTable<string, object> _cache;

    public YamlStorage(YamlStorageConfiguration storageConfiguration)
    {
        StorageConfiguration = storageConfiguration;

        _serializer = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        _deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        _cache = new ConditionalWeakTable<string, object>();
    }

    public void Save(string fileName, object obj)
    {
        var savePath = StorageConfiguration.GetAbsoluteSavePath(fileName);
        _cache.AddOrUpdate(savePath, obj);
        
        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

        using var streamWriter = File.CreateText(savePath);
        _serializer.Serialize(streamWriter, obj);
    }

    public T Load<T>(string fileName, IEnumerable<string>? altFileNames = null)
        where T : class
    {
        if (_cache.TryGetValue(
                StorageConfiguration.GetAbsoluteSavePath(fileName),
                out var cachedObj) && cachedObj is T typedObj)
        {
            return typedObj;
        }
        
        foreach (var path in StorageConfiguration.GetAbsoluteLoadPaths(fileName, altFileNames))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                T? obj;

                using (var streamReader = File.OpenText(path))
                {
                    obj = _deserializer.Deserialize<T?>(streamReader);
                }

                if (obj == null)
                {
                    continue;
                }

                Save(fileName, obj);
                _cache.AddOrUpdate(StorageConfiguration.GetAbsoluteSavePath(fileName), obj);

                return obj;
            }
            catch
            {
                // ignored
            }
        }

        var newObj = (T) Activator.CreateInstance(typeof(T), true)!;

        Save(StorageConfiguration.GetAbsoluteSavePath(fileName), newObj);
        _cache.AddOrUpdate(fileName, newObj);

        return newObj;
    }

    public void Delete(string fileName, IEnumerable<string>? altFileNames = null)
    {
        _cache.Remove(StorageConfiguration.GetAbsoluteSavePath(fileName));

        foreach (var path in StorageConfiguration.GetAbsoluteLoadPaths(fileName, altFileNames))
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}