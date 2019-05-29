using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Utils;
using UnityEngine.AI;

public class GenerateRooms : MonoBehaviour
{
    public Vector3 worldSize;
    public int numberOfRooms = 20;
    public NavMeshSurface surface;

    private Room[,] _rooms;

    private List<Vector3> _takenPositions = new List<Vector3>();

    private int _gridSizeX, _gridSizeZ;
    private GameObject _lastRoom;
        

    private List<GameObject> _allRooms = new List<GameObject>();
       

    // Start is called before the first frame update
    void Start()
    {
        _allRooms = RoomTemplates.Instance.GetAllRooms();
        if (numberOfRooms >= (worldSize.x * 2) * (worldSize.z * 2))
        {
            numberOfRooms = Mathf.RoundToInt((worldSize.x * 2) * (worldSize.z * 2));
        }
        _gridSizeX = Mathf.RoundToInt(worldSize.x);
        _gridSizeZ = Mathf.RoundToInt(worldSize.z);
        CreateRooms();
        SetRoomDoors();
        DrawMap();
        _lastRoom.GetComponent<NavMeshSurface>().BuildNavMesh();
    }
    void CreateRooms()
    {
        //setup
        _rooms = new Room[_gridSizeX * 2, _gridSizeZ * 2];
        _rooms[_gridSizeX, _gridSizeZ] = new Room(Vector3.zero, 1);
        _takenPositions.Insert(0, Vector3.zero);
        Vector3 checkPos = Vector3.zero;
        //magic numbers
        float randomCompare = 0.2f, randomCompareStart = 0.2f, randomCompareEnd = 0.01f;
        //add _rooms
        for (int i = 0; i < numberOfRooms - 1; i++)
        {
            float randomPerc = ((float)i) / (((float)numberOfRooms - 1));
            randomCompare = Mathf.Lerp(randomCompareStart, randomCompareEnd, randomPerc);
            //grab new position
            checkPos = NewPosition();
            //test new position
            
            if (NumberOfNeighbors(checkPos, _takenPositions) > 1 && Random.value > randomCompare)
            {
                int iterations = 0;
                do
                {
                    checkPos = SelectiveNewPosition();
                    iterations++;
                } while (NumberOfNeighbors(checkPos, _takenPositions) > 1 && iterations < 100);
                if (iterations >= 50)
                    print("error: could not create with fewer neighbors than : " + NumberOfNeighbors(checkPos, _takenPositions));
            }
            //finalize position
            _rooms[(int)checkPos.x + _gridSizeX, (int)checkPos.z + _gridSizeZ] = new Room(checkPos, 0);
            _takenPositions.Insert(0, checkPos);
        }
    }

    Vector3 NewPosition()
    {
        int x = 0, z = 0;
        Vector3 checkingPos = Vector3.zero;
        do
        {
            int index = Mathf.RoundToInt(Random.value * (_takenPositions.Count - 1)); // pick a random room
            x = (int)_takenPositions[index].x;//capture its x, z position
            z = (int)_takenPositions[index].z;
            bool UpDown = (Random.value < 0.5f);//randomly pick wether to look on hor or vert axis
            bool positive = (Random.value < 0.5f);//pick whether to be positive or negative on that axis
            if (UpDown)
            { //find the position bnased on the above bools
                if (positive)
                {
                    z += 1;
                }
                else
                {
                    z -= 1;
                }
            }
            else
            {
                if (positive)
                {
                    x += 1;
                }
                else
                {
                    x -= 1;
                }
            }
            checkingPos = new Vector3(x,0,z);
        } while (_takenPositions.Contains(checkingPos) || x >= _gridSizeX || x < -_gridSizeX || z >= _gridSizeZ || z < -_gridSizeZ); //make sure the position is valid
        return checkingPos;
    }
    Vector3 SelectiveNewPosition()
    { // method differs from the above in the two commented ways
        int index = 0, inc = 0;
        int x = 0, z = 0;
        Vector3 checkingPos = Vector3.zero;
        do
        {
            inc = 0;
            do
            {
                //instead of getting a room to find an adject empty space, we start with one that only 
                //as one neighbor. This will make it more likely that it returns a room that branches out
                index = Mathf.RoundToInt(Random.value * (_takenPositions.Count - 1));
                inc++;
            } while (NumberOfNeighbors(_takenPositions[index], _takenPositions) > 1 && inc < 100);
            x = (int)_takenPositions[index].x;
            z = (int)_takenPositions[index].z;
            bool UpDown = (Random.value < 0.5f);
            bool positive = (Random.value < 0.5f);
            if (UpDown)
            {
                if (positive)
                {
                    z += 1;
                }
                else
                {
                    z -= 1;
                }
            }
            else
            {
                if (positive)
                {
                    x += 1;
                }
                else
                {
                    x -= 1;
                }
            }
            checkingPos = new Vector3(x,0,z);
        } while (_takenPositions.Contains(checkingPos) || x >= _gridSizeX || x < -_gridSizeX || z >= _gridSizeZ || z < -_gridSizeZ);
        if (inc >= 100)
        { // break loop if it takes too long: this loop isnt garuanteed to find solution, which is fine for this
            print("Error: could not find position with only one neighbor");
        }
        return checkingPos;
    }
    int NumberOfNeighbors(Vector3 checkingPos, List<Vector3> usedPositions)
    {
        int ret = 0; // start at zero, add 1 for each side there is already a room
        if (usedPositions.Contains(checkingPos + Vector3.right))
        { //using Vector.[direction] as short hands, for simplicity
            ret++;
        }
        if (usedPositions.Contains(checkingPos + Vector3.left))
        {
            ret++;
        }
        if (usedPositions.Contains(checkingPos + Vector3.forward))
        {
            ret++;
        }
        if (usedPositions.Contains(checkingPos + Vector3.back))
        {
            ret++;
        }
        return ret;
    }
    void DrawMap()
    {
        
        foreach (Room room in _rooms)
        {
            
            if (room == null)
            {
                continue; //skip where there is no room
            }        
            CreateRoom(room);
        }
    }
    void CreateRoom(Room room)
    {
        Vector3 drawPos = room.gridPos;
        drawPos.x *= 50;//aspect ratio of map sprite
        drawPos.z *= 50;
        var possibleRooms2 = new GameObject[_allRooms.Count];
        _allRooms.CopyTo(possibleRooms2);
        var possibleRooms = possibleRooms2.ToList();

        if (room.doorRight)
            possibleRooms = possibleRooms.Intersect(RoomTemplates.Instance.RightRooms).ToList();
        else
            possibleRooms = possibleRooms.Except(RoomTemplates.Instance.RightRooms).ToList();
        
        if (room.doorBot)
            possibleRooms = possibleRooms.Intersect(RoomTemplates.Instance.DownRooms).ToList();
        else
            possibleRooms = possibleRooms.Except(RoomTemplates.Instance.DownRooms).ToList();
        
        if (room.doorTop)
            possibleRooms = possibleRooms.Intersect(RoomTemplates.Instance.UpRooms).ToList();
        else
            possibleRooms = possibleRooms.Except(RoomTemplates.Instance.UpRooms).ToList();
        
        if (room.doorLeft)        
            possibleRooms = possibleRooms.Intersect(RoomTemplates.Instance.LeftRooms).ToList();
        else
            possibleRooms = possibleRooms.Except(RoomTemplates.Instance.LeftRooms).ToList();

        Debug.Log(possibleRooms.Count);
        var placedRoom = RandomUtils.Choice(possibleRooms);
        _lastRoom = Instantiate(placedRoom, drawPos, placedRoom.transform.rotation);
        //_lastRoom.GetComponent<NavMeshSurface>().BuildNavMesh();
    }
    void SetRoomDoors()
    {
        for (int x = 0; x < ((_gridSizeX * 2)); x++)
        {
            for (int z = 0; z < ((_gridSizeZ * 2)); z++)
            {
                if (_rooms[x, z] == null)
                {
                    continue;
                }
                Vector3 gridPosition = new Vector3(x, 0, z);
                if (z - 1 < 0)
                { //check above
                    _rooms[x, z].doorBot = false;
                }
                else
                {
                    _rooms[x, z].doorBot = (_rooms[x, z - 1] != null);
                }
                if (z + 1 >= _gridSizeZ * 2)
                { //check bellow
                    _rooms[x, z].doorTop = false;
                }
                else
                {
                    _rooms[x, z].doorTop = (_rooms[x, z + 1] != null);
                }
                if (x - 1 < 0)
                { //check left
                    _rooms[x, z].doorLeft = false;
                }
                else
                {
                    _rooms[x, z].doorLeft = (_rooms[x - 1, z] != null);
                }
                if (x + 1 >= _gridSizeX * 2)
                { //check right
                    _rooms[x, z].doorRight = false;
                }
                else
                {
                    _rooms[x, z].doorRight = (_rooms[x + 1, z] != null);
                }
            }
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
