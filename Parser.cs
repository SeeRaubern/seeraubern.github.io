using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parser
{


    public static List<int> findWordLocations(string line, string word)
    {
        List<int> indexes = new List<int>();

        for (int i = 0; i < line.Length - word.Length; i++)
        {
            if (line.Substring(i, word.Length) == word)
            {
                if (i == 0 || line[i-1] == '\t' || line[i-1] == ' ' || line[i - 1] == '(')
                {
                    if (line[i + word.Length] == ' ' || line[i + word.Length] == '(' || line[i + word.Length] == '.' || line[i + word.Length] == '[' || line[i + word.Length] == '\n')
                    { 
                        indexes.Add(i);
                    }
                }
            }
        }
        return indexes;
    }
}
