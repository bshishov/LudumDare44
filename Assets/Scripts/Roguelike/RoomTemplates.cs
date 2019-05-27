using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Utils;

public class RoomTemplates : Singleton<RoomTemplates>
{
    public List<GameObject> DownRooms;
    public List<GameObject> UpRooms;
    public List<GameObject> LeftRooms;
    public List<GameObject> RightRooms;

    public List<GameObject> GetAllRooms()
    {
        return DownRooms.Union(UpRooms).ToList().Union(LeftRooms).ToList().Union(RightRooms).ToList();
    }

}
