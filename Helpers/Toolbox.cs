using System.Text;

namespace Foo.Helpers;

public static class Toolbox
{
    /// <summary>
    /// Shuffles a given list
    /// </summary>
    /// <param name="list">The list to be shuffled</param>
    /// <returns>The shuffled list</returns>
    public static List<T>  Shuffle<T>(List<T> list)  
    {  
        var rng = new Random();  
        var n = list.Count;  
        while (n > 1) {  
            n--;  
            var k = rng.Next(n + 1);  
            (list[k], list[n]) = (list[n], list[k]);
        }

        return list;
    }
    
    
    public static bool Contains(this string source, string toCheck, StringComparison comp)
    {
        return source?.IndexOf(toCheck, comp) >= 0;
    }

    private static List<string> RIList = new List<string>() {":zero:", ":one:", ":two:", ":three:", ":four:", ":five:", ":six:", ":seven:", ":eight:", ":nine:"};
    public static string GetRegionalIndicator(int number)
    {
        var output = "";
        foreach(var character in number.ToString())
        {
            output += RIList[int.Parse(character.ToString())];
        }
        return output;
    }
    
    /// <summary>
    /// Creates a page of a given list
    /// A page is defined here as a group of strings seperated by new lines
    /// Hey look, a list ^
    /// </summary>
    /// <param name="stringList">The list of strings to be converted into a page</param>
    /// <param name="pageQuery">The page to return</param>
    /// <param name="sort">Whether we should sort the strings by name</param>
    /// <param name="length">how many entries are used in each page</param>
    /// <param name="enumerate">1 2, buckle my shoe</param>
    /// <returns>The page</returns>
    public static (string, int, int) CreatePageFromList(List<string> stringList, string pageQuery, bool sort, int length, bool enumerate)
    {
        var pages = new List<string>();
        var stringBuilder = new StringBuilder();
        if (sort) {stringList.Sort();}
        
        int pagenumber;
        if (string.IsNullOrWhiteSpace(pageQuery))
        {
            pagenumber = 1;
        }
        else
        {
            var isNumeric = int.TryParse(pageQuery, out pagenumber);
            pagenumber = isNumeric && pagenumber >= 1 ? pagenumber : 1;
        }
        var index = 1;
        foreach (var line in stringList)
        {
            stringBuilder.Append((enumerate ? $"{index}: " : "") + $"{line}\n");
            if (stringBuilder.Length > length)
            {
                pages.Add(stringBuilder.ToString());
                stringBuilder.Clear();
            }

            index++;
        }
        if (!string.IsNullOrWhiteSpace(stringBuilder.ToString()))
        {
            pages.Add(stringBuilder.ToString());
        }
        
        if (pagenumber > pages.Count)
        {
            pagenumber = pages.Count;
        }
        return (pages[pagenumber - 1], pages.Count, pagenumber);
    }
}