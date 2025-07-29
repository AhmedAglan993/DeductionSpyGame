using System;
using System.Collections.Generic;

[Serializable]
public class RoomData
{
    public Player[] players;
    public string word;
}

[Serializable]
public class Player
{
    public string playerName;
    public bool isImposter;
}

