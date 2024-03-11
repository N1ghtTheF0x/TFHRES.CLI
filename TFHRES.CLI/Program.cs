using System.Diagnostics;
using System.Text;
using ThemModdingHerds.TFHResource;
using ThemModdingHerds.TFHResource.Data;

const string DATABASE_STR = "database:/";

if (args.Length == 0)
{
    Console.WriteLine("Usage: TFHRES.CLI <.tfhres-file> ...");
    Environment.Exit(1);
}
foreach(string file in args)
{
    string path = Path.Combine(Environment.CurrentDirectory,file);
    if(File.Exists(path))
        ExtractFile(path);
}

void ExtractFile(string file)
{
    string name = Path.GetFileNameWithoutExtension(file);
    string output = Path.Combine(Environment.CurrentDirectory,name);
    if(!Directory.Exists(output))
        Directory.CreateDirectory(output);
    Database database = new(file);
    Console.WriteLine($"{name}: extraction start!");
    Stopwatch watch = Stopwatch.StartNew();
    database.Open();
    string FindShortPath(string shortname,string fallback)
    {
        foreach(CacheRecord record in database.ReadCacheRecord())
        {
            if(record.Shortname == shortname)
                return record.SourcePath;
        }
        foreach(FilemapRecord record in database.ReadFilemapRecord())
        {
            if(record.Shortname == shortname)
                return record.SourcePath;
        }
        string newPath = $"{fallback}/{shortname.Replace(DATABASE_STR,"")}";
        Console.WriteLine($"WARN: couldn't find source path for '{shortname}', using {newPath} as fallback");
        string folder = Path.Combine(output,fallback);
        CreateFolder(folder);
        return newPath;
    }

    void CreateFolder(string folder)
    {
        if(!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
    }
    void CreateFolderForFile(string file)
    {
        string? folder = Path.GetDirectoryName(file);
        if(folder == null) return;
        CreateFolder(folder);
    }
    foreach(CachedImage image in database.ReadCachedImage())
    {
        string path = FindShortPath(image.Shortname,"cachedImage");
        if(image.IsCompressed != 0)
            path += ".compressed";
        string fullpath = Path.Combine(output,path);
        CreateFolderForFile(fullpath);
        FileStream stream = File.OpenWrite(fullpath);
        Console.WriteLine($"{name}: writing image {image.Shortname}");
        stream.Write(image.ImageData);
        stream.Close();
    }

    foreach(CachedTextfile textfile in database.ReadCachedTextfile())
    {
        string fullpath = Path.Combine(output,textfile.SourceFile);
        CreateFolderForFile(fullpath);
        FileStream stream = File.OpenWrite(fullpath);
        Console.WriteLine($"{name}: writing text file {textfile.Shortname}");
        stream.Write(textfile.TextData);
        stream.Close();
    }

    FileStream image_biome_file = File.OpenWrite(Path.Combine(output,"image_biome_records.txt"));
    List<string> image_biomes = [];
    foreach(ImageBiomeRecord imageBiome in database.ReadImageBiomeRecord())
    {
        image_biomes.Add($"{imageBiome.Biomename} -> {FindShortPath(imageBiome.ImageShortname,"cachedImage")}\n");
    }
    if(image_biomes.Count != 0)
    {
        Console.WriteLine($"{name}: writing {image_biomes.Count} image biome entries");
        image_biome_file.Write(Encoding.UTF8.GetBytes(string.Join('\n',image_biomes)));
    }
    image_biome_file.Close();

    foreach(InkBytecode inkBytecode in database.ReadInkBytecode())
    {
        string fullpath = Path.Combine(output,inkBytecode.SourceFile);
        CreateFolderForFile(fullpath);
        FileStream stream = File.OpenWrite(fullpath);
        Console.WriteLine($"{name}: writing ink bytecode {inkBytecode.Shortname}");
        stream.Write(Encoding.UTF8.GetBytes(inkBytecode.Bytecode));
        stream.Close();
    }
    Dictionary<string,List<string>> localizedTexts = [];
    foreach(LocalizedText localizedText in database.ReadLocalizedText())
    {
        string value = $"{localizedText.Tag} -> {localizedText.Text}";
        List<string> values = localizedTexts.ContainsKey(localizedText.Langcode) ? localizedTexts[localizedText.Langcode] : [];
        values.Add(value);
        localizedTexts[localizedText.Langcode] = values;
    }
    foreach(KeyValuePair<string,List<string>> localizedText in localizedTexts)
    {
        string fullpath = Path.Combine(output,"localizedText",$"{localizedText.Key}.txt");
        CreateFolderForFile(fullpath);
        FileStream stream = File.OpenWrite(fullpath);
        Console.WriteLine($"{name}: writing ${localizedText.Value.Count} entries for language ${localizedText.Key}");
        stream.Write(Encoding.UTF8.GetBytes(string.Join('\n',localizedText.Value)));
        stream.Close();
    }

    FileStream map_biome_file = File.OpenWrite(Path.Combine(output,"map_biome_records.txt"));
    List<string> map_biomes = [];
    foreach(MapBiomeRecord mapBiome in database.ReadMapBiomeRecord())
    {
        map_biomes.Add($"{mapBiome.Biomename} -> {FindShortPath(mapBiome.MapShortname,"maps")}");
    }
    if(map_biomes.Count != 0)
    {
        Console.WriteLine($"{name}: writing {map_biomes.Count} map biome entries");
        map_biome_file.Write(Encoding.UTF8.GetBytes(string.Join('\n',map_biomes)));
    }
    map_biome_file.Close();
    watch.Stop();
    Console.WriteLine($"{name}: extraction complete! {watch.ElapsedMilliseconds}ms");
}