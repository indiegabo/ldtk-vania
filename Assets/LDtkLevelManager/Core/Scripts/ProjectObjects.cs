using System.Collections.Generic;
using LDtkLevelManager.Utils;
using UnityEngine;
using LDtkUnity;

namespace LDtkLevelManager
{
    public class PaginatedResponse<T>
    {
        public int TotalCount { get; set; }
        public List<T> Items { get; set; }
    }

    public struct PaginationInfo
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

    public struct LevelListFilters
    {
        public string world;
        public string area;
        public string levelName;
    }

    [System.Serializable]
    public class WorldInfo
    {
        public string iid;
        public string name;
        public List<string> areas;
    }

    [System.Serializable]
    public class InfoDictionary : SerializedDictionary<string, LevelInfo> { }

    [System.Serializable]
    public class WorldInfoDictionary : SerializedDictionary<string, WorldInfo> { }
}